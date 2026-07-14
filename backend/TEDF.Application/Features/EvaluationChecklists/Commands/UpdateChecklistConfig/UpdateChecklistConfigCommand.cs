using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.UpdateChecklistConfig;

/// <summary>Edits a Draft checklist's criteria (text and order). Active configs must be copied to a new version.</summary>
[ActionLog("Update Checklist Config", "EvaluationChecklist")]
public record UpdateChecklistConfigCommand(
    Guid Id,
    IReadOnlyList<ChecklistCriterionInput> Criteria
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["checklist-configs:"];
}
