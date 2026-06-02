using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.SupportAggregate.Events
{
    public sealed record TicketClosedEvent(Guid TicketId) : DomainEventBase;
}
