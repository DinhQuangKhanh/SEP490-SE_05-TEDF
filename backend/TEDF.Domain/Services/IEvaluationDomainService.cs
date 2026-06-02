
namespace TEDF.Domain.Services
{
    public interface IEvaluationDomainService
    {
        /// <summary>
        /// Checks if a project can be resubmitted for evaluation.
        /// </summary>
        Task<bool> CanResubmitAsync(Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the remaining number of resubmissions allowed for a project.
        /// </summary>
        Task<int> GetRemainingResubmissionsAsync(Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the modification deadline has passed.
        /// </summary>
        Task<bool> IsModificationDeadlinePassedAsync(Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation statistics for a semester.
        /// </summary>
        Task<EvaluationStatistics> GetStatisticsAsync(int semesterId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Auto-assigns an evaluator to a submission based on workload and expertise.
        /// </summary>
        Task<Guid?> SuggestEvaluatorAsync(Guid submissionId, CancellationToken cancellationToken = default);
    }
    public record EvaluationStatistics(
    int TotalSubmissions,
    int PendingSubmissions,
    int InReviewSubmissions,
    int CompletedSubmissions,
    int ApprovedCount,
    int NeedsModificationCount,
    int RejectedCount,
    double AverageEvaluationDays,
    Dictionary<Guid, int> EvaluatorWorkload
    );
}
