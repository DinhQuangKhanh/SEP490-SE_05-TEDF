using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TEDF.Persistence.MongoDB.Documents;

/// <summary>
/// Document for error logs. Captures full exception details including
/// stack traces, inner exceptions, and request context for admin debugging.
/// </summary>
public class ErrorLogDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? ActiveRole { get; set; }

    /// <summary>Severity level: "error" or "critical".</summary>
    public string Severity { get; set; } = "error";

    /// <summary>Where the error originated: "Command", "Middleware", "EventHandler", "Background".</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Machine key of the command/handler that triggered the error.</summary>
    public string? ActionCode { get; set; }

    /// <summary>Human-readable action name.</summary>
    public string? Action { get; set; }

    /// <summary>Frontend route path from X-Route-Path header.</summary>
    public string? RoutePath { get; set; }

    /// <summary>API endpoint path (e.g., /api/semesters).</summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>HTTP method (GET, POST, PUT, DELETE).</summary>
    public string RequestMethod { get; set; } = string.Empty;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Full exception type name (e.g., System.InvalidOperationException).</summary>
    public string ErrorType { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    /// <summary>Full inner exception chain.</summary>
    public List<InnerExceptionDetail> InnerExceptions { get; set; } = [];

    /// <summary>Serialized command/request parameters.</summary>
    public BsonDocument? RequestParameters { get; set; }

    /// <summary>HttpContext.TraceIdentifier for correlating with other logs.</summary>
    public string? CorrelationId { get; set; }
}

public class InnerExceptionDetail
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}
