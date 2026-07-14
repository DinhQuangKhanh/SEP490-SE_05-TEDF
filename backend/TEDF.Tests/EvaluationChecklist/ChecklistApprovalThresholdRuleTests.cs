using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Rules;
using Xunit;

namespace TEDF.Tests.EvaluationChecklist;

public class ChecklistApprovalThresholdRuleTests
{
    [Fact]
    public void IsBroken_WhenNoChecklistSaved_ReturnsTrue()
    {
        var rule = new ChecklistApprovalThresholdRule(checklist: null);
        Assert.True(rule.IsBroken());
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    [InlineData(8, false)]
    [InlineData(10, false)]
    public void IsBroken_ReflectsSevenOfTenThreshold(int passedCount, bool expectedBroken)
    {
        var config = ChecklistTestData.ActiveConfig();
        var result = ChecklistTestData.ResultWithPassed(config, passedCount);

        var rule = new ChecklistApprovalThresholdRule(result);

        Assert.Equal(expectedBroken, rule.IsBroken());
    }

    [Fact]
    public void Message_IncludesActualAndRequiredCounts()
    {
        var config = ChecklistTestData.ActiveConfig();
        var result = ChecklistTestData.ResultWithPassed(config, 6);

        var rule = new ChecklistApprovalThresholdRule(result);

        Assert.Contains("6/10", rule.Message);
        Assert.Contains("7", rule.Message);
    }
}
