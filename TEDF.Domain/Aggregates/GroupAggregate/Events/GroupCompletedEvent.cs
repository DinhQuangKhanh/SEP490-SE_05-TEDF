using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.GroupAggregate.Events
{
    public sealed record GroupCompletedEvent(Guid GroupId) : DomainEventBase;
}
