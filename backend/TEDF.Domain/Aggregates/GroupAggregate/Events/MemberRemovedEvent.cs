using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.GroupAggregate.Events
{
    public sealed record MemberRemovedEvent(Guid GroupId, Guid StudentId, Guid RemovedBy) : DomainEventBase;
}
