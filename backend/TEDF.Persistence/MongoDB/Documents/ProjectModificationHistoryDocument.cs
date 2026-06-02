using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TEDF.Persistence.MongoDB.Documents
{
    /// <summary>
    /// Document for project modification history.
    /// </summary>
    public class ProjectModificationHistoryDocument
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }

        public string ProjectCode { get; set; } = string.Empty;

        public Guid ModifiedBy { get; set; }

        public string ModifiedByName { get; set; } = string.Empty;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        public List<FieldChange> FieldsChanged { get; set; } = [];
        public string? Reason { get; set; }
        public int? RelatedSubmissionNumber { get; set; }
    }

    public class FieldChange
    {
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
