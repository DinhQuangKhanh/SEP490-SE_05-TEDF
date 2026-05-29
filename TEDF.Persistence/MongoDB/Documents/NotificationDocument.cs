using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Persistence.MongoDB.Documents
{
    /// <summary>
    /// Document for user notifications.
    /// </summary>
    public class NotificationDocument
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        public NotificationType Type { get; set; }

        [BsonRepresentation(BsonType.String)]
        public NotificationCategory Category { get; set; }

        public string? TargetUrl { get; set; }

        public Guid? RelatedEntityId { get; set; }

        public string? RelatedEntityType { get; set; }
        public bool IsRead { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ReadAt { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonExtraElements]
        public BsonDocument? Metadata { get; set; }
    }
}
