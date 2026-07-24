using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events;

public sealed record DocumentDeletedEvent(Guid ProjectId, Guid DocumentId, Guid DeletedBy) : DomainEventBase;
