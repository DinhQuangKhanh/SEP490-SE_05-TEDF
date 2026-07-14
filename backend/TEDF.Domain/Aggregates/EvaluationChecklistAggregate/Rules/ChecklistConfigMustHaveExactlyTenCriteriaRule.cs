using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Rules;

/// <summary>
/// Rule: a checklist configuration must contain exactly <see cref="ChecklistConfig.RequiredCriteriaCount"/>
/// criteria before it can be activated.
/// </summary>
public sealed class ChecklistConfigMustHaveExactlyTenCriteriaRule : IBusinessRule
{
    private readonly int _criteriaCount;

    public ChecklistConfigMustHaveExactlyTenCriteriaRule(int criteriaCount)
    {
        _criteriaCount = criteriaCount;
    }

    public bool IsBroken() => _criteriaCount != ChecklistConfig.RequiredCriteriaCount;

    public string Message =>
        $"Checklist phải có đúng {ChecklistConfig.RequiredCriteriaCount} tiêu chí để được kích hoạt (hiện có {_criteriaCount}).";
}
