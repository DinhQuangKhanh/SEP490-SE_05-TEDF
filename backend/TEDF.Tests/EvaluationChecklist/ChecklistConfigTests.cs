using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Enums.Evaluation;
using Xunit;

namespace TEDF.Tests.EvaluationChecklist;

public class ChecklistConfigTests
{
    [Fact]
    public void Activate_WithFewerThanTenCriteria_Throws()
    {
        var config = ChecklistConfig.Create(101, 1, ChecklistTestData.Criteria(9));

        var ex = Assert.Throws<BusinessRuleValidationException>(() => config.Activate());
        Assert.Contains("10", ex.Message);
        Assert.Equal(ChecklistConfigStatus.Draft, config.Status);
    }

    [Fact]
    public void Activate_WithMoreThanTenCriteria_Throws()
    {
        var config = ChecklistConfig.Create(101, 1, ChecklistTestData.Criteria(11));
        Assert.Throws<BusinessRuleValidationException>(() => config.Activate());
    }

    [Fact]
    public void Activate_WithExactlyTenCriteria_Succeeds()
    {
        var config = ChecklistConfig.Create(101, 1, ChecklistTestData.Criteria(10));

        config.Activate();

        Assert.Equal(ChecklistConfigStatus.Active, config.Status);
    }

    [Fact]
    public void ReplaceCriteria_OnActiveConfig_Throws()
    {
        var config = ChecklistTestData.ActiveConfig();

        Assert.Throws<BusinessRuleValidationException>(
            () => config.ReplaceCriteria(ChecklistTestData.Criteria(10)));
    }

    [Fact]
    public void ReplaceCriteria_ReordersAndRenumbers()
    {
        var config = ChecklistConfig.Create(101, 1, new[]
        {
            ("A", "A", (string?)null),
            ("B", "B", (string?)null),
        });

        config.ReplaceCriteria(new[]
        {
            ("B", "B", (string?)null),
            ("A", "A", (string?)null),
        });

        var ordered = config.Criteria.OrderBy(c => c.Order).ToList();
        Assert.Equal("B", ordered[0].TitleVi);
        Assert.Equal(1, ordered[0].Order);
        Assert.Equal("A", ordered[1].TitleVi);
        Assert.Equal(2, ordered[1].Order);
    }

    [Fact]
    public void CopyTo_ProducesDraftWithSameCriteria()
    {
        var source = ChecklistTestData.ActiveConfig(semesterId: 101);

        var copy = source.CopyTo(targetSemesterId: 102, version: 3);

        Assert.Equal(ChecklistConfigStatus.Draft, copy.Status);
        Assert.Equal(102, copy.SemesterId);
        Assert.Equal(3, copy.Version);
        Assert.Equal(source.Criteria.Count, copy.Criteria.Count);
        Assert.NotEqual(source.Id, copy.Id);
        Assert.Equal(
            source.Criteria.OrderBy(c => c.Order).Select(c => c.TitleVi),
            copy.Criteria.OrderBy(c => c.Order).Select(c => c.TitleVi));
    }
}
