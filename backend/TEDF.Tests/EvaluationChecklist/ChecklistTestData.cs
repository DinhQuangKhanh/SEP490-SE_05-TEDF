using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

namespace TEDF.Tests.EvaluationChecklist;

/// <summary>Helpers for building checklist aggregates in tests.</summary>
internal static class ChecklistTestData
{
    public static IEnumerable<(string TitleVi, string TitleEn, string? Description)> Criteria(int count)
    {
        for (var i = 1; i <= count; i++)
            yield return ($"Tiêu chí {i}", $"Criterion {i}", $"Mô tả {i}");
    }

    public static ChecklistConfig ActiveConfig(int semesterId = 101, int criteriaCount = 10)
    {
        var config = ChecklistConfig.Create(semesterId, version: 1, Criteria(criteriaCount));
        config.Activate();
        return config;
    }

    /// <summary>Builds a saved result marking the first <paramref name="passedCount"/> criteria as passed.</summary>
    public static ProjectEvaluationChecklist ResultWithPassed(ChecklistConfig config, int passedCount)
    {
        var result = ProjectEvaluationChecklist.CreateFromConfig(config, Guid.NewGuid(), Guid.NewGuid(), submissionNumber: 1);
        var passedIds = config.Criteria.OrderBy(c => c.Order).Take(passedCount).Select(c => c.Id).ToList();
        result.ApplyPassedCriteria(passedIds, note: null);
        return result;
    }
}
