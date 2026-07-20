using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Persistence.SqlServer;

namespace TEDF.Persistence.Seeds;

/// <summary>
/// Seeds a default Active evaluation checklist (the 10 default criteria, threshold 7/10) for any
/// semester that does not yet have any checklist configuration. Idempotent and additive: existing
/// configurations are never modified, so this is safe to run on every startup. Ensures evaluators are
/// not blocked before a Department Head has manually configured a checklist.
/// </summary>
public static class EvaluationChecklistSeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger? logger = null)
    {
        var semesterIds = await context.Set<Semester>()
            .Select(s => s.Id)
            .ToListAsync();

        if (semesterIds.Count == 0)
            return;

        var configuredSemesterIds = await context.Set<ChecklistConfig>()
            .Select(c => c.SemesterId)
            .Distinct()
            .ToListAsync();

        var missing = semesterIds.Except(configuredSemesterIds).ToList();
        if (missing.Count == 0)
            return;

        foreach (var semesterId in missing)
        {
            var criteria = DefaultChecklistCriteria.Items
                .Select(i => new ChecklistCriterionSpec(
                    i.TitleVi, i.TitleEn, i.Description,
                    DefaultChecklistCriteria.DefaultMaxScore, DefaultChecklistCriteria.DefaultPassScore));

            var config = ChecklistConfig.Create(
                semesterId, version: 1, criteria, DefaultChecklistCriteria.DefaultRequiredPassCount);
            config.Activate();
            await context.Set<ChecklistConfig>().AddAsync(config);
        }

        await context.SaveChangesAsync();
        logger?.LogInformation("Seeded default evaluation checklist for {Count} semester(s).", missing.Count);
    }
}
