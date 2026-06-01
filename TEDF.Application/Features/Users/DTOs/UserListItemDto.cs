namespace TEDF.Application.Features.Users.DTOs;

public record UserListItemDto(
    Guid Id,
    string FullName,
    string Email,
    string? AvatarUrl,
    string? StudentCode,
    string? EmployeeCode,
    string? AcademicTitle,
    int? DepartmentId,
    string? DepartmentName,
    string Status,
    List<string> Roles,
    DateTime CreatedAt);

public record GetUsersQueryResult(
    IReadOnlyList<UserListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
