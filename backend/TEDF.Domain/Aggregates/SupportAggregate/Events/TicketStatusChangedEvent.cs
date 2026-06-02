using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Domain.Aggregates.SupportAggregate.Events
{
    public sealed record TicketStatusChangedEvent(Guid TicketId, TicketStatus OldStatus, TicketStatus NewStatus) : DomainEventBase;
}
