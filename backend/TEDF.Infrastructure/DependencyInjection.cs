using System.Security.Claims;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Aggregates.UserAggregate.ValueObjects;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Constants;
using TEDF.Domain.Enums.User;
using TEDF.Domain.Services;
using TEDF.Infrastructure.Authentication;
using TEDF.Infrastructure.Authorization;
using TEDF.Infrastructure.Authorization.Policies;
using TEDF.Infrastructure.BackgroundJobs;
using TEDF.Infrastructure.BackgroundJobs.Jobs;
using TEDF.Infrastructure.BackgroundJobs.Scheduling;
using TEDF.Infrastructure.Caching;
using TEDF.Infrastructure.Middleware;
using TEDF.Infrastructure.RealTime.Services;
using TEDF.Infrastructure.Services;
using TEDF.Infrastructure.Services.DomainServices;
using TEDF.Infrastructure.Services.Excel;
using TEDF.Infrastructure.Services.Email;
using TEDF.Infrastructure.Services.Email.Firestore;
using TEDF.Infrastructure.Services.Email.Templates;
using TEDF.Infrastructure.Services.FileStorage;
using TEDF.Infrastructure.Services.Notification;
using TEDF.Persistence.SqlServer.QueryServices;

namespace TEDF.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Firebase Configuration
            services.Configure<FirebaseSettings>(configuration.GetSection(FirebaseSettings.SectionName));
            var firebaseSettings = configuration.GetSection(FirebaseSettings.SectionName).Get<FirebaseSettings>();

            // Initialize Firebase Admin SDK
            if (FirebaseApp.DefaultInstance == null && firebaseSettings != null)
            {
                // When using the Firebase Auth Emulator, set the environment variable
                // so the Admin SDK routes all requests to the local emulator.
                if (firebaseSettings.UseEmulator && !string.IsNullOrEmpty(firebaseSettings.EmulatorHost))
                {
                    Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", firebaseSettings.EmulatorHost);
                }

                var credential = BuildFirebaseCredential(firebaseSettings);

                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = firebaseSettings.ProjectId
                });
            }

            // Firebase Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                var projectId = firebaseSettings?.ProjectId;

                if (firebaseSettings?.UseEmulator == true)
                {
                    // Firebase Auth Emulator: tokens are self-signed, so we skip
                    // issuer signing key validation while keeping other checks.
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = $"https://securetoken.google.com/{projectId}",
                        ValidateAudience = true,
                        ValidAudience = projectId,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = false,
                        SignatureValidator = (token, _) =>
                            new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token)
                    };
                }
                else
                {
                    options.Authority = $"https://securetoken.google.com/{projectId}";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = $"https://securetoken.google.com/{projectId}",
                        ValidateAudience = true,
                        ValidAudience = projectId,
                        ValidateLifetime = true
                    };
                }

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    },

                    OnTokenValidated = async context =>
                    {
                        // Firebase's "sub" claim (ClaimTypes.NameIdentifier) contains the Firebase UID,
                        // not the database Id. Resolve the database Id here and inject it as a
                        // separate claim so that CurrentUserService can return the correct Guid.
                        var firebaseUid = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (string.IsNullOrEmpty(firebaseUid))
                        {
                            context.Fail("Firebase token does not carry a user id.");
                            return;
                        }

                        var userRepo = context.HttpContext.RequestServices
                            .GetRequiredService<IUserRepository>();

                        var user = await userRepo.GetByFirebaseUidAsync(firebaseUid);
                        if (user is null)
                        {
                            // The account can already exist while holding a placeholder FirebaseUid:
                            // imported accounts get "pending:<code>" and seeded ones get "test-*",
                            // because the real UID is only issued the first time the person signs in
                            // with Google. Match on the email instead and re-point the row to the real
                            // UID. The email claim is only trusted when Firebase reports it verified —
                            // otherwise anyone able to create a Firebase account with someone's FPT
                            // address could take that account over.
                            var email = context.Principal?.FindFirstValue("email")
                                ?? context.Principal?.FindFirstValue(ClaimTypes.Email);
                            var emailVerified = string.Equals(
                                context.Principal?.FindFirstValue("email_verified"),
                                "true",
                                StringComparison.OrdinalIgnoreCase);

                            if (!string.IsNullOrEmpty(email) && emailVerified)
                            {
                                var existing = await userRepo.GetByEmailAsync(email);

                                // A locked or disabled account is never linked: the row keeps its
                                // placeholder UID until an admin reactivates it. The access gate would
                                // block the request anyway, this just avoids writing to a dead account.
                                if (existing is not null && existing.Status == UserStatus.Active)
                                {
                                    existing.LinkFirebaseAccount(firebaseUid);
                                    await userRepo.UpdateAsync(existing);
                                    await context.HttpContext.RequestServices
                                        .GetRequiredService<IUnitOfWork>()
                                        .SaveChangesAsync();

                                    // Best-effort: push roles to Firebase so later tokens carry custom claims.
                                    try
                                    {
                                        var firebaseAuth = context.HttpContext.RequestServices
                                            .GetService<IFirebaseAuthService>();
                                        var existingUserRoles = existing.GetActiveRoles();
                                        if (firebaseAuth is not null)
                                            await firebaseAuth.SetCustomClaimsAsync(firebaseUid, new Dictionary<string, object>
                                            {
                                                ["dbUserId"] = existing.Id.ToString(),
                                                ["roles"] = existing.GetActiveRoles().ToArray()
                                            });
                                    }
                                    catch { /* claims sync is best-effort */ }

                                    user = existing;
                                }
                                else if (existing is null)
                                {
                                    // Nobody owns this verified FPT address yet: create the account
                                    // on the spot so a first-time signer-in is not dead-ended on a
                                    // 401 with no way to self-register.
                                    //
                                    // It is deliberately provisioned with the LEAST privileged role.
                                    // That is safe because the account still has to pass
                                    // AccountAccessGate: a Student who is not on the semester's
                                    // eligible roster is rejected with NOT_ELIGIBLE, so this creates
                                    // a row without granting access to anything.
                                    user = await ProvisionFromGoogleAsync(
                                        context.HttpContext.RequestServices,
                                        userRepo,
                                        firebaseUid,
                                        email,
                                        context.Principal);
                                }
                            }
                        }

                        if (user is null)
                        {
                            // Valid Firebase token, but no TEDF account owns it. Failing here returns a
                            // 401 instead of letting the request run as Anonymous and making every
                            // handler throw "User is not authenticated".
                            context.Fail("No TEDF account is linked to this Firebase user.");
                            return;
                        }

                        var identity = context.Principal!.Identity as ClaimsIdentity;
                        identity?.AddClaim(new Claim(AppClaimTypes.DbUserId, user.Id.ToString()));

                        // Inject FullName so CurrentUserService.FullName works
                        if (!string.IsNullOrWhiteSpace(user.FullName))
                            identity?.AddClaim(new Claim(ClaimTypes.Name, user.FullName));

                        // Inject active roles so CurrentUserService.Roles works
                        foreach (var role in user.GetActiveRoles())
                            identity?.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                };
            });

            // Authorization
            services.AddAuthorizationPolicies();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, ProjectOwnerAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, GroupMemberAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, GroupLeaderAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, MentorOfProjectAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, DepartmentHeadOfDepartmentAuthorizationHandler>();
            services.AddScoped<IAccessControlService, AccessControlService>();

            // Firebase Auth Service
            services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();
            services.AddScoped<IAuthAccountService, FirebaseAuthService>();

            // Domain Services
            services.AddScoped<IProjectsDomainService, ProjectsDomainService>();
            services.AddScoped<IEvaluationsDomainService, EvaluationsDomainService>();
            services.AddScoped<ChecklistRepositories>();
            services.AddScoped<IChecklistDomainService, ChecklistDomainService>();
            services.AddScoped<IChecklistExcelService, ChecklistExcelService>();
            services.AddScoped<ITopicPoolsDomainService, TopicPoolsDomainService>();
            services.AddScoped<ISemestersDomainService, SemestersDomainService>();
            services.AddScoped<IStudentGroupsDomainService, StudentGroupsDomainService>();
            services.AddScoped<IUsersDomainService, UsersDomainService>();
            services.AddScoped<ISettingsDomainService, SettingsDomainService>();
            services.AddScoped<ISupportsDomainService, SupportsDomainService>();
            services.AddScoped<IArchivesDomainService, ArchivesDomainService>();
            services.AddScoped<ITopicsDomainService, TopicsDomainService>();
            services.AddScoped<IDirectTopicsDomainService, DirectTopicsDomainService>();
            services.AddScoped<IDashboardDomainService, DashboardDomainService>();
            services.AddScoped<INotificationsDomainService, NotificationsDomainService>();
            services.AddScoped<IAuthenticationsDomainService, AuthenticationsDomainService>();

            // Evaluation Services
            services.AddScoped<ITitleSimilarityService, TitleSimilarityService>();

            // Email — SMTP (admin "send test email" only)
            services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IEmailSender, EmailSenderAdapter>();

            // Email — transactional mail via the Firestore "Trigger Email" extension.
            // Singleton so the Firestore channel is built once and reused across jobs.
            services.Configure<FirestoreMailOptions>(configuration.GetSection(FirestoreMailOptions.SectionName));
            services.AddSingleton<IFirestoreMailQueue, FirestoreMailQueue>();
            services.AddScoped<IProjectMailContextFactory, ProjectMailContextFactory>();

            // File Storage - Firebase Storage
            services.Configure<FileStorageSettings>(configuration.GetSection(FileStorageSettings.SectionName));
            services.AddScoped<IFileStorageService, FirebaseStorageService>();
            services.AddScoped<IExcelService, ExcelService>();

            // Notification & RealTime
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();

            // Caching - L1 (Memory) + L2 (Redis) Hybrid
            services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));
            services.AddMemoryCache();
            services.AddSingleton<MemoryCacheService>(); // L1 - concrete registration for HybridCacheService

            var cacheSettings = configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>();
            if (!string.IsNullOrEmpty(cacheSettings?.RedisConnectionString))
            {
                // Redis available → register L2 + Hybrid + pub/sub listener
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(cacheSettings.RedisConnectionString));
                services.AddSingleton<RedisCacheService>();                          // L2
                services.AddSingleton<ICacheService, HybridCacheService>();          // Hybrid = L1 + L2
                services.AddHostedService<RedisCacheInvalidationListener>();         // Cross-instance L1 sync
            }
            else
            {
                // No Redis → fallback to Memory only (dev environment)
                services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<MemoryCacheService>());
            }

            services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();

            // Background Jobs
            services.AddScoped<IBackgroundJobService, HangfireJobService>();
            services.AddScoped<TopicExpirationJob>();
            services.AddScoped<EvaluationReminderJob>();
            services.AddScoped<SemesterPhaseTransitionJob>();
            services.AddScoped<DefenseScheduleReminderJob>();
            services.AddScoped<MeetingReminderJob>();
            services.AddScoped<GroupJoinRequestExpirationJob>();
            services.AddScoped<DataCleanupJob>();
            services.AddScoped<SendRosterPublishedMailJob>();
            services.AddScoped<MailDispatchJob>();

            var hangfireConn = configuration.GetConnectionString("HangfireConnection") ?? configuration.GetConnectionString("DefaultConnection");
            services.AddHangfire(c => c.SetDataCompatibilityLevel(CompatibilityLevel.Version_180).UseSimpleAssemblyNameTypeSerializer().UseRecommendedSerializerSettings().UseSqlServerStorage(hangfireConn));
            services.AddHangfireServer();

            // MediatR - Register Infrastructure Handlers (like Domain Event Handlers)
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

            return services;
        }

        public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<PerformanceMonitoringMiddleware>();
            app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = [new HangfireAuthFilter()] });
            RecurringJobsConfiguration.ConfigureRecurringJobs();
            return app;
        }

        /// <summary>
        /// Creates a TEDF account for a Google user who signed in successfully but has no row yet.
        /// Returns null when the address is not an FPT one, so outsiders cannot self-register.
        /// </summary>
        /// <remarks>
        /// Only the <c>Users</c> + <c>UserRoles</c> rows are written — no <c>Students</c> profile,
        /// because a Google token carries no student code and that column is unique and required.
        /// Note this means a later roster import will report the row as an issue instead of
        /// completing it (<c>TryProvisionUserAsync</c> skips addresses that already exist).
        /// </remarks>
        private static async Task<User?> ProvisionFromGoogleAsync(
            IServiceProvider services,
            IUserRepository userRepo,
            string firebaseUid,
            string email,
            ClaimsPrincipal? principal)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Checked here rather than relying on Email.Create throwing: a non-FPT sign-in is an
            // expected outcome, not an exceptional one. The `hd` hint set by the SPA is only a UI
            // filter and can be bypassed, so this is the real gate.
            if (!normalizedEmail.EndsWith($"@{Email.AllowedDomain}", StringComparison.OrdinalIgnoreCase))
                return null;

            var fullName = principal?.FindFirstValue("name")
                ?? principal?.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = normalizedEmail.Split('@')[0];

            var user = User.Create(
                firebaseUid: firebaseUid,   // real UID — no "pending:" placeholder needed here
                email: normalizedEmail,
                fullName: fullName.Trim(),
                avatarUrl: principal?.FindFirstValue("picture"));

            user.AssignRole(DomainRoleIds.Student, DomainRoleNames.Student);

            await userRepo.AddAsync(user);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

            // Firebase custom claims are pushed by SyncFirebaseClaimsOnUserCreatedHandler /
            // SyncFirebaseClaimsOnRoleAssignedHandler once the domain events dispatch after save.
            return user;
        }

        private static GoogleCredential BuildFirebaseCredential(FirebaseSettings settings)
        {
            if (settings.UseEmulator)
            {
                return GoogleCredential.FromAccessToken("emulator-fake-token");
            }

            var serviceAccountPath = ResolveServiceAccountPath(settings.ServiceAccountKeyPath);
            if (string.IsNullOrWhiteSpace(serviceAccountPath))
            {
                return GoogleCredential.GetApplicationDefault();
            }

            return CredentialFactory
                .FromFile<ServiceAccountCredential>(serviceAccountPath)
                .ToGoogleCredential();
        }

        private static string ResolveServiceAccountPath(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            var fromContentRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath));
            if (File.Exists(fromContentRoot))
            {
                return fromContentRoot;
            }

            var fromBaseDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
            if (File.Exists(fromBaseDirectory))
            {
                return fromBaseDirectory;
            }

            return fromContentRoot;
        }
    }

    /// <summary>
    /// Hangfire dashboard authorization: only authenticated Admin users may access.
    /// </summary>
    public sealed class HangfireAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("Admin");
        }
    }
}
