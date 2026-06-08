using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Group;

namespace TEDF.Domain.Aggregates.GroupAggregate.Events
{
    public sealed record MemberAddedEvent(Guid GroupId, Guid StudentId, GroupMemberRole Role) : DomainEventBase;
}
