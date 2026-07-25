using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;

/// <summary>
/// A single criterion inside a <see cref="ChecklistConfig"/>. Part of the ChecklistConfig aggregate;
/// only mutated through the aggregate root. Carries the per-criterion scoring configuration
/// (<see cref="MaxScore"/> / <see cref="PassScore"/>) the Department Head imports from Excel.
/// </summary>
public class ChecklistCriterion : Entity<Guid>
{
    public Guid ChecklistConfigId { get; private set; }

    /// <summary>1-based display order within the checklist.</summary>
    public int Order { get; private set; }

    public string TitleVi { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>Maximum score an evaluator can give for this criterion. Must be &gt; 0.</summary>
    public decimal MaxScore { get; private set; }

    /// <summary>Minimum score for this criterion to count as passed. In range [0, MaxScore].</summary>
    public decimal PassScore { get; private set; }

    private ChecklistCriterion() { }

    public static ChecklistCriterion Create(
        Guid checklistConfigId, int order, string titleVi, string titleEn, string? description,
        decimal maxScore, decimal passScore)
    {
        if (string.IsNullOrWhiteSpace(titleVi))
            throw new BusinessRuleValidationException("Tên tiêu chí (tiếng Việt) không được để trống.");
        if (maxScore <= 0)
            throw new BusinessRuleValidationException(
                $"Điểm tối đa của tiêu chí \"{titleVi.Trim()}\" phải lớn hơn 0.");
        if (passScore < 0)
            throw new BusinessRuleValidationException(
                $"Điểm đạt của tiêu chí \"{titleVi.Trim()}\" không được âm.");
        if (passScore > maxScore)
            throw new BusinessRuleValidationException(
                $"Điểm đạt của tiêu chí \"{titleVi.Trim()}\" không được lớn hơn điểm tối đa ({maxScore}).");

        return new ChecklistCriterion
        {
            Id = Guid.NewGuid(),
            ChecklistConfigId = checklistConfigId,
            Order = order,
            TitleVi = titleVi.Trim(),
            TitleEn = titleEn.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            MaxScore = maxScore,
            PassScore = passScore
        };
    }
}
