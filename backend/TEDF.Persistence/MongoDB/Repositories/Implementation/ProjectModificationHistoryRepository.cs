using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation
{
    public class ProjectModificationHistoryRepository : IProjectModificationHistoryRepository
    {
        private readonly IMongoCollection<ProjectModificationHistoryDocument> _collection;

        public ProjectModificationHistoryRepository(MongoDbContext context)
        {
            _collection = context.GetCollection<ProjectModificationHistoryDocument>(MongoDbContext.Collections.ProjectModifications);
        }

        public async Task AddAsync(ProjectModificationHistoryDocument history, CancellationToken ct = default)
            => await _collection.InsertOneAsync(history, cancellationToken: ct);

        public async Task<IEnumerable<ProjectModificationHistoryDocument>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
            => await _collection.Find(h => h.ProjectId == projectId).SortByDescending(h => h.ModifiedAt).ToListAsync(ct);
    }
}
