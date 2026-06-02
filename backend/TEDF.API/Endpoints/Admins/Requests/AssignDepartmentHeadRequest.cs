namespace TEDF.API.Endpoints.Admins.Requests;

/// <summary>
/// Request body for setting department head.
/// </summary>
public sealed record AssignDepartmentHeadRequest(Guid UserId);
