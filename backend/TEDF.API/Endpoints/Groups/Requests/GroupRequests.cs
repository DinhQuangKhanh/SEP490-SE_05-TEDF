namespace TEDF.API.Endpoints.Groups.Requests;

public record CreateGroupRequest(string? Name);
public record InviteMemberRequest(string StudentCode, string? Message);
public record JoinGroupRequest(string? Message);
public record BulkRespondJoinRequestsRequest(List<int> RequestIds);
