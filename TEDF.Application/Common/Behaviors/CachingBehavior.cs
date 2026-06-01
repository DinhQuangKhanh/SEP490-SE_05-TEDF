using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that provides L1/L2 caching for queries implementing ICachedQuery.
/// Sits in the pipeline between LoggingBehavior and ValidationBehavior.
/// On cache hit, returns cached result and short-circuits the pipeline.
/// On cache miss, executes the handler and stores the result in both L1 and L2.
/// Includes stampede protection via per-key locking.
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery<TResponse>
{
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    /// <summary>Per-key locks to prevent cache stampede (thundering herd).</summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public CachingBehavior(
        ICacheService cacheService,
        ICurrentUserService currentUserService,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(request);

        // Skip caching if key is null (e.g. search queries that shouldn't be cached)
        if (cacheKey is null)
            return await next();

        var requestName = typeof(TRequest).Name;

        // Try to get from cache (L1 → L2)
        var cached = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {RequestName}: {CacheKey}", requestName, cacheKey);
            return cached;
        }

        // Cache miss → use per-key lock to prevent stampede
        var keyLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await keyLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check: another thread may have populated cache while we waited
            cached = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit (after lock) for {RequestName}: {CacheKey}", requestName, cacheKey);
                return cached;
            }

            _logger.LogDebug("Cache miss for {RequestName}: {CacheKey}", requestName, cacheKey);

            // Execute handler (only 1 thread per key reaches here)
            var response = await next();

            // Store in cache with per-query L1 and L2 TTLs
            var l1Expiration = request.L1Expiration ?? TimeSpan.FromMinutes(2);
            var l2Expiration = request.L2Expiration ?? TimeSpan.FromMinutes(15);

            await _cacheService.SetAsync(cacheKey, response, l1Expiration, l2Expiration, cancellationToken);

            _logger.LogDebug(
                "Cached {RequestName}: {CacheKey}, L1 TTL: {L1TTL}, L2 TTL: {L2TTL}",
                requestName, cacheKey, l1Expiration, l2Expiration);

            return response;
        }
        finally
        {
            keyLock.Release();

            // Clean up lock if no one is waiting (prevent memory leak)
            if (keyLock.CurrentCount == 1)
                _locks.TryRemove(cacheKey, out _);
        }
    }

    private string? BuildCacheKey(TRequest request)
    {
        var key = request.CacheKey;

        // Null key = skip caching (e.g. search queries)
        if (key is null) return null;

        // Replace {userId} placeholder with actual user ID
        if (key.Contains("{userId}"))
        {
            var userId = _currentUserService.UserId?.ToString() ?? "anonymous";
            key = key.Replace("{userId}", userId);
        }

        return key;
    }
}
