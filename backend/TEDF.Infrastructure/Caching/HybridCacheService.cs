using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Caching
{
    /// <summary>
    /// Two-level cache: L1 (MemoryCacheService) + L2 (RedisCacheService).
    /// Flow: L1 → L2 → source. On L2 hit, backfills L1.
    /// Falls back gracefully to L1-only if Redis is unavailable.
    /// </summary>
    public class HybridCacheService : ICacheService
    {
        private readonly MemoryCacheService _l1;
        private readonly RedisCacheService _l2;
        private readonly CacheSettings _settings;
        private readonly ILogger<HybridCacheService> _logger;

        /// <summary>Unique ID for this instance, used to skip self-invalidation in pub/sub.</summary>
        internal static readonly string InstanceId = Guid.NewGuid().ToString("N")[..8];

        public HybridCacheService(
            MemoryCacheService l1,
            RedisCacheService l2,
            IOptions<CacheSettings> settings,
            ILogger<HybridCacheService> logger)
        {
            _l1 = l1;
            _l2 = l2;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            // Check L1 first (fastest)
            var l1Value = await _l1.GetAsync<T>(key, ct);
            if (l1Value is not null)
            {
                _logger.LogDebug("Cache L1 hit: {Key}", key);
                return l1Value;
            }

            // Check L2
            var l2Value = await _l2.GetAsync<T>(key, ct);
            if (l2Value is not null)
            {
                _logger.LogDebug("Cache L2 hit: {Key}, backfilling L1", key);
                // Backfill L1 with default TTL (per-query TTL only available at Set time)
                await _l1.SetAsync(key, l2Value, TimeSpan.FromMinutes(_settings.L1ExpirationMinutes), ct);
                return l2Value;
            }

            _logger.LogDebug("Cache miss (L1+L2): {Key}", key);
            return default;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            var l1Ttl = TimeSpan.FromMinutes(_settings.L1ExpirationMinutes);
            var l2Ttl = expiration ?? TimeSpan.FromMinutes(_settings.L2ExpirationMinutes);

            // Write to both levels
            await _l1.SetAsync(key, value, l1Ttl, ct);
            await _l2.SetAsync(key, value, l2Ttl, ct);
        }

        /// <summary>
        /// Sets a value in both L1 and L2 with separate TTLs.
        /// Used by CachingBehavior to honor per-query TTL overrides.
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan l1Expiration, TimeSpan l2Expiration, CancellationToken ct = default)
        {
            await _l1.SetAsync(key, value, l1Expiration, ct);
            await _l2.SetAsync(key, value, l2Expiration, ct);
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default)
        {
            await _l1.RemoveAsync(key, ct);
            await _l2.RemoveAsync(key, ct);
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        {
            await _l1.RemoveByPrefixAsync(prefix, ct);
            await _l2.RemoveByPrefixAsync(prefix, ct);

            // Broadcast invalidation to other instances (include instance ID to skip self)
            await _l2.PublishInvalidationAsync($"{InstanceId}|{prefix}");
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            var cached = await GetAsync<T>(key, ct);
            if (cached is not null) return cached;

            var value = await factory();
            await SetAsync(key, value, expiration, ct);
            return value;
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            if (await _l1.ExistsAsync(key, ct)) return true;
            return await _l2.ExistsAsync(key, ct);
        }
    }
}
