using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate;
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
            services.AddScoped<DomainEventInterceptor>();

            // Add SQL Server DbContext
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    b => { b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName); b.EnableRetryOnFailure(3); });
                options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>(), sp.GetRequiredService<DomainEventInterceptor>());
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
            services.AddScoped<ISystemConfigurationRepository, SystemConfigurationRepository>();
            services.AddScoped<IProjectArchiveRepository, ProjectArchiveRepository>();

            // System settings (cached read access over SystemConfiguration)
            services.AddScoped<ISystemSettingsService, SystemSettingsService>();

            // Add Query Services
            services.AddScoped<IStudentGroupsQueryService, StudentGroupsQueryService>();
            services.AddScoped<IEvaluationsQueryService, EvaluationsQueryService>();
            services.AddScoped<ITopicPoolsQueryService, TopicPoolsQueryService>();
            services.AddScoped<ITopicsQueryService, TopicsQueryService>();
            services.AddScoped<IDashboardQueryService, DashboardQueryService>();
            services.AddScoped<IProjectsQueryService, ProjectsQueryService>();
            services.AddScoped<IUsersQueryService, UsersQueryService>();
            services.AddScoped<ISemestersQueryService, SemestersQueryService>();
            services.AddScoped<ISettingsQueryService, SettingsQueryService>();
            services.AddScoped<ISupportsQueryService, SupportsQueryService>();
            services.AddScoped<IArchivesQueryService, ArchivesQueryService>();
            services.AddScoped<IDirectTopicsQueryService, DirectTopicsQueryService>();
            services.AddScoped<INotificationsQueryService, NotificationsQueryService>();
            services.AddScoped<IAuthenticationsQueryService, AuthenticationsQueryService>();

            // Add MongoDB Repositories
            services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
            services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
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
        /// </summary>
        /// <example>
        /// var app = builder.Build();
        /// await app.Services.InitializeDatabaseAsync();
        /// </example>
        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInit");

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

            // Seed development data (idempotent - skips if data already exists)
            //await DevelopmentDataSeeder.SeedAsync(dbContext);

            // Load-test data: seed 1000 users + relationships when Firebase Emulator is enabled
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var useEmulator = configuration.GetValue<bool>("Firebase:UseEmulator");

            if (useEmulator)
            {
                await LoadTestDataSeeder.SeedAsync(dbContext, logger);
                await FirebaseEmulatorSeeder.SeedAsync(logger);
            }

            var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            await MongoIndexConfiguration.CreateIndexesAsync(mongoContext);
        }
    }
}
