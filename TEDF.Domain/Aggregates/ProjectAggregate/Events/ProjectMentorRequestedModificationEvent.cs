using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record ProjectMentorRequestedModificationEvent(Guid ProjectId, string? Feedback) : DomainEventBase;
}
