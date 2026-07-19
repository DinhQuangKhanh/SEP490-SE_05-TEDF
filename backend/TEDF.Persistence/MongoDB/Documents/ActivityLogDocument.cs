using MongoDB.Bson.Serialization.Attributes;

namespace TEDF.Persistence.MongoDB.Documents;

public class ActivityLogDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }

    /// <summary>Role resolved from API path prefix: admin, mentor, student, evaluator, department-head.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Command class name, e.g. CreateSemesterCommand.</summary>
    public string ActionCode { get; set; } = string.Empty;

    /// <summary>Human-readable name from [ActionLogAttribute], e.g. "Tạo kỳ học".</summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>Feature grouping: Semester, Project, Group, Evaluation, TopicPool, User, System.</summary>
    public string FeatureCategory { get; set; } = string.Empty;

    /// <summary>Frontend route from X-Route-Path header — informational only, may be null.</summary>
    public string? RoutePath { get; set; }

    /// <summary>Actual API endpoint path, e.g. /api/admin/semesters.</summary>
    public string RequestPath { get; set; } = string.Empty;

    public string RequestMethod { get; set; } = string.Empty;

    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }

    /// <summary>Success or Failure.</summary>
    public string Status { get; set; } = "Success";

    public long DurationMs { get; set; }

    /// <summary>HttpContext.TraceIdentifier — use this to find the matching error_logs entry on failure.</summary>
    public string? CorrelationId { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
