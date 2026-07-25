using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces
{
    public interface ISystemAuditLogRepository
    {
        Task AddAsync(SystemAuditLogDocument log, CancellationToken ct = default);
        Task<IEnumerable<SystemAuditLogDocument>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
        Task<IEnumerable<SystemAuditLogDocument>> GetRecentAsync(int limit = 100, CancellationToken ct = default);
    }
}
