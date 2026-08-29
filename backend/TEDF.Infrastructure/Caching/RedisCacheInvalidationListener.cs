using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace TEDF.Infrastructure.Caching
{
    /// <summary>
    /// BackgroundService that subscribes to Redis pub/sub for cache invalidation events.
    /// When another instance publishes an invalidation, this listener clears the
    /// matching keys from the local L1 (MemoryCacheService) to ensure cross-instance consistency.
    /// </summary>
    public sealed class RedisCacheInvalidationListener : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly MemoryCacheService _l1;
        private readonly CacheSettings _settings;
        private readonly ILogger<RedisCacheInvalidationListener> _logger;

        public RedisCacheInvalidationListener(
            IConnectionMultiplexer redis,
            MemoryCacheService l1,
            IOptions<CacheSettings> settings,
            ILogger<RedisCacheInvalidationListener> logger)
        {
            _redis = redis;
            _l1 = l1;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var subscriber = _redis.GetSubscriber();
                var channel = RedisChannel.Literal(_settings.InvalidationChannel);

                // OnMessage with an async handler lets us await the L1 clear instead of blocking a
                // ThreadPool thread with GetAwaiter().GetResult(); StackExchange.Redis processes these
                // messages sequentially per queue, so ordering is preserved.
                var queue = await subscriber.SubscribeAsync(channel);
                queue.OnMessage(async channelMessage =>
                {
                    var message = channelMessage.Message;
                    if (message.IsNullOrEmpty) return;

                    var raw = message.ToString();
                    var separatorIndex = raw.IndexOf('|');
                    if (separatorIndex < 0) return;

                    var senderId = raw[..separatorIndex];
                    var prefix = raw[(separatorIndex + 1)..];

                    // Skip self-invalidation (this instance already cleared its own L1)
                    if (senderId == HybridCacheService.InstanceId)
                        return;

                    _logger.LogDebug(
                        "Received cross-instance cache invalidation from {SenderId} for prefix: {Prefix}",
                        senderId, prefix);

                    try
                    {
                        await _l1.RemoveByPrefixAsync(prefix, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to clear L1 cache for prefix {Prefix}", prefix);
                    }
                });

                _logger.LogInformation(
                    "Redis cache invalidation listener started on channel: {Channel}",
                    _settings.InvalidationChannel);

                // Keep alive until cancellation
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Redis cache invalidation listener stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis cache invalidation listener encountered an error");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                var subscriber = _redis.GetSubscriber();
                var channel = RedisChannel.Literal(_settings.InvalidationChannel);
                await subscriber.UnsubscribeAsync(channel);
                _logger.LogInformation("Redis cache invalidation listener unsubscribed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error unsubscribing from Redis invalidation channel");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
