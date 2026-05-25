using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record ProjectStartedEvent(Guid ProjectId, DateTime StartDate, DateTime Deadline) : DomainEventBase;
}
