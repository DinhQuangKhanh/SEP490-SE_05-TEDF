using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TEDF.Domain.Enums.Message;

namespace TEDF.Persistence.MongoDB.Documents
{
    /// <summary>
    /// Document for conversations.
    /// </summary>
    public class ConversationDocument
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonRepresentation(BsonType.String)]
        public ConversationType Type { get; set; }

        public string? Name { get; set; }

        public List<Guid> ParticipantIds { get; set; } = [];

        public Guid? GroupId { get; set; }

        public Guid? LastMessageId { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastMessageAt { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? UpdatedAt { get; set; }
    }
}
