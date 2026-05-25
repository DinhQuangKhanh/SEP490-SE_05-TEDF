using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.SupportAggregate.Events
{
    public sealed record TicketReopenedEvent(Guid TicketId) : DomainEventBase;
}
