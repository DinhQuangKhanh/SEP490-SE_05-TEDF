using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Caching
{
    /// <summary>
    /// Redis-backed cache service (L2) using IConnectionMultiplexer directly.
    /// Supports SCAN-based prefix removal and pub/sub for cross-instance invalidation.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly CacheSettings _settings;
        private readonly ILogger<RedisCacheService> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public RedisCacheService(IConnectionMultiplexer redis, IOptions<CacheSettings> settings, ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _settings = settings.Value;
            _logger = logger;
        }

        private IDatabase Db => _redis.GetDatabase();

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            try
            {
                var data = await Db.StringGetAsync(key);
                if (data.IsNullOrEmpty) return default;

                return JsonSerializer.Deserialize<T>(data!, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Redis cache value for key: {Key}", key);
                return default;
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on GET for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            try
            {
                var data = JsonSerializer.Serialize(value, JsonOptions);
                var ttl = expiration ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes);
                await Db.StringSetAsync(key, data, ttl);
                _logger.LogDebug("Redis cache set: {Key}, TTL: {TTL}", key, ttl);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on SET for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default)
        {
            try
            {
                await Db.KeyDeleteAsync(key);
                _logger.LogDebug("Redis cache removed: {Key}", key);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on DELETE for key: {Key}", key);
            }
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        {
            try
            {
                var endpoints = _redis.GetEndPoints();
                var removedCount = 0;

                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);
                    if (server.IsConnected && !server.IsReplica)
                    {
                        await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
                        {
                            await Db.KeyDeleteAsync(key);
                            removedCount++;
                        }
                    }
                }

                _logger.LogDebug("Redis cache removed by prefix: {Prefix}, count: {Count}", prefix, removedCount);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on RemoveByPrefix: {Prefix}", prefix);
            }
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
            try
            {
                return await Db.KeyExistsAsync(key);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on EXISTS for key: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Publishes a cache invalidation message to the Redis pub/sub channel.
        /// Other instances will receive this and clear their L1 caches.
        /// </summary>
        public async Task PublishInvalidationAsync(string prefix)
        {
            try
            {
                var subscriber = _redis.GetSubscriber();
                await subscriber.PublishAsync(
                    RedisChannel.Literal(_settings.InvalidationChannel),
                    prefix);
                _logger.LogDebug("Published cache invalidation for prefix: {Prefix}", prefix);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on publish invalidation for prefix: {Prefix}", prefix);
            }
        }
    }
}
