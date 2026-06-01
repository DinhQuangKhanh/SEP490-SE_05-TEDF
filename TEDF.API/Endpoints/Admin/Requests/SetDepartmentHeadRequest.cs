namespace TEDF.API.Endpoints.Admin.Requests;

/// <summary>
/// Request body for setting department head.
/// </summary>
public sealed record SetDepartmentHeadRequest(Guid UserId);
