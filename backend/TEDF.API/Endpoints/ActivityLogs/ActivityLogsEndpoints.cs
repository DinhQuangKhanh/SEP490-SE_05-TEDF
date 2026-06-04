using TEDF.Persistence.MongoDB.Repositories.Interfaces;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.ActivityLogs;

public sealed class ActivityLogsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/activity-logs")
            .RequireAuthorization(PolicyNames.RequireAdmin);

        group.MapGet("", GetUserActivityLogs)
            .WithTags("ActivityLogs")
            .WithName("GetUserActivityLogs")
            .Produces(200).Produces(401);

        group.MapGet("/grouped", GetGroupedActivityLogs)
            .WithTags("ActivityLogs")
            .WithName("GetGroupedActivityLogs")
            .Produces(200).Produces(401);

        group.MapGet("/severity-summary", GetSeveritySummary)
            .WithTags("ActivityLogs")
            .WithName("GetSeveritySummary")
            .Produces(200).Produces(401);

        group.MapGet("/errors/{id:guid}", GetErrorLogDetail)
            .WithTags("ActivityLogs")
            .WithName("GetErrorLogDetail")
            .Produces(200).Produces(401).Produces(404);

        group.MapGet("/errors", GetActivityLogErrorDetails)
            .WithTags("ActivityLogs")
            .WithName("GetActivityLogErrorDetails")
            .Produces(200).Produces(401);
    }

    private static async Task<IResult> GetUserActivityLogs(
        IUserActivityLogRepository repository,
        string? role, string? category, string? severity, string? search, DateTime? from, DateTime? to,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var (items, totalCount) = await repository.GetPagedAsync(role, category, severity, search, from, to, page, pageSize, ct);
        return Ok(new
        {
            Items = items.Select(i => new
            {
                i.Id,
                UserId = i.UserId.ToString(),
                i.UserName,
                i.UserEmail,
                i.ActiveRole,
                i.Action,
                i.Category,
                i.EntityType,
                EntityId = i.EntityId?.ToString(),
                i.Severity,
                i.IpAddress,
                i.Timestamp,
            }),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        });
    }

    private static async Task<IResult> GetGroupedActivityLogs(
        IUserActivityLogRepository repository,
        string? role, string? severity, string? search, DateTime? from, DateTime? to,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var (items, totalGroups, roleCounts) = await repository.GetGroupedAsync(role, severity, search, from, to, page, pageSize, ct);
        return Ok(new
        {
            Items = items.Select(i => new
            {
                UserId = i.UserId.ToString(),
                i.UserName,
                i.UserEmail,
                i.ActiveRole,
                i.Action,
                i.Category,
                i.TotalCount,
                i.LatestTimestamp,
                i.SeverityCounts,
            }),
            TotalGroups = totalGroups,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalGroups / pageSize),
            RoleCounts = roleCounts,
        });
    }

    private static async Task<IResult> GetSeveritySummary(
        IUserActivityLogRepository repository, string? role, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var summary = await repository.GetSeveritySummaryAsync(role, from, to, ct);
        return Ok(new
        {
            summary.Info,
            summary.Warning,
            summary.Error,
            summary.Critical,
            Total = summary.Info + summary.Warning + summary.Error + summary.Critical,
        });
    }

    private static async Task<IResult> GetErrorLogDetail(
        IErrorLogRepository repository, Guid id, CancellationToken ct = default)
    {
        var errorLog = await repository.GetByIdAsync(id, ct);
        if (errorLog is null) return Results.NotFound();
        return Ok(new
        {
            errorLog.Id,
            UserId = errorLog.UserId?.ToString(),
            errorLog.UserName,
            errorLog.UserEmail,
            errorLog.ActiveRole,
            errorLog.Severity,
            errorLog.Source,
            errorLog.ActionCode,
            Action = errorLog.Action,
            errorLog.RoutePath,
            errorLog.RequestPath,
            errorLog.RequestMethod,
            errorLog.ErrorMessage,
            errorLog.ErrorType,
            errorLog.StackTrace,
            InnerExceptions = errorLog.InnerExceptions.Select(ie => new { ie.Message, ie.Type, ie.StackTrace }),
            errorLog.CorrelationId,
            errorLog.Timestamp,
        });
    }

    private static async Task<IResult> GetActivityLogErrorDetails(
        IUserActivityLogRepository repository, Guid userId, string action, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var errors = await repository.GetErrorDetailsAsync(userId, action, from, to, ct);
        return Ok(new
        {
            Errors = errors.Select(e => new { e.Message, e.ErrorType, e.Count, e.LatestAt })
        });
    }
}
