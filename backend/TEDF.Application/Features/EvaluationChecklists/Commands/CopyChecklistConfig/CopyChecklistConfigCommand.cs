using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.CopyChecklistConfig;

/// <summary>Clones an existing checklist configuration into a new Draft for the target semester.</summary>
[ActionLog("Copy Checklist Config", "EvaluationChecklist")]
public record CopyChecklistConfigCommand(
    Guid SourceConfigId,
    int TargetSemesterId
) : ICacheInvalidatingCommand<Guid>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["checklist-configs:"];
}
