using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Events
{
    /// <summary>
    /// Domain event raised when an evaluator is assigned to a project.
    /// </summary>
    public sealed record EvaluatorAssignedToProjectEvent(
        Guid AssignmentId,
        Guid ProjectId,
        int PhaseId,
        Guid EvaluatorId,
        int EvaluatorOrder,
        Guid AssignedBy
    ) : DomainEventBase;
}
