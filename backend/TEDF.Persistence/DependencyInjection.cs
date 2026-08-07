using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using TEDF.Persistence.MongoDB;
using TEDF.Persistence.MongoDB.Indexes;
using TEDF.Persistence.MongoDB.Repositories.Implementation;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;
using TEDF.Persistence.MongoDB.Serializers;
using TEDF.Persistence.Seeds;
using TEDF.Persistence.Services;
using TEDF.Persistence.SqlServer;
using TEDF.Persistence.SqlServer.Interceptors;
using TEDF.Persistence.SqlServer.QueryServices;
using TEDF.Persistence.SqlServer.Repositories;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;
using IDateTimeService = TEDF.Application.Common.Interfaces.IDateTimeService;

namespace TEDF.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure MongoDB serializers (thread-safe, idempotent)
            MongoSerializerConfiguration.Configure();
            // Add HttpContextAccessor for CurrentUserService
            services.AddHttpContextAccessor();

            // Add Core Services
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<IDateTimeService, DateTimeService>();

            // Add Interceptors
            services.AddScoped<AuditableEntityInterceptor>();
            services.AddScoped<SoftDeleteInterceptor>();
            services.AddScoped<ProjectAuditLogInterceptor>();
            services.AddScoped<DomainEventInterceptor>();

            // Add SQL Server DbContext
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    b => { b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName); b.EnableRetryOnFailure(3); });
                options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>(),
                    sp.GetRequiredService<ProjectAuditLogInterceptor>(),
                    sp.GetRequiredService<DomainEventInterceptor>());
            });

            // Add MongoDB
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));
            services.AddSingleton<IMongoClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                return new MongoClient(settings.ConnectionString);
            });

            services.AddSingleton<MongoDbContext>();

            // Add Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Add SQL Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IProjectDocumentWriteService, ProjectDocumentWriteService>();
            services.AddScoped<ITopicPoolRepository, TopicPoolRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<ISemesterRepository, SemesterRepository>();
            services.AddScoped<IEvaluationSubmissionRepository, EvaluationSubmissionRepository>();
            services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
            services.AddScoped<ITopicRegistrationRepository, TopicRegistrationRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IMajorReadRepository, MajorRepository>();
            services.AddScoped<IProjectEvaluatorAssignmentRepository, ProjectEvaluatorAssignmentRepository>();
            services.AddScoped<IChecklistConfigRepository, ChecklistConfigRepository>();
            services.AddScoped<IProjectEvaluationChecklistRepository, ProjectEvaluationChecklistRepository>();
            services.AddScoped<ISystemConfigurationRepository, SystemConfigurationRepository>();
            services.AddScoped<IProjectArchiveRepository, ProjectArchiveRepository>();

            // System settings (cached read access over SystemConfiguration)
            services.AddScoped<ISystemSettingsService, SystemSettingsService>();

            // Add Query Services
            services.AddScoped<IStudentGroupsQueryService, StudentGroupsQueryService>();
            services.AddScoped<IEvaluationsQueryService, EvaluationsQueryService>();
            services.AddScoped<IChecklistQueryService, ChecklistQueryService>();
            services.AddScoped<ITopicPoolsQueryService, TopicPoolsQueryService>();
            services.AddScoped<ITopicsQueryService, TopicsQueryService>();
            services.AddScoped<IDashboardQueryService, DashboardQueryService>();
            services.AddScoped<IProjectsQueryService, ProjectsQueryService>();
            services.AddScoped<IUsersQueryService, UsersQueryService>();
            services.AddScoped<ISemestersQueryService, SemestersQueryService>();
            services.AddScoped<ISettingsQueryService, SettingsQueryService>();
            services.AddScoped<ISupportsQueryService, SupportsQueryService>();
            services.AddScoped<IArchivesQueryService, ArchivesQueryService>();
            services.AddScoped<INotificationsQueryService, NotificationsQueryService>();
            services.AddScoped<IAuthenticationsQueryService, AuthenticationsQueryService>();

            // Add MongoDB Repositories
            services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
            services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
            services.AddScoped<ISystemAuditLogRepository, SystemAuditLogRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IQuarantinedAttachmentRepository, QuarantinedAttachmentRepository>();

            // Add Log Services
            services.AddScoped<IActivityLogService, ActivityLogService>();
            services.AddScoped<IErrorLogService, ErrorLogService>();

            return services;
        }

        /// <summary>
        /// Initializes the database with migrations and seeding.
        /// Call this method after building the WebApplication instance.
        /// <para>
        /// The seed data itself runs in <b>every</b> environment: it lands entirely in the three
        /// historical semesters (Fall 2025 / Spring 2026 / Summer 2026) and in fixed Id ranges, so
        /// semesters created later from the admin UI — and the real accounts working in them — are
        /// left alone. Two things stay Development-only:
        /// <list type="bullet">
        ///   <item><b>EF Core migrations</b> — schema changes on real data stay a deliberate act.</item>
        ///   <item><b>The destructive reset switch</b> (<c>TEDF_RESET_LOADTEST_ON_STARTUP</c>).</item>
        /// </list>
        /// The Firebase accounts for the seeded users are created in the Auth <i>Emulator</i> only,
        /// gated on <c>Firebase:UseEmulator</c> — never against a real Firebase project.
        /// </para>
        /// </summary>
        /// <param name="isDevelopment">
        /// Pass <c>app.Environment.IsDevelopment()</c>. Outside Development the database holds real
        /// data, so migrations and the reset switch are withheld and a seeding failure is logged
        /// instead of taking the API down.
        /// </param>
        /// <example>
        /// var app = builder.Build();
        /// await app.Services.InitializeDatabaseAsync(app.Environment.IsDevelopment());
        /// </example>
        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, bool isDevelopment)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInit");

            if (isDevelopment)
            {
                var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
                if (pendingMigrations.Count == 0)
                {
                    logger.LogInformation("No pending EF Core migrations.");
                }
                else
                {
                    logger.LogWarning("Applying {Count} pending migration(s): {Migrations}",
                        pendingMigrations.Count,
                        string.Join(", ", pendingMigrations));
                }

                await dbContext.Database.MigrateAsync();
                logger.LogInformation("EF Core migrations applied successfully.");
            }
            else
            {
                logger.LogInformation(
                    "Non-Development environment: skipping automatic EF Core migrations. Apply them out of band.");
            }

            try
            {
                // Reference data first: the full seeder short-circuits once its own users exist, so on
                // an already-seeded database Departments/Majors would otherwise never be topped up.
                await LoadTestDataSeeder.SeedEssentialAsync(dbContext, logger);

                // Full load-test dataset (users, semesters, groups, projects, registrations…).
                await LoadTestDataSeeder.SeedAsync(dbContext, logger, allowDestructiveReset: isDevelopment);

                // Sign-in accounts for those users exist in the Auth Emulator only. Running this
                // against a real Firebase project would create ~1000 usable accounts sharing one
                // password that is published in the source — hence the hard gate.
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                if (configuration.GetValue<bool>("Firebase:UseEmulator"))
                    await FirebaseEmulatorSeeder.SeedAsync(logger);

                // Ensure every existing semester has an Active evaluation checklist (idempotent, additive).
                await EvaluationChecklistSeeder.SeedAsync(dbContext, logger);

                var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
                await MongoIndexConfiguration.CreateIndexesAsync(mongoContext);
            }
            catch (Exception ex) when (!isDevelopment)
            {
                // Seeding is worth a loud error, not a crash loop: a half-seeded demo dataset still
                // beats an API that refuses to start and takes the whole site down with it.
                // In Development the exception propagates so the failure is not missed.
                logger.LogError(ex, "Database seeding failed. The API will start without the seeded data.");
            }
        }
    }
}
