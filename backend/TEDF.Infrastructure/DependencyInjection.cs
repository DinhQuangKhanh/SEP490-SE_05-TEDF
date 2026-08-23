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

                // DEV-ONLY escape hatch (default OFF). When true, a still-pending imported account
                // (FirebaseUid == "pending:<code>", never claimed) may be linked to a new Firebase UID
                // even if the email is UNVERIFIED — so manually-created email/password mock accounts
                // can sign in at demo time. Keep this false in production: it weakens the
                // account-takeover guard below. Set via Auth__AllowUnverifiedPendingLink in .env.
                var allowUnverifiedPendingLink = configuration.GetValue<bool>("Auth:AllowUnverifiedPendingLink");

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
                            // with Google. Match on the email and either re-point that row to the real
                            // UID, or — if nobody owns the address yet — provision a new account.
                            var email = context.Principal?.FindFirstValue("email")
                                ?? context.Principal?.FindFirstValue(ClaimTypes.Email);
                            var emailVerified = string.Equals(
                                context.Principal?.FindFirstValue("email_verified"),
                                "true",
                                StringComparison.OrdinalIgnoreCase);

                            if (!string.IsNullOrEmpty(email))
                            {
                                var existing = await userRepo.GetByEmailAsync(email);

                                if (existing is null)
                                {
                                    // Nobody owns this address yet: create the account on the spot so a
                                    // first-time signer-in isn't dead-ended on a 401. The role comes from
                                    // the mail domain (see DefaultRoleForEmail): @fe.edu.vn -> Mentor,
                                    // otherwise Student.
                                    //
                                    // This is allowed even for an UNVERIFIED email (e.g. an email/password
                                    // account) because a brand-new account gains no access on its own —
                                    // AccountAccessGate still rejects a Student not on the semester's
                                    // eligible roster and a Mentor not on the assigned-lecturer roster.
                                    // (Takeover protection below only matters when LINKING an existing row.)
                                    user = await ProvisionFromFirebaseAsync(
                                        context.HttpContext.RequestServices,
                                        userRepo,
                                        firebaseUid,
                                        email,
                                        context.Principal);
                                }
                                else if (existing.Status == UserStatus.Active &&
                                         (emailVerified ||
                                          (allowUnverifiedPendingLink && existing.IsPendingActivation)))
                                {
                                    // Re-point an EXISTING active account to the real UID — but only when
                                    // Firebase reports the email verified, otherwise anyone able to create a
                                    // Firebase account with someone's FPT address could take that account
                                    // over. A locked/disabled row keeps its placeholder UID until an admin
                                    // reactivates it (the access gate would block it anyway).
                                    //
                                    // Exception: the DEV-ONLY Auth:AllowUnverifiedPendingLink flag lets a
                                    // still-pending (never-claimed) imported row link even with an unverified
                                    // email, so email/password mock accounts work at demo time. See the flag.
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
            services.AddScoped<ITopicProposalService, TopicProposalService>();
            services.AddScoped<ITopicRegistrationService, TopicRegistrationService>();
            services.AddScoped<IPoolLifecycleService, PoolLifecycleService>();
            services.AddScoped<ISemestersDomainService, SemestersDomainService>();
            services.AddScoped<IStudentGroupsDomainService, StudentGroupsDomainService>();
            services.AddScoped<IUsersDomainService, UsersDomainService>();
            services.AddScoped<ISettingsDomainService, SettingsDomainService>();
            services.AddScoped<ISupportsDomainService, SupportsDomainService>();
            services.AddScoped<IArchivesDomainService, ArchivesDomainService>();
            services.AddScoped<ITopicsDomainService, TopicsDomainService>();
            services.AddScoped<IDashboardDomainService, DashboardDomainService>();
            services.AddScoped<INotificationsDomainService, NotificationsDomainService>();
            services.AddScoped<IAuthenticationsDomainService, AuthenticationsDomainService>();

            // Evaluation Services
            services.AddScoped<ITitleSimilarityService, TitleSimilarityService>();

            // External Python (DASSF) similarity service — typed HTTP client.
            var similarityBaseUrl = configuration["SimilarityService:BaseUrl"] ?? "http://localhost:8000";
            services.AddHttpClient<ISimilarityApiClient, SimilarityApiClient>(client =>
            {
                client.BaseAddress = new Uri(similarityBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Email — every message leaves through the Firestore "Trigger Email" extension; the
            // backend never speaks SMTP itself. Singleton so the Firestore channel is built once
            // and reused across jobs.
            services.Configure<FirestoreMailOptions>(configuration.GetSection(FirestoreMailOptions.SectionName));
            services.AddSingleton<IFirestoreMailQueue, FirestoreMailQueue>();
            services.AddScoped<IProjectMailContextFactory, ProjectMailContextFactory>();
            services.AddScoped<IEmailSender, FirestoreEmailSender>();

            // File Storage - Firebase Storage
            services.Configure<FileStorageSettings>(configuration.GetSection(FileStorageSettings.SectionName));
            services.AddScoped<IFileStorageService, FirebaseStorageService>();
            services.AddScoped<IExcelService, ExcelService>();
            services.AddScoped<IRegisterFormParser, Services.RegisterForm.RegisterFormParser>();

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
        /// Creates a TEDF account for someone whose Firebase sign-in succeeded — Google or
        /// email/password, the token looks the same either way — but who has no row yet.
        /// Returns null when the address is not on an accepted domain, so outsiders cannot
        /// self-register.
        /// </summary>
        /// <remarks>
        /// Only the <c>Users</c> + <c>UserRoles</c> rows are written — no <c>Students</c> or
        /// <c>Lecturers</c> profile, because a Firebase token carries no student/employee code and
        /// those columns are unique and required. Note this means a later roster import will report
        /// the row as an issue instead of completing it (<c>TryProvisionUserAsync</c> skips
        /// addresses that already exist).
        /// </remarks>
        private static async Task<User?> ProvisionFromFirebaseAsync(
            IServiceProvider services,
            IUserRepository userRepo,
            string firebaseUid,
            string email,
            ClaimsPrincipal? principal)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Checked here rather than relying on Email.Create throwing: an address on an
            // unaccepted domain is an expected outcome, not an exceptional one. The `hd` hint set
            // by the SPA is only a UI filter and can be bypassed, so this is the real gate.
            if (!Email.IsAllowed(normalizedEmail))
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

            var (roleId, roleName) = DefaultRoleForEmail(normalizedEmail);
            user.AssignRole(roleId, roleName);

            await userRepo.AddAsync(user);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

            // Firebase custom claims are pushed by SyncFirebaseClaimsOnUserCreatedHandler /
            // SyncFirebaseClaimsOnRoleAssignedHandler once the domain events dispatch after save.
            return user;
        }

        /// <summary>
        /// Role a self-provisioned account starts with, decided by the mail domain: @fe.edu.vn is
        /// the lecturers' domain, every other accepted domain belongs to a student.
        /// </summary>
        /// <remarks>
        /// Either way the account gains no actual access on its own: <c>AccountAccessMiddleware</c>
        /// still rejects a Student who is not on the semester's eligible roster, and a Mentor who is
        /// not on the assigned-lecturer roster.
        /// </remarks>
        private static (int Id, string Name) DefaultRoleForEmail(string normalizedEmail)
        {
            return normalizedEmail.EndsWith("@fe.edu.vn", StringComparison.OrdinalIgnoreCase)
                ? (DomainRoleIds.Mentor, DomainRoleNames.Mentor)
                : (DomainRoleIds.Student, DomainRoleNames.Student);
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
