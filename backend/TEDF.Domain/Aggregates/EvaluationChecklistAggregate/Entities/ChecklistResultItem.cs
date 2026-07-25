using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;

/// <summary>
/// A snapshot of one criterion's score/pass state within a <see cref="ProjectEvaluationChecklist"/>.
/// The title and the scoring bounds (<see cref="MaxScore"/> / <see cref="PassScore"/>) are copied at
/// creation time so historical results stay stable even if the source checklist configuration is later
/// edited. <see cref="IsPassed"/> is always derived by the domain from <see cref="Score"/> — never taken
/// from client input.
/// </summary>
public class ChecklistResultItem : Entity<Guid>
{
    public Guid ProjectEvaluationChecklistId { get; private set; }

    /// <summary>The criterion (from the config version used) this item corresponds to.</summary>
    public Guid CriterionId { get; private set; }

    public int Order { get; private set; }

    /// <summary>Snapshot of the criterion title at the time the result was created.</summary>
    public string TitleVi { get; private set; } = string.Empty;

    /// <summary>Snapshot of the criterion's maximum score.</summary>
    public decimal MaxScore { get; private set; }

    /// <summary>Snapshot of the criterion's pass score.</summary>
    public decimal PassScore { get; private set; }

    /// <summary>The evaluator's score for this criterion. Null until the evaluator scores it.</summary>
    public decimal? Score { get; private set; }

    /// <summary>The evaluator's per-criterion comment. Null when empty.</summary>
    public string? Comment { get; private set; }

    /// <summary>Derived by the domain: true when <see cref="Score"/> is set and &gt;= <see cref="PassScore"/>.</summary>
    public bool IsPassed { get; private set; }

    private ChecklistResultItem() { }

    public static ChecklistResultItem Create(
        Guid parentId, Guid criterionId, int order, string titleVi, decimal maxScore, decimal passScore)
    {
        return new ChecklistResultItem
        {
            Id = Guid.NewGuid(),
            ProjectEvaluationChecklistId = parentId,
            CriterionId = criterionId,
            Order = order,
            TitleVi = titleVi,
            MaxScore = maxScore,
            PassScore = passScore,
            Score = null,
            Comment = null,
            IsPassed = false
        };
    }

    /// <summary>
    /// Applies the evaluator's score + comment for this item. Validates the score is within
    /// [0, <see cref="MaxScore"/>] against the snapshot bounds, then recomputes <see cref="IsPassed"/>.
    /// </summary>
    internal void ApplyScore(decimal? score, string? comment)
    {
        if (score.HasValue)
        {
            if (score.Value < 0)
                throw new BusinessRuleValidationException(
                    $"Điểm chấm cho tiêu chí \"{TitleVi}\" không được âm.");
            if (score.Value > MaxScore)
                throw new BusinessRuleValidationException(
                    $"Điểm chấm cho tiêu chí \"{TitleVi}\" không được vượt quá điểm tối đa ({MaxScore}).");
        }

        Score = score;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        IsPassed = score.HasValue && score.Value >= PassScore;
    }
}
