using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Events
{
    /// <summary>
    /// Domain event raised when an evaluator submits their individual evaluation result.
    /// </summary>
    public sealed record EvaluatorSubmittedResultEvent(
        Guid AssignmentId,
        Guid ProjectId,
        Guid EvaluatorId,
        EvaluationResult Result
    ) : DomainEventBase;
}
