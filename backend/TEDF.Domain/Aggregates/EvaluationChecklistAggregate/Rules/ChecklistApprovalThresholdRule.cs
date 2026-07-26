using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Rules;

/// <summary>
/// Rule: an evaluator may only approve a topic when their saved checklist meets the required
/// passed-criteria threshold. Broken when no checklist has been saved, or when too few criteria pass.
/// The threshold and totals come entirely from the saved checklist snapshot (no hard-coded numbers) —
/// this is the server-side guard that makes the requirement impossible to bypass via direct API calls.
/// </summary>
public sealed class ChecklistApprovalThresholdRule : IBusinessRule
{
    private readonly ProjectEvaluationChecklist? _checklist;
    private readonly int _passed;
    private readonly int _total;
    private readonly int _required;

    public ChecklistApprovalThresholdRule(ProjectEvaluationChecklist? checklist)
    {
        _checklist = checklist;
        _passed = checklist?.PassedCount ?? 0;
        _total = checklist?.Items.Count ?? 0;
        _required = checklist?.RequiredPassCount ?? 0;
    }

    public bool IsBroken() => _checklist is null || _checklist.PassedCount < _checklist.RequiredPassCount;

    public string Code => "CHECKLIST_THRESHOLD_NOT_MET";

    public string Message => _checklist is null
        ? "Không thể duyệt đề tài vì chưa lưu kết quả checklist thẩm định."
        : $"Không thể duyệt đề tài vì checklist thẩm định chỉ đạt {_passed}/{_total} tiêu chí. Yêu cầu tối thiểu là {_required} tiêu chí.";
}
