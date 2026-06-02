using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Events
{
    /// <summary>
    /// Event raised when a new topic pool is created for a major.
    /// </summary>
    public sealed record TopicPoolCreatedEvent(
        Guid TopicPoolId,
        string Code,
        int MajorId
    ) : DomainEventBase;
}
