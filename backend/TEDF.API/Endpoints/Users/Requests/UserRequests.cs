namespace TEDF.API.Endpoints.Users.Requests;

/// <summary>Body for assigning a user as head of a department
/// (POST /api/users/departments/{departmentId}/head).</summary>
public sealed record AssignDepartmentHeadRequest(Guid UserId);
