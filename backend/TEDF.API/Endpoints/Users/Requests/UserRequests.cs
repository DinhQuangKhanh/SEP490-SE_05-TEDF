namespace TEDF.API.Endpoints.Users.Requests;

/// <summary>Body for assigning a user as head of a department
/// (POST /api/users/departments/{departmentId}/head).</summary>
public sealed record AssignDepartmentHeadRequest(Guid UserId);

/// <summary>Body for creating a single user (POST /api/users).</summary>
public sealed record CreateUserRequest(
    string Role,
    string Email,
    string FullName,
    string Code,
    string? Phone,
    string? AcademicTitle,
    int? MajorId);
