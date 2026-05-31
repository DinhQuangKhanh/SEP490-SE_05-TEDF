using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Evaluations.Commands.SubmitEvaluation;

[ActionLog("Submit Evaluation", "Evaluation")]
public record SubmitEvaluationCommand(Guid ProjectId, int Result, string? Feedback) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["evaluator:{userId}:"];
}
