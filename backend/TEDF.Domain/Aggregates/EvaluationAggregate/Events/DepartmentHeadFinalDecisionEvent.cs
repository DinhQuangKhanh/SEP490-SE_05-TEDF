using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Events
{
    /// <summary>
    /// Domain event raised when the Department Head makes a final decision
    /// on a project where the two evaluators gave conflicting results.
    /// </summary>
    public sealed record DepartmentHeadFinalDecisionEvent(
        Guid ProjectId,
        EvaluationResult FinalResult,
        Guid DecidedBy
    ) : DomainEventBase;
}
