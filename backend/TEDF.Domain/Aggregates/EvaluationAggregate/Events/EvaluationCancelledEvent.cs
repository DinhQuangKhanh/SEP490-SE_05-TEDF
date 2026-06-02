using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Events
{
    public sealed record EvaluationCancelledEvent(Guid SubmissionId, Guid ProjectId) : DomainEventBase;
}
