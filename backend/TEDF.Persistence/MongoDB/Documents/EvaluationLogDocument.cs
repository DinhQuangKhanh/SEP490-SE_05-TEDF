using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Persistence.MongoDB.Documents
{
    /// <summary>
    /// Document for evaluation audit logs.
    /// </summary>
    public class EvaluationLogDocument
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }

        public string ProjectCode { get; set; } = string.Empty;

        public Guid? EvaluationSubmissionId { get; set; }

        public int SubmissionNumber { get; set; }

        [BsonRepresentation(BsonType.String)]
        public EvaluationAction Action { get; set; }

        public Guid? PerformedBy { get; set; }

        public string PerformedByName { get; set; } = string.Empty;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        [BsonRepresentation(BsonType.String)]
        public EvaluationResult? Result { get; set; }

        public string? Feedback { get; set; }
        public string? CriteriaFeedbackJson { get; set; }
        public string? ProjectSnapshotJson { get; set; }

        [BsonExtraElements]
        public BsonDocument? Metadata { get; set; }
    }
}
