using TEDF.Domain.Aggregates.EvaluationAggregate.Entities;

namespace TEDF.Domain.Aggregates.EvaluationAggregate
{
    /// <summary>
    /// Repository interface for ProjectEvaluatorAssignment entity.
    /// </summary>
    public interface IProjectEvaluatorAssignmentRepository
    {
        /// <summary>
        /// Adds a new evaluator assignment.
        /// </summary>
        Task AddAsync(ProjectEvaluatorAssignment assignment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of active evaluator assignments for a project.
        /// </summary>
        Task<int> GetActiveCountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active evaluator assignments for a project.
        /// </summary>
        Task<IEnumerable<ProjectEvaluatorAssignment>> GetActiveByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an active evaluator assignment for a specific project and evaluator.
        /// </summary>
        Task<ProjectEvaluatorAssignment?> GetActiveByProjectAndEvaluatorAsync(Guid projectId, Guid evaluatorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all evaluator assignments for a project (including inactive).
        /// </summary>
        Task<IEnumerable<ProjectEvaluatorAssignment>> GetByProjectIdIncludingInactiveAsync(Guid projectId, CancellationToken cancellationToken = default);

    }
}
