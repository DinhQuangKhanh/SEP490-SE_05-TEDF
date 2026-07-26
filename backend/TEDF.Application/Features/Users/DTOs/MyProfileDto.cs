namespace TEDF.Application.Features.Users.DTOs;

/// <summary>
/// DTO returned by the GET /api/users/me endpoint.
/// Contains full profile information for the currently authenticated user.
/// </summary>
public record MyProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string? AvatarUrl,
    string? StudentCode,
    string? EmployeeCode,
    string? PhoneNumber,
    DateOnly? BirthDate,
    string? PrivacySettings,
    string? AcademicTitle,
    int? DepartmentId,
    string? DepartmentName,
    int? ProgramId,
    string? ProgramCode,
    string? ProgramName,
    int? ComboId,
    string? ComboName,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt);
