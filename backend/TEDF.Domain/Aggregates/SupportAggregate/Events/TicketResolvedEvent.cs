using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.SupportAggregate.Events
{
    public sealed record TicketResolvedEvent(Guid TicketId) : DomainEventBase;
}
