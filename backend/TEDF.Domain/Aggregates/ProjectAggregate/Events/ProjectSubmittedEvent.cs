using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record ProjectSubmittedEvent(Guid ProjectId, Guid SubmittedBy, int SubmissionNumber) : DomainEventBase;
}
