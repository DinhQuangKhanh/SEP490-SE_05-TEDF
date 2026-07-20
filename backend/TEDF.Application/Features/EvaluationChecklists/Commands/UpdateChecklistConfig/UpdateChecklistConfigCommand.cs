using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.UpdateChecklistConfig;

/// <summary>Edits a Draft checklist's criteria (text, order, scores) and required-pass count. Active configs must be copied to a new version.</summary>
[ActionLog("Update Checklist Config", "EvaluationChecklist")]
public record UpdateChecklistConfigCommand(
    Guid Id,
    IReadOnlyList<ChecklistCriterionInput> Criteria,
    int RequiredPassCount
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["checklist-configs:"];
}
