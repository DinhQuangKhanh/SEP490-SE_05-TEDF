using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record MentorRemovedEvent(Guid ProjectId, Guid MentorId) : DomainEventBase;
}
