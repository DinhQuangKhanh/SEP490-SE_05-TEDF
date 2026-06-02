namespace TEDF.API.Endpoints.StudentGroups.Requests;

public record CreateGroupRequest(string? Name);
public record InviteMemberRequest(string StudentCode, string? Message);

public record JoinGroupRequest(string? Message);
