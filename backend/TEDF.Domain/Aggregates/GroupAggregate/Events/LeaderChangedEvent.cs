using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.GroupAggregate.Events
{
    public sealed record LeaderChangedEvent(Guid GroupId, Guid? OldLeaderId, Guid NewLeaderId) : DomainEventBase;
}
