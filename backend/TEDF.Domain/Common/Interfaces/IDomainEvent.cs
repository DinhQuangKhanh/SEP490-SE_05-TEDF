using MediatR;

namespace TEDF.Domain.Common.Interfaces
{
    /// <summary>
    /// Marker interface for domain events. Inherits from MediatR's INotification for automatic publishing.
    /// </summary>
    public interface IDomainEvent : INotification
    {
        /// <summary>
        /// Gets the unique identifier of the event.
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// Gets the date and time when the event occurred.
        /// </summary>
        DateTime OccurredOn { get; }
    }

    /// <summary>
    /// Base record for domain events providing default implementations.
    /// </summary>
    public abstract record DomainEventBase : IDomainEvent
    {
        /// <inheritdoc />
        public Guid EventId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
