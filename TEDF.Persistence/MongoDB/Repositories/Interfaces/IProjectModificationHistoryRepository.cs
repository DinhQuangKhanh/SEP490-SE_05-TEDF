using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces
{
    public interface IProjectModificationHistoryRepository
    {
        Task AddAsync(ProjectModificationHistoryDocument history, CancellationToken ct = default);
        Task<IEnumerable<ProjectModificationHistoryDocument>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    }
}
