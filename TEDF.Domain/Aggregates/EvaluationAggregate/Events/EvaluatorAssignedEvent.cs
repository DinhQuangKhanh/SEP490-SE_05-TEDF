using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Events
{
    public sealed record EvaluatorAssignedEvent(Guid SubmissionId, Guid EvaluatorId, Guid AssignedBy, Guid ProjectId) : DomainEventBase;
}
