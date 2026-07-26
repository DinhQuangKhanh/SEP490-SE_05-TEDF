using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Events;

/// <summary>
/// Raised when an evaluator saves (upserts) their checklist result for a project. Dispatched after the
/// unit of work commits, so any real-time notification fires only once the data is persisted.
/// </summary>
public sealed record ProjectChecklistSavedEvent(
    Guid ProjectId,
    Guid EvaluatorId,
    int SubmissionNumber
) : DomainEventBase;
