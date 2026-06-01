using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TEDF.Persistence.MongoDB.Documents
{
    /// <summary>
    /// Document for user activity logs.
    /// Tracks meaningful actions (commands and domain events) performed by users.
    /// Queries are NOT logged here — only commands and domain events.
    /// </summary>
    public class UserActivityLogDocument
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>ID of the user who performed the action.</summary>
        public Guid UserId { get; set; }

        /// <summary>Display name of the user at the time of the action.</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>Email of the user at the time of the action.</summary>
        public string? UserEmail { get; set; }

        /// <summary>
        /// The active role from ICurrentUserService (e.g. "admin", "mentor", "evaluator", "student").
        /// </summary>
        public string ActiveRole { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable action name (e.g. "Create Semester", "Approve Project").
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Machine key for the action (e.g. "CreateSemesterCommand", "ProjectSubmitted").
        /// </summary>
        public string ActionCode { get; set; } = string.Empty;

        /// <summary>
        /// Category to group related actions for easier filtering
        /// (e.g. "Semester", "Project", "Evaluation", "Group", "TopicPool", "User", "Department").
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Type of entity involved (e.g. "Group", "Project").</summary>
        public string? EntityType { get; set; }

        /// <summary>ID of the entity involved.</summary>
        public Guid? EntityId { get; set; }

        /// <summary>
        /// Severity level: "info" (default), "warning", "error", "critical".
        /// </summary>
        [BsonDefaultValue("info")]
        public string Severity { get; set; } = "info";

        /// <summary>Frontend route path from X-Route-Path header (e.g. "/admin/semesters").</summary>
        public string? RoutePath { get; set; }

        /// <summary>Client IP address.</summary>
        public string? IpAddress { get; set; }

        /// <summary>Client User-Agent string.</summary>
        public string? UserAgent { get; set; }

        /// <summary>Links to error_logs document when Severity is error/critical.</summary>
        public Guid? ErrorLogId { get; set; }

        /// <summary>Command execution time in milliseconds.</summary>
        public long? DurationMs { get; set; }

        /// <summary>When the action occurred (UTC).</summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Flexible key-value bag for action-specific data.</summary>
        [BsonExtraElements]
        public BsonDocument? Details { get; set; }
    }
}
