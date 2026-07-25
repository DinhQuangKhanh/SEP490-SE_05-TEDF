using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Events;

/// <summary>Raised when a new checklist configuration (draft) is created for a semester.</summary>
public sealed record ChecklistConfigCreatedEvent(
    Guid ChecklistConfigId,
    int SemesterId,
    int Version
) : DomainEventBase;
