using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces
{
    public interface IEvaluationLogRepository
    {
        Task AddAsync(EvaluationLogDocument log, CancellationToken ct = default);
        Task<IEnumerable<EvaluationLogDocument>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
        Task<IEnumerable<EvaluationLogDocument>> GetBySubmissionIdAsync(Guid submissionId, CancellationToken ct = default);
        Task<IEnumerable<EvaluationLogDocument>> GetByPerformedByAsync(Guid userId, CancellationToken ct = default);
    }
}
