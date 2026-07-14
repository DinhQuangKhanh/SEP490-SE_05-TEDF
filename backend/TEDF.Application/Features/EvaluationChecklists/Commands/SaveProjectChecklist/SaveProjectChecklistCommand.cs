using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.SaveProjectChecklist;

/// <summary>
/// An evaluator saves (upserts) their checklist result for a project. The set of passed criterion ids
/// is authoritative input; the server recomputes the passed count from valid, de-duplicated ids.
/// </summary>
[ActionLog("Save Evaluation Checklist", "Evaluation")]
public record SaveProjectChecklistCommand(
    Guid ProjectId,
    IReadOnlyList<Guid> PassedCriterionIds,
    string? Note
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["evaluator:{userId}:"];
}
