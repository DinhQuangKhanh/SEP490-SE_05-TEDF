namespace TEDF.Application.Common.Interfaces;

public interface IActivityLogService
{
    Task LogAsync(ActivityLogEntry entry, CancellationToken ct = default);
}

public record ActivityLogEntry(
    string ActionCode,
    string ActionName,
    string FeatureCategory,
    string? UserId,
    string? UserName,
    string? UserEmail,
    string? Role,
    bool IsSuccess,
    long DurationMs,
    DateTime Timestamp,
    string? EntityType = null,
    Guid? EntityId = null,
    string? CorrelationId = null
);
