using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using Xunit;

namespace TEDF.Tests.EvaluationChecklist;

public class ProjectEvaluationChecklistTests
{
    [Fact]
    public void CreateFromConfig_SnapshotsAllCriteriaUnpassed()
    {
        var config = ChecklistTestData.ActiveConfig();

        var result = ProjectEvaluationChecklist.CreateFromConfig(config, Guid.NewGuid(), Guid.NewGuid(), 1);

        Assert.Equal(10, result.Items.Count);
        Assert.All(result.Items, i => Assert.False(i.IsPassed));
        Assert.Equal(0, result.PassedCount);
        Assert.Equal(ChecklistConfig.DefaultPassThreshold, result.RequiredPassCount);
    }

    [Fact]
    public void ApplyPassedCriteria_CountsDistinctValidIds()
    {
        var config = ChecklistTestData.ActiveConfig();
        var result = ProjectEvaluationChecklist.CreateFromConfig(config, Guid.NewGuid(), Guid.NewGuid(), 1);
        var ids = config.Criteria.OrderBy(c => c.Order).Select(c => c.Id).ToList();

        result.ApplyPassedCriteria(new[] { ids[0], ids[1], ids[2] }, note: null);

        Assert.Equal(3, result.PassedCount);
    }

    [Fact]
    public void ApplyPassedCriteria_DuplicateIdsDoNotInflateCount()
    {
        var config = ChecklistTestData.ActiveConfig();
        var result = ProjectEvaluationChecklist.CreateFromConfig(config, Guid.NewGuid(), Guid.NewGuid(), 1);
        var id = config.Criteria.First().Id;

        result.ApplyPassedCriteria(new[] { id, id, id }, note: null);

        Assert.Equal(1, result.PassedCount);
    }

    [Fact]
    public void ApplyPassedCriteria_UnknownIdsAreIgnored()
    {
        var config = ChecklistTestData.ActiveConfig();
        var result = ProjectEvaluationChecklist.CreateFromConfig(config, Guid.NewGuid(), Guid.NewGuid(), 1);

        result.ApplyPassedCriteria(new[] { Guid.NewGuid(), Guid.NewGuid() }, note: null);

        Assert.Equal(0, result.PassedCount);
    }

    [Fact]
    public void MeetsApprovalThreshold_TransitionsAtSeven()
    {
        var config = ChecklistTestData.ActiveConfig();

        Assert.False(ChecklistTestData.ResultWithPassed(config, 6).MeetsApprovalThreshold);
        Assert.True(ChecklistTestData.ResultWithPassed(config, 7).MeetsApprovalThreshold);
    }

    [Fact]
    public void SavedResult_IsUnaffectedByLaterConfigEdits()
    {
        // Snapshot captured at creation time must not change when the source config is later edited.
        var config = ChecklistConfig.Create(101, 1, ChecklistTestData.Criteria(10));
        var result = ProjectEvaluationChecklist.CreateFromConfig(config, Guid.NewGuid(), Guid.NewGuid(), 1);
        var originalTitles = result.Items.OrderBy(i => i.Order).Select(i => i.TitleVi).ToList();

        // Edit the (still-Draft) config's criteria entirely.
        config.ReplaceCriteria(new[] { ("Đã đổi", "Changed", (string?)"x") });

        var titlesAfter = result.Items.OrderBy(i => i.Order).Select(i => i.TitleVi).ToList();
        Assert.Equal(originalTitles, titlesAfter);
        Assert.Equal(10, result.Items.Count);
    }
}
