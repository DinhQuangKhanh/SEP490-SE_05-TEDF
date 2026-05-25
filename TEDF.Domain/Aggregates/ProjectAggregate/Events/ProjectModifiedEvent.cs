using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record ProjectModifiedEvent(Guid ProjectId) : DomainEventBase;
}
