using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record ProjectCreatedEvent(Guid ProjectId, string ProjectCode, ProjectSourceType SourceType) : DomainEventBase;
}
