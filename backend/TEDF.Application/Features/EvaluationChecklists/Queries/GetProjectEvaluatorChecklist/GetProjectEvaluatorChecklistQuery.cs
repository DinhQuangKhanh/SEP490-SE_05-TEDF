using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetProjectEvaluatorChecklist;

/// <summary>
/// Department-Head view of a specific evaluator's checklist for a project (read-only). Used on the
/// "needs-decision" and history screens so the DH can inspect how each evaluator scored the topic.
/// Authorization (DH of the project's department) is enforced by the endpoint policy.
/// </summary>
public record GetProjectEvaluatorChecklistQuery(Guid ProjectId, Guid EvaluatorId)
    : IQuery<ProjectChecklistDto>;
