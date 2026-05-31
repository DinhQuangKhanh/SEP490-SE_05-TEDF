using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Users.DTOs;

namespace TEDF.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Query to get a paginated list of users with optional role and search filters.
/// Implements ICachedQuery to leverage L1/L2 caching pipeline.
/// Search queries bypass cache to avoid cache key explosion.
/// </summary>
public record GetUsersQuery(
    string? Role,
    string? Search,
    int Page = 1,
    int PageSize = 20
) : ICachedQuery<GetUsersQueryResult>
{
    public string? CacheKey => string.IsNullOrWhiteSpace(Search)
        ? $"users:list:role:{Role ?? "all"}:page:{Page}:size:{PageSize}"
        : null;

    public TimeSpan? L1Expiration => TimeSpan.FromMinutes(2);
    public TimeSpan? L2Expiration => TimeSpan.FromMinutes(10);
}
