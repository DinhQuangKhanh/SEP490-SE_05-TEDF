using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Topics.DTOs;

namespace TEDF.Application.Features.Topics.Queries.GetTopicsInPool;

/// <summary>
/// Query to get a paginated list of topics available in the topic pool with optional filters.
/// Implements ICachedQuery with L1/L2 caching — search queries bypass cache.
/// </summary>
public record GetTopicsInPoolQuery(
    int? MajorId,
    string? Search,
    int? PoolStatus,
    string? SortBy,
    int Page = 1,
    int PageSize = 12
) : ICachedQuery<GetTopicsInPoolResult>
{
    /// <summary>
    /// Cache key based on filter combination. Returns null when searching to avoid cache key explosion.
    /// </summary>
    public string? CacheKey => string.IsNullOrWhiteSpace(Search)
        ? $"topics:in-pool:major:{MajorId ?? 0}:status:{PoolStatus ?? -1}:sort:{SortBy ?? "newest"}:p:{Page}:s:{PageSize}"
        : null;

    public TimeSpan? L1Expiration => TimeSpan.FromMinutes(2);
    public TimeSpan? L2Expiration => TimeSpan.FromMinutes(10);
}
