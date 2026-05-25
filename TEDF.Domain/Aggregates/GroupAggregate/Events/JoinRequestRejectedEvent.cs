using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.GroupAggregate.Events
{
    public sealed record JoinRequestRejectedEvent(Guid GroupId, string GroupCode, Guid StudentId) : DomainEventBase;
}
