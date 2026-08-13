using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.GroupAggregate.Events
{
    /// <summary>
    /// A member left the group on their own. Distinct from <see cref="MemberRemovedEvent"/>, which the
    /// leader triggers — here the leader is the one who needs to be told.
    /// </summary>
    public sealed record MemberLeftEvent(Guid GroupId, string GroupCode, Guid StudentId, Guid? LeaderId) : DomainEventBase;
}
