namespace TEDF.Domain.Services
{
    /// <summary>
    /// Write-side service for the Evaluations feature (plus evaluation helper queries).
    /// Command handlers depend on this only.
    /// </summary>
    public interface IEvaluationsDomainService
    {
        // ── Helper queries / policies ──
        Task<bool> CanResubmitAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<int> GetRemainingResubmissionsAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<bool> IsModificationDeadlinePassedAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<EvaluationStatistics> GetStatisticsAsync(int semesterId, CancellationToken cancellationToken = default);
        Task<Guid?> SuggestEvaluatorAsync(Guid submissionId, CancellationToken cancellationToken = default);

        // ── Evaluations feature write operations ──

        /// <summary>Department head assigns an evaluator to a project (validates department ownership + role).</summary>
        Task AssignEvaluatorAsync(Guid currentUserId, Guid projectId, int phaseId, Guid evaluatorId, int evaluatorOrder, CancellationToken cancellationToken = default);

        /// <summary>Evaluator submits their individual result; auto-resolves the project if both agree.</summary>
        Task SubmitEvaluationAsync(Guid evaluatorId, Guid projectId, int result, string? feedback, CancellationToken cancellationToken = default);

        /// <summary>Department head resolves a conflicted evaluation with a final decision.</summary>
        Task SubmitFinalDecisionAsync(Guid currentUserId, Guid projectId, int result, string? notes, CancellationToken cancellationToken = default);
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
