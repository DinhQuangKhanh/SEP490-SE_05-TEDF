using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.CreateChecklistConfig;

/// <summary>Department Head creates a new Draft checklist configuration for a semester.</summary>
[ActionLog("Create Checklist Config", "EvaluationChecklist")]
public record CreateChecklistConfigCommand(
    int SemesterId,
    IReadOnlyList<ChecklistCriterionInput> Criteria
) : ICacheInvalidatingCommand<Guid>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["checklist-configs:"];
}
