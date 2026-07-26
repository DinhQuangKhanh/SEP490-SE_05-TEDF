using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.ActivateChecklistConfig;

/// <summary>
/// Activates a checklist configuration for its semester. Any currently Active config for the same
/// semester is deactivated first, guaranteeing exactly one Active checklist per semester.
/// </summary>
[ActionLog("Activate Checklist Config", "EvaluationChecklist")]
public record ActivateChecklistConfigCommand(Guid Id) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["checklist-configs:"];
}
