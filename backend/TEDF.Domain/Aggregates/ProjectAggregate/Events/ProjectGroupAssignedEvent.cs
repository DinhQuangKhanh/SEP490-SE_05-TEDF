using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record ProjectGroupAssignedEvent(Guid ProjectId, Guid GroupId) : DomainEventBase;
}
