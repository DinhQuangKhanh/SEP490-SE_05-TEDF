using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation
{
    public class SystemAuditLogRepository : ISystemAuditLogRepository
    {
        private readonly IMongoCollection<SystemAuditLogDocument> _collection;

        public SystemAuditLogRepository(MongoDbContext context)
        {
            _collection = context.GetCollection<SystemAuditLogDocument>(MongoDbContext.Collections.SystemAuditLogs);
        }

        public async Task AddAsync(SystemAuditLogDocument log, CancellationToken ct = default)
            => await _collection.InsertOneAsync(log, cancellationToken: ct);

        public async Task<IEnumerable<SystemAuditLogDocument>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
            => await _collection.Find(l => l.EntityType == entityType && l.EntityId == entityId).SortByDescending(l => l.Timestamp).ToListAsync(ct);

        public async Task<IEnumerable<SystemAuditLogDocument>> GetRecentAsync(int limit = 100, CancellationToken ct = default)
            => await _collection.Find(_ => true).SortByDescending(l => l.Timestamp).Limit(limit).ToListAsync(ct);
    }
}
