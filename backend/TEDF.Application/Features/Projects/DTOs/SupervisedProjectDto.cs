namespace TEDF.Application.Features.Projects.DTOs;

/// <summary>A project the authenticated user supervises as an active mentor (profile supervision history).</summary>
public record SupervisedProjectDto(
    Guid Id,
    string Code,
    string NameVi,
    string? NameEn,
    string Status,
    int StatusValue,
    string SemesterName,
    string? GroupCode,
    DateTime? StartDate,
    DateTime? Deadline,
    DateTime CreatedAt);

public record GetMySupervisedProjectsResult(
    IReadOnlyList<SupervisedProjectDto> Items,
    int TotalCount);
