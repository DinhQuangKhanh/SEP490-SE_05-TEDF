using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record ProjectMentorApprovedEvent(Guid ProjectId, Guid MentorId) : DomainEventBase;
}
