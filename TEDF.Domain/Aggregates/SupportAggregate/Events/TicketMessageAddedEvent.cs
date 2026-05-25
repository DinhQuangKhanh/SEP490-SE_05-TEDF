using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.SupportAggregate.Events
{
    /// <summary>
    /// Event raised when a new message/reply is added to a support ticket.
    /// </summary>
    public sealed record TicketMessageAddedEvent(
        Guid TicketId,
        Guid MessageId,
        Guid SenderId) : DomainEventBase;
}

