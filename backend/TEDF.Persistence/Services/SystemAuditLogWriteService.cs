using System.Text.Json;
using MongoDB.Bson;
using TEDF.Application.Common.Interfaces;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.Services;

public class SystemAuditLogWriteService : ISystemAuditLogWriteService
{
    private readonly ISystemAuditLogRepository _repository;

    public SystemAuditLogWriteService(ISystemAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task LogAsync(string entityType, Guid entityId, string action, Guid? performedBy, object? metadata, CancellationToken ct = default)
    {
        var log = new SystemAuditLogDocument
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            PerformedBy = performedBy,
            Metadata = metadata != null ? BsonDocument.Parse(JsonSerializer.Serialize(metadata)) : null
        };
        await _repository.AddAsync(log, ct);
    }
}
