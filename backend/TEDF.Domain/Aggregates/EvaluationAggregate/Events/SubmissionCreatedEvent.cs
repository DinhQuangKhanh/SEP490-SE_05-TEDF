using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Events
{
    public sealed record SubmissionCreatedEvent(Guid SubmissionId, Guid ProjectId, int PhaseId, Guid SubmittedBy, int SubmissionNumber) : DomainEventBase;
}
