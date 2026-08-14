using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using StackExchange.Redis;
using TEDF.Infrastructure.Caching;
using TEDF.API.Common.Security.Abstractions;
using TEDF.API.Common.Security.Jobs;
using TEDF.API.Common.Security.Options;
using TEDF.API.Common.Security.Services;
using TEDF.Application.Common;
using TEDF.Infrastructure.HealthChecks;

namespace TEDF.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 25 * 1024 * 1024;
        });

        services.AddRequestTimeouts(options =>
        {
            options.AddPolicy("ProposeTopicUploadTimeout", TimeSpan.FromSeconds(60));
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("ProposeTopicUploadPolicy", httpContext =>
            {
                var dbUserId = httpContext.User.FindFirst(AppClaimTypes.DbUserId)?.Value;
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var key = !string.IsNullOrWhiteSpace(dbUserId) ? $"user:{dbUserId}" : $"ip:{ip}";

                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2,
                    AutoReplenishment = true,
                });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                var payload = ApiResponse.Fail("Bạn gửi yêu cầu quá nhanh. Vui lòng thử lại sau.");
                var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
                await context.HttpContext.Response.WriteAsync(json, token);
            };
        });

        services.Configure<MalwareScanOptions>(configuration.GetSection(MalwareScanOptions.SectionName));
        services.AddScoped<IMalwareScanAuditLogger, MalwareScanAuditLogger>();
        services.AddScoped<IAttachmentScanAuditService, AttachmentScanAuditService>();
        services.AddScoped<IClamAvMalwareScanClient, ClamAvMalwareScanClient>();
        services.AddScoped<IMalwareScanner, MalwareScanner>();
        services.AddScoped<IAttachmentQuarantineService, AttachmentQuarantineService>();
        services.AddScoped<IAttachmentScanNotificationService, AttachmentScanNotificationService>();
        services.AddScoped<IAttachmentPromotionService, AttachmentPromotionService>();
        services.AddScoped<IAttachmentScanProcessingService, AttachmentScanProcessingService>();
        services.AddScoped<IAttachmentScanWorkflow, AttachmentScanWorkflow>();

        return services;
    }

    public static IServiceCollection AddJobServices(this IServiceCollection services)
    {
        services.AddScoped<AttachmentScanJob>();
        services.AddScoped<QuarantineRetryJob>();

        return services;
    }

    /// <summary>
    /// Registers SignalR and, when Redis is configured, the Redis backplane.
    /// </summary>
    /// <remarks>
    /// Without a backplane each API instance only knows about the clients connected to itself, so a
    /// notification raised on instance A never reaches a user whose browser happens to hold its socket on
    /// instance B. That failure is silent and load-dependent — realtime looks "flaky" rather than broken —
    /// so wire the backplane up front instead of after the first scale-out.
    ///
    /// Reuses <see cref="CacheSettings.RedisConnectionString"/> (the same Redis the hybrid cache uses) and
    /// mirrors its fallback: no connection string configured → plain in-memory SignalR, which is correct for
    /// a single instance and for local runs without Redis. SignalR keys its own channels, so it does not
    /// collide with the cache's <c>cache:invalidate</c> pub/sub channel.
    /// </remarks>
    public static IServiceCollection AddSignalRServices(this IServiceCollection services, IConfiguration configuration)
    {
        var signalR = services.AddSignalR();

        var redisConnectionString = configuration
            .GetSection(CacheSettings.SectionName)
            .Get<CacheSettings>()?
            .RedisConnectionString;

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            signalR.AddStackExchangeRedis(redisConnectionString, options =>
            {
                // Namespaced so the backplane's keys stay distinguishable from the cache's in the same
                // Redis instance.
                options.Configuration.ChannelPrefix = RedisChannel.Literal("tedf:signalr");
            });
        }

        return services;
    }

    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("sqlserver")
            .AddCheck<MongoDbHealthCheck>("mongodb");

        return services;
    }
}