using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.GroupAggregate.Events
{
    public sealed record JoinRequestedEvent(Guid GroupId, string GroupCode, Guid StudentId, Guid? LeaderId) : DomainEventBase;
}
