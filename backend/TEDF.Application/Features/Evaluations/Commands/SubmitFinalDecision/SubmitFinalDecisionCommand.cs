using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Evaluations.Commands.SubmitFinalDecision;

[ActionLog("Submit Final Decision", "Department")]
public record SubmitFinalDecisionCommand(Guid ProjectId, int Result, string? Notes) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
    [
        "department-head:",
        "evaluator:"
    ];
}
