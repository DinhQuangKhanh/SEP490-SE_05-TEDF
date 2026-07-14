using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;

/// <summary>
/// A snapshot of one criterion's pass/fail state within a <see cref="ProjectEvaluationChecklist"/>.
/// The title is copied at creation time so historical results stay stable even if the source
/// checklist configuration is later edited.
/// </summary>
public class ChecklistResultItem : Entity<Guid>
{
    public Guid ProjectEvaluationChecklistId { get; private set; }

    /// <summary>The criterion (from the config version used) this item corresponds to.</summary>
    public Guid CriterionId { get; private set; }

    public int Order { get; private set; }

    /// <summary>Snapshot of the criterion title at the time the result was created.</summary>
    public string TitleVi { get; private set; } = string.Empty;

    public bool IsPassed { get; private set; }

    private ChecklistResultItem() { }

    public static ChecklistResultItem Create(Guid parentId, Guid criterionId, int order, string titleVi, bool isPassed)
    {
        return new ChecklistResultItem
        {
            Id = Guid.NewGuid(),
            ProjectEvaluationChecklistId = parentId,
            CriterionId = criterionId,
            Order = order,
            TitleVi = titleVi,
            IsPassed = isPassed
        };
    }

    internal void SetPassed(bool passed) => IsPassed = passed;
}
