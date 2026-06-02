namespace TEDF.API.Endpoints.StudentGroups.Requests;

public record CreateGroupRequest(string? Name);
public record InviteMemberRequest(Guid GroupId, string StudentCode, string? Message);
public record JoinGroupRequest(Guid GroupId, string? Message);
public record AcceptInvitationRequest(Guid GroupId, int InvitationId);
public record RejectInvitationRequest(Guid GroupId, int InvitationId);

public record ApproveJoinRequestRequest(Guid GroupId, int RequestId);
public record RejectJoinRequestRequest(Guid GroupId, int RequestId);
