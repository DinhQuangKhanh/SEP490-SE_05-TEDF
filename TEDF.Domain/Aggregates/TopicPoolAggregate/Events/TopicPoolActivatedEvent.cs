using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Events
{
    public sealed record TopicPoolActivatedEvent(Guid TopicPoolId) : DomainEventBase;
}
