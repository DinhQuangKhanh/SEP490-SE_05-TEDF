namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Service for persisting error logs to MongoDB.
/// </summary>
public interface IErrorLogService
{
    Task LogAsync(ErrorLogEntry entry, CancellationToken ct = default);
}

public record ErrorLogEntry(
    string? UserId,
    string? UserName,
    string? UserEmail,
    string? ActiveRole,
    string? Severity,
    string Source,
    string? ActionCode,
    string? ActionDisplayName,
    string? RoutePath,
    string RequestPath,
    string RequestMethod,
    string? IpAddress,
    string? UserAgent,
    string ErrorMessage,
    string ErrorType,
    string? StackTrace,
    List<InnerExceptionEntry> InnerExceptions,
    Dictionary<string, object?>? RequestParameters,
    string? CorrelationId,
    DateTime Timestamp
);

public record InnerExceptionEntry(string Message, string Type, string? StackTrace);
