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
using TEDF.Infrastructure.Services.Email;
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
                        if (string.IsNullOrEmpty(firebaseUid)) return;

                        var userRepo = context.HttpContext.RequestServices
                            .GetRequiredService<IUserRepository>();

                        var user = await userRepo.GetByFirebaseUidAsync(firebaseUid);
                        if (user is null) return;

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

            // Firebase Auth Service
            services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();
            services.AddScoped<IAuthAccountService, FirebaseAuthService>();

            // Domain Services
            services.AddScoped<IProjectsDomainService, ProjectsDomainService>();
            services.AddScoped<IEvaluationsDomainService, EvaluationsDomainService>();
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

            // Email
            services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IEmailSender, EmailSenderAdapter>();

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
