using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Events
{
    public sealed record TopicPoolSuspendedEvent(Guid TopicPoolId) : DomainEventBase;
}
