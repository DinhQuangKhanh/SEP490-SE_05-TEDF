using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record ProjectCancelledEvent(Guid ProjectId) : DomainEventBase;
}
