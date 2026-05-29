using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation
{
    public class EvaluationLogRepository : IEvaluationLogRepository
    {
        private readonly IMongoCollection<EvaluationLogDocument> _collection;

        public EvaluationLogRepository(MongoDbContext context)
        {
            _collection = context.GetCollection<EvaluationLogDocument>(MongoDbContext.Collections.EvaluationLogs);
        }

        public async Task AddAsync(EvaluationLogDocument log, CancellationToken ct = default)
            => await _collection.InsertOneAsync(log, cancellationToken: ct);

        public async Task<IEnumerable<EvaluationLogDocument>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
            => await _collection.Find(l => l.ProjectId == projectId).SortByDescending(l => l.PerformedAt).ToListAsync(ct);

        public async Task<IEnumerable<EvaluationLogDocument>> GetBySubmissionIdAsync(Guid submissionId, CancellationToken ct = default)
            => await _collection.Find(l => l.EvaluationSubmissionId == submissionId).SortBy(l => l.PerformedAt).ToListAsync(ct);

        public async Task<IEnumerable<EvaluationLogDocument>> GetByPerformedByAsync(Guid userId, CancellationToken ct = default)
            => await _collection.Find(l => l.PerformedBy == userId).SortByDescending(l => l.PerformedAt).ToListAsync(ct);
    }
}
