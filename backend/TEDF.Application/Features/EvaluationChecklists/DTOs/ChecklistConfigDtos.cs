namespace TEDF.Application.Features.EvaluationChecklists.DTOs;

/// <summary>A single criterion inside a checklist configuration.</summary>
public record ChecklistCriterionDto(
    Guid Id,
    int Order,
    string TitleVi,
    string TitleEn,
    string? Description,
    decimal MaxScore,
    decimal PassScore);

/// <summary>A checklist configuration (Department-Head view), including its criteria.</summary>
public record ChecklistConfigDto(
    Guid Id,
    int SemesterId,
    string SemesterName,
    int Version,
    string Status,
    int RequiredPassCount,
    int CriteriaCount,
    string? SourceFileName,
    bool IsUsed,
    DateTime CreatedAt,
    Guid? CreatedBy,
    string? CreatedByName,
    DateTime? UpdatedAt,
    Guid? UpdatedBy,
    string? UpdatedByName,
    IReadOnlyList<ChecklistCriterionDto> Criteria);

/// <summary>Lightweight semester option for the checklist management screen.</summary>
public record ChecklistSemesterOptionDto(
    int Id,
    string Name,
    string Code,
    string Status);

/// <summary>Payload powering the Department-Head checklist management screen.</summary>
public record ChecklistConfigListDto(
    IReadOnlyList<ChecklistSemesterOptionDto> Semesters,
    IReadOnlyList<ChecklistConfigDto> Configs);

/// <summary>One default criterion (used to prefill the "create checklist" form / Excel template).</summary>
public record ChecklistCriterionSeedDto(
    int Order,
    string TitleVi,
    string TitleEn,
    string Description,
    decimal MaxScore,
    decimal PassScore);
