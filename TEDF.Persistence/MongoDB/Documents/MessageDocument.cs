using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TEDF.Domain.Enums.Message;

namespace TEDF.Persistence.MongoDB.Documents
{
    /// <summary>
    /// Document for messages.
    /// </summary>
    public class MessageDocument
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ConversationId { get; set; }

        public Guid SenderId { get; set; }

        public string SenderName { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        public MessageType Type { get; set; }

        public string Content { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }

        public List<Guid> ReadByUserIds { get; set; } = [];

        public bool IsDeleted { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? EditedAt { get; set; }
    }
}
