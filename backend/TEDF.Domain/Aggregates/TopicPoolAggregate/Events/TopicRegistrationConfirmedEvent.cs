using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Events
{
    /// <summary>
    /// Event raised when a topic registration is confirmed by mentor.
    /// </summary>
    public sealed record TopicRegistrationConfirmedEvent(
        Guid RegistrationId,
        Guid ProjectId,
        Guid GroupId,
        Guid ConfirmedBy
    ) : DomainEventBase;
}
