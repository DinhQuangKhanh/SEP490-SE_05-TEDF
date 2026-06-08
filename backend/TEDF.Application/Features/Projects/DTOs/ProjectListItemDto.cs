namespace TEDF.Application.Features.Projects.DTOs;

public record ProjectListItemDto(
    Guid Id,
    string Code,
    string NameVi,
    string? NameEn,
    string Status,
    string MajorName,
    string MajorCode,
    string SemesterName,
    string SourceType,
    List<string> MentorNames,
    List<string> StudentNames,
    string? GroupCode,
    DateTime CreatedAt);

public record GetProjectsQueryResult(
    IReadOnlyList<ProjectListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
