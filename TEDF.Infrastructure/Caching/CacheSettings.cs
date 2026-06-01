namespace TEDF.Infrastructure.Caching
{
    public class CacheSettings
    {
        public const string SectionName = "CacheSettings";
        public string Provider { get; set; } = "Memory"; // Memory, Redis, Hybrid
        public int DefaultExpirationMinutes { get; set; } = 30;
        public string? RedisConnectionString { get; set; }
        public int L1ExpirationMinutes { get; set; } = 2;
        public int L2ExpirationMinutes { get; set; } = 15;
        public string InvalidationChannel { get; set; } = "cache:invalidate";
    }
}
