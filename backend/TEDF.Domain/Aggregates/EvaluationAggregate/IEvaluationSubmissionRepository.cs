using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.EvaluationAggregate
{
    public interface IEvaluationSubmissionRepository : IRepository<EvaluationSubmission, Guid>
    {
        Task<IEnumerable<EvaluationSubmission>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<EvaluationSubmission?> GetLatestByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<EvaluationSubmission>> GetByEvaluatorIdAsync(Guid evaluatorId, CancellationToken cancellationToken = default);
        Task<IEnumerable<EvaluationSubmission>> GetPendingAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<EvaluationSubmission>> GetInReviewAsync(CancellationToken cancellationToken = default);
        Task<int> GetSubmissionCountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<List<EvaluationSubmission>> GetBySemesterWithSnapshotAsync(int semesterId, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, int>> GetActiveEvaluatorWorkloadCountsAsync(CancellationToken cancellationToken = default);
    }
}
