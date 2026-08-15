using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;

/// <summary>
/// A single criterion inside a <see cref="ChecklistConfig"/>. Part of the ChecklistConfig aggregate;
/// only mutated through the aggregate root. Criteria carry no scoring configuration — an evaluator
/// marks each one Pass or Fail.
/// </summary>
public class ChecklistCriterion : Entity<Guid>
{
    public Guid ChecklistConfigId { get; private set; }

    /// <summary>1-based display order within the checklist.</summary>
    public int Order { get; private set; }

    public string TitleVi { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private ChecklistCriterion() { }

    public static ChecklistCriterion Create(
        Guid checklistConfigId, int order, string titleVi, string titleEn, string? description)
    {
        if (string.IsNullOrWhiteSpace(titleVi))
            throw new BusinessRuleValidationException("Tên tiêu chí (tiếng Việt) không được để trống.");

        return new ChecklistCriterion
        {
            Id = Guid.NewGuid(),
            ChecklistConfigId = checklistConfigId,
            Order = order,
            TitleVi = titleVi.Trim(),
            TitleEn = (titleEn ?? string.Empty).Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };
    }

    /// <summary>Mutates this criterion in place. Only callable through <see cref="ChecklistConfig"/>.</summary>
    internal void Update(int order, string titleVi, string titleEn, string? description)
    {
        if (string.IsNullOrWhiteSpace(titleVi))
            throw new BusinessRuleValidationException("Tên tiêu chí (tiếng Việt) không được để trống.");

        Order = order;
        TitleVi = titleVi.Trim();
        TitleEn = (titleEn ?? string.Empty).Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
