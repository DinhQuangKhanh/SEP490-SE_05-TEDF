using TEDF.Application.Features.EvaluationChecklists.DTOs;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

namespace TEDF.Application.Features.EvaluationChecklists;

/// <summary>Maps <see cref="ChecklistConfig"/> aggregates to their Department-Head DTOs.</summary>
internal static class ChecklistConfigMapper
{
    public static ChecklistConfigDto ToDto(
        ChecklistConfig config,
        string semesterName,
        bool isUsed,
        IReadOnlyDictionary<Guid, string> userNames)
    {
        var criteria = config.Criteria
            .OrderBy(c => c.Order)
            .Select(c => new ChecklistCriterionDto(c.Id, c.Order, c.TitleVi, c.TitleEn, c.Description))
            .ToList();

        return new ChecklistConfigDto(
            Id: config.Id,
            SemesterId: config.SemesterId,
            SemesterName: semesterName,
            Version: config.Version,
            Status: config.Status.ToString(),
            PassThreshold: config.PassThreshold,
            CriteriaCount: criteria.Count,
            IsUsed: isUsed,
            CreatedAt: config.CreatedAt,
            CreatedBy: config.CreatedBy,
            CreatedByName: Lookup(userNames, config.CreatedBy),
            UpdatedAt: config.UpdatedAt,
            UpdatedBy: config.UpdatedBy,
            UpdatedByName: Lookup(userNames, config.UpdatedBy),
            Criteria: criteria);
    }

    private static string? Lookup(IReadOnlyDictionary<Guid, string> names, Guid? id)
        => id.HasValue && names.TryGetValue(id.Value, out var name) ? name : null;
}
