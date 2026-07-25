using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Events;

/// <summary>Raised when a checklist configuration becomes the Active checklist for a semester.</summary>
public sealed record ChecklistConfigActivatedEvent(
    Guid ChecklistConfigId,
    int SemesterId,
    int Version
) : DomainEventBase;
