using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Caching
{
    /// <summary>
    /// In-memory cache with key tracking for prefix-based invalidation.
    /// Must be registered as Singleton so the key set persists across scopes.
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly CacheSettings _settings;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly HashSet<string> _keys = [];
        private readonly object _lock = new();

        public MemoryCacheService(IMemoryCache cache, IOptions<CacheSettings> settings, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            _cache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes)
            };

            _cache.Set(key, value, options);

            lock (_lock) { _keys.Add(key); }

            _logger.LogDebug("Cache set: {Key}", key);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _cache.Remove(key);
            lock (_lock) { _keys.Remove(key); }
            _logger.LogDebug("Cache removed: {Key}", key);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        {
            List<string> keysToRemove;

            // Single lock acquisition: snapshot keys AND remove them from the tracking set atomically
            lock (_lock)
            {
                keysToRemove = _keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
                foreach (var key in keysToRemove)
                    _keys.Remove(key);
            }

            // Cache.Remove is thread-safe and doesn't need our lock
            foreach (var key in keysToRemove)
                _cache.Remove(key);

            _logger.LogDebug("Cache removed by prefix: {Prefix}, count: {Count}", prefix, keysToRemove.Count);
            return Task.CompletedTask;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            var cached = await GetAsync<T>(key, ct);
            if (cached is not null) return cached;

            var value = await factory();
            await SetAsync(key, value, expiration, ct);
            return value;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            return Task.FromResult(_cache.TryGetValue(key, out _));
        }
    }
}
