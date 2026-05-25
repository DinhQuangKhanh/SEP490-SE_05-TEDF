using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Events
{
    /// <summary>
    /// Event raised when a topic in the pool expires (after 2 semesters without registration).
    /// </summary>
    public sealed record PoolTopicExpiredEvent(
        Guid ProjectId,
        Guid TopicPoolId,
        int CreatedSemesterId,
        int ExpiredAtSemesterId
    ) : DomainEventBase;
}
