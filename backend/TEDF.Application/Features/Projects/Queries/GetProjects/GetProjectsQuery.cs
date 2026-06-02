using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetProjects;

/// <summary>
/// Query to get a paginated list of projects with optional filters.
/// Search queries bypass cache to avoid cache key explosion.
/// </summary>
public record GetProjectsQuery(
    string? Search,
    int? SemesterId,
    string? Status,
    int? MajorId,
    int Page = 1,
    int PageSize = 20
) : ICachedQuery<GetProjectsQueryResult>
{
    public string? CacheKey => string.IsNullOrWhiteSpace(Search)
        ? $"projects:list:sem:{SemesterId?.ToString() ?? "all"}:status:{Status ?? "all"}:major:{MajorId?.ToString() ?? "all"}:page:{Page}:size:{PageSize}"
        : null;

    public TimeSpan? L1Expiration => TimeSpan.FromMinutes(2);
    public TimeSpan? L2Expiration => TimeSpan.FromMinutes(10);
}
