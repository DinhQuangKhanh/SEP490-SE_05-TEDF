using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Rules;

/// <summary>
/// Rule: an evaluator may only approve a topic when their saved checklist meets the required
/// passed-criteria threshold. Broken when no checklist has been saved, or when too few criteria pass.
/// This is the server-side guard that makes the 7/10 requirement impossible to bypass via direct API calls.
/// </summary>
public sealed class ChecklistApprovalThresholdRule : IBusinessRule
{
    private readonly ProjectEvaluationChecklist? _checklist;
    private readonly int _passed;
    private readonly int _total;
    private readonly int _required;

    public ChecklistApprovalThresholdRule(
        ProjectEvaluationChecklist? checklist,
        int fallbackRequired = ChecklistConfig.DefaultPassThreshold,
        int fallbackTotal = ChecklistConfig.RequiredCriteriaCount)
    {
        _checklist = checklist;
        _passed = checklist?.PassedCount ?? 0;
        _total = checklist is null || checklist.Items.Count == 0 ? fallbackTotal : checklist.Items.Count;
        _required = checklist?.RequiredPassCount ?? fallbackRequired;
    }

    public bool IsBroken() => _checklist is null || _checklist.PassedCount < _checklist.RequiredPassCount;

    public string Code => "CHECKLIST_THRESHOLD_NOT_MET";

    public string Message =>
        $"Không thể duyệt đề tài vì checklist thẩm định chỉ đạt {_passed}/{_total} tiêu chí. Yêu cầu tối thiểu là {_required} tiêu chí.";
}
