namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

/// <summary>
/// One evaluator score entry for a criterion: the raw score (null = not yet scored) and an optional
/// per-criterion comment. The domain validates the score against the criterion's snapshot bounds and
/// derives the pass state — this is never trusted as a pass flag from the client.
/// </summary>
public readonly record struct ChecklistScoreEntry(Guid CriterionId, decimal? Score, string? Comment);
