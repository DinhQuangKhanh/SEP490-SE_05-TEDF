using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.SaveProjectChecklist;

/// <summary>One evaluation entry sent by the evaluator for a criterion.</summary>
public record ChecklistEvaluationInput(Guid CriterionId, bool IsPassed, string? Comment);

/// <summary>
/// An evaluator saves (upserts) their checklist result for a project. The inputs are authoritative;
/// the server recomputes the passed count.
/// </summary>
[ActionLog("Save Evaluation Checklist", "Evaluation")]
public record SaveProjectChecklistCommand(
    Guid ProjectId,
    IReadOnlyList<ChecklistEvaluationInput> Items,
    string? Note
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["evaluator:{userId}:"];
}
