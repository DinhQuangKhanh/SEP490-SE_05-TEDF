namespace TEDF.Domain.Services
{
    /// <summary>
    /// Write-side service for the Evaluations feature: assign evaluators, submit results, and resolve
    /// conflicts with a final decision. Command handlers depend on this only; reads go through
    /// <c>IEvaluationsQueryService</c>.
    /// </summary>
    public interface IEvaluationsDomainService
    {
        /// <summary>Department head assigns an evaluator to a project (validates department ownership + role).</summary>
        Task AssignEvaluatorAsync(Guid currentUserId, Guid projectId, int phaseId, Guid evaluatorId, int evaluatorOrder, CancellationToken cancellationToken = default);

        /// <summary>Evaluator submits their individual result; auto-resolves the project if both agree.</summary>
        Task SubmitEvaluationAsync(Guid evaluatorId, Guid projectId, int result, string? feedback, CancellationToken cancellationToken = default);

        /// <summary>Department head resolves a conflicted evaluation with a final decision.</summary>
        Task SubmitFinalDecisionAsync(Guid currentUserId, Guid projectId, int result, string? notes, CancellationToken cancellationToken = default);
    }
}
