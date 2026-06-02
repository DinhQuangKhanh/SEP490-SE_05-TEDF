using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace TEDF.Persistence.MongoDB.Serializers
{
    /// <summary>
    /// Serializer configuration for MongoDB. Thread-safe singleton initialization.
    /// </summary>
    public static class MongoSerializerConfiguration
    {
        private static int _configured;

        /// <summary>
        /// Configures global serializers for MongoDB.
        /// Should be called once at application startup.
        /// Thread-safe: uses Interlocked to prevent double registration.
        /// </summary>
        public static void Configure()
        {
            if (Interlocked.Exchange(ref _configured, 1) == 1) return;

            // Register custom serializers
            BsonSerializer.RegisterSerializer(new GuidAsStringSerializer());
            BsonSerializer.RegisterSerializer(new NullableGuidAsStringSerializer());

            // Configure DateTime to always use UTC
            BsonSerializer.RegisterSerializer(typeof(DateTime), new DateTimeSerializer(DateTimeKind.Utc));
        }
    }
}
