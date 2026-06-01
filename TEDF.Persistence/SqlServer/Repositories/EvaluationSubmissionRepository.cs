using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Domain.Specifications.Evaluations;
using TEDF.Persistence.Common;

namespace TEDF.Persistence.SqlServer.Repositories
{
    /// <summary>
    /// Repository implementation for EvaluationSubmission aggregate using specifications.
    /// </summary>
    public class EvaluationSubmissionRepository : BaseRepository<EvaluationSubmission, Guid>, IEvaluationSubmissionRepository
    {
        public EvaluationSubmissionRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<EvaluationSubmission>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var spec = new SubmissionsByProjectSpec(projectId);
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<EvaluationSubmission?> GetLatestByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(s => s.ProjectId == projectId)
                .OrderByDescending(s => s.SubmissionNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<EvaluationSubmission>> GetByEvaluatorIdAsync(Guid evaluatorId, CancellationToken cancellationToken = default)
        {
            var spec = new SubmissionsByEvaluatorSpec(evaluatorId);
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<IEnumerable<EvaluationSubmission>> GetPendingAsync(CancellationToken cancellationToken = default)
        {
            var spec = new PendingSubmissionsSpec();
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<IEnumerable<EvaluationSubmission>> GetInReviewAsync(CancellationToken cancellationToken = default)
        {
            var spec = new InReviewSubmissionsSpec();
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<int> GetSubmissionCountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(s => s.ProjectId == projectId, cancellationToken);
        }

        /// <summary>
        /// Gets in-review submissions for a specific evaluator using specification.
        /// </summary>
        public async Task<IEnumerable<EvaluationSubmission>> GetInReviewByEvaluatorAsync(Guid evaluatorId, CancellationToken cancellationToken = default)
        {
            var spec = new InReviewSubmissionsSpec(evaluatorId);
            return await ListAsync(spec, cancellationToken);
        }

        /// <summary>
        /// Gets submissions by evaluator with specific status using specification.
        /// </summary>
        public async Task<IEnumerable<EvaluationSubmission>> GetByEvaluatorAndStatusAsync(Guid evaluatorId, SubmissionStatus status, CancellationToken cancellationToken = default)
        {
            var spec = new SubmissionsByEvaluatorSpec(evaluatorId, status);
            return await ListAsync(spec, cancellationToken);
        }

        /// <summary>
        /// Gets evaluation statistics for dashboard.
        /// </summary>
        public async Task<Dictionary<SubmissionStatus, int>> GetStatusCountAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        }

        /// <summary>
        /// Gets evaluation result statistics.
        /// </summary>
        public async Task<Dictionary<EvaluationResult, int>> GetResultCountAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(s => s.Status == SubmissionStatus.Completed)
                .GroupBy(s => s.Result)
                .Select(g => new { Result = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Result, x => x.Count, cancellationToken);
        }

        public async Task<List<EvaluationSubmission>> GetBySemesterWithSnapshotAsync(int semesterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(e => e.Snapshot)
                .Where(e => _context.Projects.Any(p => p.Id == e.ProjectId && p.SemesterId == semesterId))
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetActiveEvaluatorWorkloadCountsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(e => (e.Status == SubmissionStatus.Pending || e.Status == SubmissionStatus.InReview) &&
                           e.AssignedEvaluatorId.HasValue)
                .GroupBy(e => e.AssignedEvaluatorId!.Value)
                .Select(g => new { EvaluatorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EvaluatorId, x => x.Count, cancellationToken);
        }
    }
}
