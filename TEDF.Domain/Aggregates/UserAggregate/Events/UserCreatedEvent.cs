using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.UserAggregate.Events
{
    /// <summary>
    /// Domain event raised when a new user is created.
    /// </summary>
    public sealed record UserCreatedEvent(
        Guid UserId,
        string Email,
        string FullName,
        string FirebaseUid
    ) : DomainEventBase;
}
