namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

/// <summary>
/// One evaluator evaluation entry for a criterion: a Pass/Fail decision and an optional
/// per-criterion comment. The domain uses this to set the item's pass state directly.
/// </summary>
public readonly record struct ChecklistEvaluationEntry(Guid CriterionId, bool IsPassed, string? Comment);
