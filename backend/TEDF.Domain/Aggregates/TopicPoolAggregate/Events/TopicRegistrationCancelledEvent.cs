using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Events
{
    /// <summary>
    /// Event raised when a topic registration is cancelled.
    /// </summary>
    public sealed record TopicRegistrationCancelledEvent(
        Guid RegistrationId,
        Guid ProjectId,
        Guid GroupId,
        string? Reason
    ) : DomainEventBase;
}
