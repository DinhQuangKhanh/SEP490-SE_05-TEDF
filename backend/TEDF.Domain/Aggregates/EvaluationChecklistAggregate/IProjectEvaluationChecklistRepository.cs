namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

/// <summary>Repository for the <see cref="ProjectEvaluationChecklist"/> aggregate (evaluator results).</summary>
public interface IProjectEvaluationChecklistRepository
{
    Task AddAsync(ProjectEvaluationChecklist checklist, CancellationToken cancellationToken = default);

    void Update(ProjectEvaluationChecklist checklist);

    /// <summary>An evaluator's result for a specific project + evaluation round.</summary>
    Task<ProjectEvaluationChecklist?> GetByProjectEvaluatorAsync(
        Guid projectId, Guid evaluatorId, int submissionNumber, CancellationToken cancellationToken = default);

    /// <summary>An evaluator's most recent result for a project (any round).</summary>
    Task<ProjectEvaluationChecklist?> GetLatestByProjectEvaluatorAsync(
        Guid projectId, Guid evaluatorId, CancellationToken cancellationToken = default);

    /// <summary>All checklist results for a project (history / both evaluators, all rounds).</summary>
    Task<IReadOnlyList<ProjectEvaluationChecklist>> GetByProjectAsync(
        Guid projectId, CancellationToken cancellationToken = default);
}
