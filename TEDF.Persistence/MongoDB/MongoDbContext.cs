using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace TEDF.Persistence.MongoDB
{
    /// <summary>
    /// MongoDB context for managing collections.
    /// </summary>
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IMongoClient mongoClient, IOptions<MongoDbSettings> settings)
        {
            if (settings?.Value == null)
                throw new ArgumentNullException(nameof(settings), "MongoDbSettings is not configured");

            _database = mongoClient.GetDatabase(settings.Value.DatabaseName);
        }

        /// <summary>
        /// Gets a typed collection.
        /// </summary>
        public IMongoCollection<T> GetCollection<T>(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Collection name cannot be null or empty", nameof(name));

            return _database.GetCollection<T>(name);
        }

        /// <summary>
        /// Collection names as constants.
        /// </summary>
        public static class Collections
        {
            public const string EvaluationLogs = "evaluation_logs";
            public const string ProjectModifications = "project_modifications";
            public const string Notifications = "notifications";
            public const string Conversations = "conversations";
            public const string Messages = "messages";
            public const string UserActivityLogs = "user_activity_logs";
            public const string SystemAuditLogs = "system_audit_logs";
            public const string ErrorLogs = "error_logs";
        }
    }
}
