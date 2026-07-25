using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(
        IActivityLogRepository repository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ActivityLogService> logger)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(ActivityLogEntry entry, CancellationToken ct = default)
    {
        try
        {
            _ = Guid.TryParse(entry.UserId, out var userId);

            var http = _httpContextAccessor.HttpContext;
            var routePath  = http?.Request.Headers["X-Route-Path"].ToString();
            var ipAddress  = http?.Connection.RemoteIpAddress?.ToString();
            var userAgent  = http?.Request.Headers["User-Agent"].ToString();
            var correlationId = entry.CorrelationId ?? http?.TraceIdentifier;
            var requestPath   = http?.Request.Path.Value ?? string.Empty;
            var requestMethod = http?.Request.Method ?? string.Empty;

            var document = new ActivityLogDocument
            {
                UserId        = userId,
                UserName      = entry.UserName ?? entry.UserEmail ?? "Anonymous",
                UserEmail     = entry.UserEmail,
                Role          = entry.Role ?? ResolveRole(requestPath),
                ActionCode    = entry.ActionCode,
                ActionName    = entry.ActionName,
                FeatureCategory = entry.FeatureCategory,
                RoutePath     = string.IsNullOrEmpty(routePath) ? null : routePath,
                RequestPath   = requestPath,
                RequestMethod = requestMethod,
                EntityType    = entry.EntityType,
                EntityId      = entry.EntityId,
                Status        = entry.IsSuccess ? "Success" : "Failure",
                DurationMs    = entry.DurationMs,
                CorrelationId = correlationId,
                IpAddress     = ipAddress,
                UserAgent     = userAgent,
                Timestamp     = entry.Timestamp,
            };

            await _repository.AddAsync(document, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist activity log for {ActionCode}", entry.ActionCode);
        }
    }

    private static string ResolveRole(string path)
    {
        if (path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase))           return "admin";
        if (path.StartsWith("/api/mentor/", StringComparison.OrdinalIgnoreCase))          return "mentor";
        if (path.StartsWith("/api/evaluator/", StringComparison.OrdinalIgnoreCase))       return "evaluator";
        if (path.StartsWith("/api/student/", StringComparison.OrdinalIgnoreCase))         return "student";
        if (path.StartsWith("/api/department-head/", StringComparison.OrdinalIgnoreCase)) return "department-head";
        return "anonymous";
    }
}
