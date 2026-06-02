namespace TEDF.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    /// <summary>
    /// Sets a value with separate L1 and L2 TTLs.
    /// Used by CachingBehavior to honor per-query TTL overrides.
    /// Non-hybrid implementations can ignore l1Expiration and just use l2Expiration.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan l1Expiration, TimeSpan l2Expiration, CancellationToken ct = default)
        => SetAsync(key, value, l2Expiration, ct); // Default: just use L2 TTL

    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}
