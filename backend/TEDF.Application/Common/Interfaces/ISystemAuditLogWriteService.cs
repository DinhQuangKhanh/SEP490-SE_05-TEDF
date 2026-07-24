namespace TEDF.Application.Common.Interfaces;

public interface ISystemAuditLogWriteService
{
    Task LogAsync(string entityType, Guid entityId, string action, Guid? performedBy, object? metadata, CancellationToken ct = default);
}
