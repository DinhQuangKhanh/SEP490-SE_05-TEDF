using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.GroupAggregate.Events
{
    public sealed record InvitationRejectedEvent(Guid GroupId, string GroupCode, Guid InviterId, Guid InviteeId) : DomainEventBase;
}
