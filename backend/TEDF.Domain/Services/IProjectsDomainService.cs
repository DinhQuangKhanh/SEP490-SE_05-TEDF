using TEDF.Domain.Aggregates.EvaluationAggregate.ValueObjects;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;

namespace TEDF.Domain.Services
{
    public interface IProjectsDomainService
    {
        /// <summary>
        /// Generates the next project code for a semester and major, e.g. "FA26-SE-01".
        /// Single source of truth for the code format.
        /// </summary>
        Task<ProjectCode> GenerateProjectCodeAsync(int semesterId, int majorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a project can be submitted for evaluation.
        /// </summary>
        Task<(bool IsValid, string[] Errors)> ValidateForSubmissionAsync(Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a project snapshot for evaluation submission.
        /// </summary>
        Task<ProjectSnapshot> CreateSnapshotAsync(Project project, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates project statistics for reporting.
        /// </summary>
        Task<ProjectStatistics> GetStatisticsAsync(int semesterId, CancellationToken cancellationToken = default);
    }
    public record ProjectStatistics(
    int TotalProjects,
    int DraftProjects,
    int PendingEvaluationProjects,
    int ApprovedProjects,
    int RejectedProjects,
    int InProgressProjects,
    int CompletedProjects,
    int CancelledProjects,
    int FromPoolCount,
    int DirectRegistrationCount
    );
}
