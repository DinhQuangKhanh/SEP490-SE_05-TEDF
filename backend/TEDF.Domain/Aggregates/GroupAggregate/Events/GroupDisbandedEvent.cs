using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.GroupAggregate.Events
{
    /// <summary>
    /// The group was disbanded by its leader. <paramref name="MemberIds"/> is the set of members who
    /// were still active at that moment — they are all dropped by the disband, so the handler needs
    /// them captured here rather than re-reading the (now emptied) membership.
    /// </summary>
    public sealed record GroupDisbandedEvent(
        Guid GroupId,
        string GroupCode,
        IReadOnlyCollection<Guid> MemberIds) : DomainEventBase;
}
