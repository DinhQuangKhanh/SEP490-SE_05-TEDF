namespace TEDF.API.Endpoints.Students.StudentGroups.Requests;

public record CreateGroupRequest(string? Name);
public record InviteMemberRequest(string StudentCode, string? Message);

public record JoinGroupRequest(string? Message);
