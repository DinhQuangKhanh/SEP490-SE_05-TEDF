using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.DeactivateChecklistConfig;

/// <summary>Retires a checklist configuration (kept for history; no longer applied to new evaluations).</summary>
[ActionLog("Deactivate Checklist Config", "EvaluationChecklist")]
public record DeactivateChecklistConfigCommand(Guid Id) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["checklist-configs:"];
}
