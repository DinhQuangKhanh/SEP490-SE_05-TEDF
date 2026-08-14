namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

/// <summary>
/// Ordered specification of a single checklist criterion, used when creating or replacing the criteria
/// of a <see cref="ChecklistConfig"/>. Layer-neutral (no infrastructure types) so it can be built from an
/// Excel import, a manual form, or a copy of another config. The domain assigns the final 1-based order.
/// Scoring has been replaced by a simple Pass/Fail evaluation per criterion.
/// </summary>
public readonly record struct ChecklistCriterionSpec(
    string TitleVi,
    string TitleEn,
    string? Description);
