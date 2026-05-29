namespace TEDF.Application.Common.Abstractions;

/// <summary>
/// Marker interface for queries that should be cached via L1/L2 caching pipeline.
/// Implement this on query records to opt-in to caching.
/// Use {userId} placeholder in CacheKey for per-user cache keys.
/// </summary>
public interface ICachedQuery<TResponse> : IQuery<TResponse>
{
    /// <summary>
    /// Cache key for this query. Use {userId} placeholder for per-user keys.
    /// Return null to skip caching (e.g. when search parameter is present).
    /// Example: "evaluator:{userId}:dashboard"
    /// </summary>
    string? CacheKey { get; }

    /// <summary>L1 (in-memory) TTL. Default: 2 minutes.</summary>
    TimeSpan? L1Expiration => TimeSpan.FromMinutes(2);

    /// <summary>L2 (Redis) TTL. Default: 15 minutes.</summary>
    TimeSpan? L2Expiration => TimeSpan.FromMinutes(15);
}
