namespace TEDF.Application.Features.EvaluationChecklists.DTOs;

/// <summary>
/// One criterion row on the evaluator's checklist, with its scoring bounds and the evaluator's saved
/// score/comment. <see cref="IsPassed"/> is server-computed (Score &gt;= PassScore).
/// </summary>
public record ProjectChecklistItemDto(
    Guid CriterionId,
    int Order,
    string TitleVi,
    string TitleEn,
    string? Description,
    decimal MaxScore,
    decimal PassScore,
    decimal? Score,
    string? Comment,
    bool IsPassed);

/// <summary>
/// The evaluator's checklist for a project: the applicable criteria plus the evaluator's saved
/// scores/comments and the derived approval eligibility.
/// </summary>
public record ProjectChecklistDto(
    Guid ProjectId,
    bool HasActiveConfig,
    Guid? ConfigId,
    int? Version,
    int TotalCriteria,
    int RequiredPassCount,
    int PassedCount,
    bool CanApprove,
    bool IsSaved,
    string? EvaluatorNote,
    DateTime? UpdatedAt,
    IReadOnlyList<ProjectChecklistItemDto> Items);
