using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Domain.Aggregates.SupportAggregate.Events
{
    public sealed record TicketCreatedEvent(Guid TicketId, string TicketCode, TicketCategory Category, TicketPriority Priority) : DomainEventBase;
}
