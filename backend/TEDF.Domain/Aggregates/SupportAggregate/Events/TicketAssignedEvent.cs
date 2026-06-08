using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.SupportAggregate.Events
{
    public sealed record TicketAssignedEvent(Guid TicketId, Guid AssigneeId) : DomainEventBase;
}
