using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Events
{
    /// <summary>
    /// Event raised when a topic registration is rejected by mentor.
    /// </summary>
    public sealed record TopicRegistrationRejectedEvent(
        Guid RegistrationId,
        Guid ProjectId,
        Guid GroupId,
        Guid RejectedBy,
        string Reason
    ) : DomainEventBase;
}
