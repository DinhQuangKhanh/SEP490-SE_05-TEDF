using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record ProjectSubmittedToMentorEvent(Guid ProjectId, Guid SubmittedBy) : DomainEventBase;
}
