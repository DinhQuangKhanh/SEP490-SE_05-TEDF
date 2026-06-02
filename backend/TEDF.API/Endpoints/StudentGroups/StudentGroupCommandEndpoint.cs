using MediatR;
using TEDF.API.Endpoints.StudentGroups.Requests;
using TEDF.Application.Features.StudentGroups.Commands.CreateGroup;
using TEDF.Application.Features.StudentGroups.Commands.InviteMember;
using TEDF.Application.Features.StudentGroups.Commands.RequestJoin;
using TEDF.Application.Features.StudentGroups.Commands.RespondInvitation;
using TEDF.Application.Features.StudentGroups.Commands.RespondJoinRequest;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.StudentGroups;

public partial class StudentGroupEndpoints : IEndpoint
{
    private static void MapCommandEndpoints(RouteGroupBuilder group)
    {
        // ─────────────────────────────────────────────────────────────
        // Commands: các endpoint làm thay đổi dữ liệu/state
        // ─────────────────────────────────────────────────────────────

        #region Tạo nhóm mới cho sinh viên

        // POST /api/student-groups
        // Sinh viên hiện tại tạo một nhóm mới và trở thành leader.
        group.MapPost("", CreateGroup)
            .WithName("CreateGroup")
            .WithTags("StudentGroups")
            .Produces<object>(201)
            .Produces(401);

        #endregion

        #region Mời sinh viên vào nhóm (chỉ leader)

        // POST /api/student-groups/{groupId}/invitations
        // Group leader gửi lời mời cho một sinh viên khác.
        group.MapPost("{groupId:guid}/invitations", InviteMemberToGroup)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("InviteMember")
            .WithTags("StudentGroups")
            .Produces<object>(201)
            .Produces(401);

        #endregion

        #region Phản hồi lời mời vào nhóm

        // PUT /api/student-groups/{groupId}/invitations/{invitationId}/accept
        // Chấp nhận lời mời vào nhóm.
        group.MapPut("{groupId:guid}/invitations/{invitationId:int}/accept", AcceptInvitation)
            .WithName("AcceptInvitation")
            .WithTags("StudentGroups")
            .Produces(204)
            .Produces(401);

        // PUT /api/student-groups/{groupId}/invitations/{invitationId}/reject
        // Từ chối lời mời vào nhóm.
        group.MapPut("{groupId:guid}/invitations/{invitationId:int}/reject", RejectInvitation)
            .WithName("RejectInvitation")
            .WithTags("StudentGroups")
            .Produces(204)
            .Produces(401);

        #endregion

        #region Gửi yêu cầu tham gia nhóm

        // POST /api/student-groups/{groupId}/join-requests
        // Sinh viên gửi request xin vào nhóm.
        group.MapPost("{groupId:guid}/join-requests", RequestJoinGroup)
            .WithName("RequestToJoinGroup")
            .WithTags("StudentGroups")
            .Produces<object>(201)
            .Produces(401);

        #endregion

        #region Xử lý yêu cầu tham gia nhóm (chỉ leader)

        // PUT /api/student-groups/{groupId}/join-requests/{requestId}/approve
        // Leader chấp nhận yêu cầu tham gia.
        group.MapPut("{groupId:guid}/join-requests/{requestId:int}/approve", ApproveJoinRequest)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("ApproveJoinRequest")
            .WithTags("StudentGroups")
            .Produces(204)
            .Produces(401);

        // PUT /api/student-groups/{groupId}/join-requests/{requestId}/reject
        // Leader từ chối yêu cầu tham gia.
        group.MapPut("{groupId:guid}/join-requests/{requestId:int}/reject", RejectJoinRequest)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("RejectJoinRequest")
            .WithTags("StudentGroups")
            .Produces(204)
            .Produces(401);

        #endregion
    }

    #region Handler: tạo nhóm mới

    private static async Task<IResult> CreateGroup(CreateGroupRequest request, ISender sender, CancellationToken ct)
    {
        var groupId = await sender.Send(new CreateGroupCommand(request.Name), ct);
        return Created($"/api/student-groups/{groupId}", new { id = groupId });
    }

    #endregion

    #region Handler: mời sinh viên vào nhóm

    private static async Task<IResult> InviteMemberToGroup(InviteMemberRequest request, ISender sender, CancellationToken ct)
    {
        var invitationId = await sender.Send(
            new InviteMemberCommand(request.GroupId, request.StudentCode, request.Message), ct);

        return Created($"/api/student-groups/{request.GroupId}/invitations/{invitationId}",
            new { id = invitationId });
    }

    #endregion

    #region Handler: chấp nhận lời mời vào nhóm

    private static async Task<IResult> AcceptInvitation(AcceptInvitationRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RespondInvitationCommand(request.GroupId, request.InvitationId, Accept: true), ct);
        return NoContent("Chấp nhận lời mời thành công.");
    }

    #endregion

    #region Handler: từ chối lời mời vào nhóm

    private static async Task<IResult> RejectInvitation(RejectInvitationRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RespondInvitationCommand(request.GroupId, request.InvitationId, Accept: false), ct);
        return NoContent("Từ chối lời mời thành công.");
    }

    #endregion

    #region Handler: gửi yêu cầu tham gia nhóm

    private static async Task<IResult> RequestJoinGroup(JoinGroupRequest request, ISender sender, CancellationToken ct)
    {
        var requestId = await sender.Send(new RequestJoinCommand(request.GroupId, request.Message), ct);
        return Created($"/api/student-groups/{request.GroupId}/join-requests/{requestId}",
            new { id = requestId });
    }

    #endregion

    #region Handler: chấp nhận yêu cầu tham gia nhóm

    private static async Task<IResult> ApproveJoinRequest(ApproveJoinRequestRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RespondJoinRequestCommand(request.GroupId, request.RequestId, Approve: true), ct);
        return NoContent("Chấp nhận yêu cầu tham gia thành công.");
    }

    #endregion

    #region Handler: từ chối yêu cầu tham gia nhóm

    private static async Task<IResult> RejectJoinRequest(RejectJoinRequestRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RespondJoinRequestCommand(request.GroupId, request.RequestId, Approve: false), ct);
        return NoContent("Từ chối yêu cầu tham gia thành công.");
    }

    #endregion
}