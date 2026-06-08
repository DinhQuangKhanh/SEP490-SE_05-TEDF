using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Events
{
    public sealed record EvaluatorReassignedEvent(Guid SubmissionId, Guid? PreviousEvaluatorId, Guid NewEvaluatorId, Guid ReassignedBy, Guid ProjectId) : DomainEventBase;
}
