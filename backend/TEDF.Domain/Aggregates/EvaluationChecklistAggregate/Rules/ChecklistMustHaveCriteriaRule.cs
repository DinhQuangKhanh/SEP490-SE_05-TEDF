using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Rules;

/// <summary>
/// Rule: a checklist configuration must contain at least one criterion before it can be activated.
/// (The number of criteria is otherwise unbounded — it comes from the imported Excel file.)
/// </summary>
public sealed class ChecklistMustHaveCriteriaRule : IBusinessRule
{
    private readonly int _criteriaCount;

    public ChecklistMustHaveCriteriaRule(int criteriaCount)
    {
        _criteriaCount = criteriaCount;
    }

    public bool IsBroken() => _criteriaCount < 1;

    public string Message => "Checklist phải có ít nhất một tiêu chí để được kích hoạt.";
}
