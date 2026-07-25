using TEDF.API.Endpoints.ActivityLogs.Requests;
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

        group.MapGet("", GetActivityLogs)
            .WithTags("ActivityLogs")
            .WithName("GetActivityLogs")
            .Produces(200).Produces(401);

        group.MapGet("/summary", GetActivityLogsSummary)
            .WithTags("ActivityLogs")
            .WithName("GetActivityLogsSummary")
            .Produces(200).Produces(401);

        group.MapGet("/errors", GetErrorLogs)
            .WithTags("ActivityLogs")
            .WithName("GetErrorLogs")
            .Produces(200).Produces(401);

        group.MapGet("/errors/{id:guid}", GetErrorLogDetail)
            .WithTags("ActivityLogs")
            .WithName("GetErrorLogDetail")
            .Produces(200).Produces(401).Produces(404);

        group.MapDelete("", DeleteActivityLogs)
            .WithTags("ActivityLogs")
            .WithName("DeleteActivityLogs")
            .Produces(200).Produces(401);

        group.MapDelete("/errors", DeleteErrorLogs)
            .WithTags("ActivityLogs")
            .WithName("DeleteErrorLogs")
            .Produces(200).Produces(401);
    }

    /// <summary>Clamp incoming paging values; unbound query params arrive as 0.</summary>
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
        => (page < 1 ? 1 : page, pageSize is < 1 or > 100 ? 20 : pageSize);

    private static async Task<IResult> GetActivityLogs(
        IActivityLogRepository repository,
        [AsParameters] GetActivityLogsRequest request,
        CancellationToken ct)
    {
        var (page, pageSize) = NormalizePaging(request.Page, request.PageSize);

        var filter = new ActivityLogFilter(
            request.Role, request.FeatureCategory, request.Status, request.Search,
            request.From, request.To, page, pageSize);

        var (items, totalCount) = await repository.GetPagedAsync(filter, ct);
        return Ok(new
        {
            Items = items.Select(i => new
            {
                i.Id,
                UserId = i.UserId.ToString(),
                i.UserName,
                i.UserEmail,
                i.Role,
                i.ActionCode,
                i.ActionName,
                i.FeatureCategory,
                i.RequestPath,
                i.RequestMethod,
                i.EntityType,
                EntityId = i.EntityId?.ToString(),
                i.Status,
                i.DurationMs,
                i.CorrelationId,
                i.IpAddress,
                i.Timestamp,
            }),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        });
    }

    private static async Task<IResult> GetActivityLogsSummary(
        IActivityLogRepository repository,
        string? role, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var roleCounts = await repository.GetRoleCountsAsync(from, to, ct);
        var (success, failure) = await repository.GetStatusCountsAsync(role, from, to, ct);
        return Ok(new
        {
            RoleCounts = roleCounts,
            Success = success,
            Failure = failure,
            Total = success + failure,
        });
    }

    private static async Task<IResult> GetErrorLogs(
        IErrorLogRepository repository,
        [AsParameters] GetErrorLogsRequest request,
        CancellationToken ct)
    {
        var (page, pageSize) = NormalizePaging(request.Page, request.PageSize);

        var filter = new ErrorLogFilter(
            request.Severity, request.Source, request.Search,
            request.From, request.To, page, pageSize);

        var (items, totalCount) = await repository.GetPagedAsync(filter, ct);
        return Ok(new
        {
            Items = items.Select(i => new
            {
                i.Id,
                UserId = i.UserId,
                i.UserName,
                i.ActiveRole,
                i.Severity,
                i.Source,
                i.ActionCode,
                i.RequestPath,
                i.RequestMethod,
                i.ErrorMessage,
                i.ErrorType,
                i.CorrelationId,
                i.Timestamp,
            }),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        });
    }

    private static async Task<IResult> DeleteActivityLogs(
        IActivityLogRepository repository,
        int? olderThanDays, CancellationToken ct = default)
    {
        DateTime? cutoff = olderThanDays is > 0 ? DateTime.UtcNow.AddDays(-olderThanDays.Value) : null;
        var deleted = await repository.DeleteOlderThanAsync(cutoff, ct);
        return Ok(new { DeletedCount = deleted });
    }

    private static async Task<IResult> DeleteErrorLogs(
        IErrorLogRepository repository,
        int? olderThanDays, CancellationToken ct = default)
    {
        DateTime? cutoff = olderThanDays is > 0 ? DateTime.UtcNow.AddDays(-olderThanDays.Value) : null;
        var deleted = await repository.DeleteOlderThanAsync(cutoff, ct);
        return Ok(new { DeletedCount = deleted });
    }

    private static async Task<IResult> GetErrorLogDetail(
        IErrorLogRepository repository, Guid id, CancellationToken ct = default)
    {
        var log = await repository.GetByIdAsync(id, ct);
        if (log is null) return Results.NotFound();
        return Ok(new
        {
            log.Id,
            UserId = log.UserId,
            log.UserName,
            log.UserEmail,
            log.ActiveRole,
            log.Severity,
            log.Source,
            log.ActionCode,
            Action = log.Action,
            log.RoutePath,
            log.RequestPath,
            log.RequestMethod,
            log.ErrorMessage,
            log.ErrorType,
            log.StackTrace,
            InnerExceptions = log.InnerExceptions.Select(ie => new { ie.Message, ie.Type, ie.StackTrace }),
            log.CorrelationId,
            log.Timestamp,
        });
    }
}
