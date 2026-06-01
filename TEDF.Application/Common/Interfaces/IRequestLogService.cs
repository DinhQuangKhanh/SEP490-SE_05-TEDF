namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Service for persisting MediatR command execution logs to a durable store.
/// </summary>
public interface IRequestLogService
{
  /// <summary>
  /// Persists a log entry for a completed or failed command.
  /// </summary>
  Task LogAsync(RequestLogEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single MediatR command log entry.
/// </summary>
public record RequestLogEntry(
    string RequestName,
    string ActionDisplayName,
    string Category,
    string? UserId,
    string? UserName,
    string? UserEmail,
    string? UserRole,
    bool IsSuccess,
    long ElapsedMilliseconds,
    DateTime Timestamp,
    string? ErrorMessage = null,
    string? ErrorType = null,
    string? StackTrace = null,
    Dictionary<string, object?>? RequestParameters = null
);
