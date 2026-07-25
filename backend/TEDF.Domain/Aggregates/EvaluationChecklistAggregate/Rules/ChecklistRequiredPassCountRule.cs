using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Rules;

/// <summary>
/// Rule: the configured "minimum criteria to pass" must be greater than 0 and must not exceed the total
/// number of criteria in the checklist. Replaces the old hard-coded 7-of-10 requirement.
/// </summary>
public sealed class ChecklistRequiredPassCountRule : IBusinessRule
{
    private readonly int _requiredPassCount;
    private readonly int _criteriaCount;

    public ChecklistRequiredPassCountRule(int requiredPassCount, int criteriaCount)
    {
        _requiredPassCount = requiredPassCount;
        _criteriaCount = criteriaCount;
    }

    public bool IsBroken() => _requiredPassCount < 1 || _requiredPassCount > _criteriaCount;

    public string Message =>
        $"Số tiêu chí tối thiểu cần đạt phải nằm trong khoảng 1 đến {_criteriaCount} (hiện là {_requiredPassCount}).";
}
