using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.SaveProjectChecklist;

/// <summary>One score entry sent by the evaluator for a criterion (score may be null = not yet scored).</summary>
public record ChecklistScoreInput(Guid CriterionId, decimal? Score, string? Comment);

/// <summary>
/// An evaluator saves (upserts) their checklist result for a project. The scores are authoritative input;
/// the server validates each score against its snapshot bounds and recomputes the pass flags + passed count.
/// </summary>
[ActionLog("Save Evaluation Checklist", "Evaluation")]
public record SaveProjectChecklistCommand(
    Guid ProjectId,
    IReadOnlyList<ChecklistScoreInput> Items,
    string? Note
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["evaluator:{userId}:"];
}
