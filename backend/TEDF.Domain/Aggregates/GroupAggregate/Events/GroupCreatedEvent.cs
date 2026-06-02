using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.GroupAggregate.Events
{
    public sealed record GroupCreatedEvent(Guid GroupId, string GroupCode, int SemesterId) : DomainEventBase;
}
