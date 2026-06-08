using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Endpoints.Groups.Requests;
using TEDF.Application.Features.StudentGroups.Commands.CreateGroup;
using TEDF.Application.Features.StudentGroups.Commands.InviteMember;
using TEDF.Application.Features.StudentGroups.Commands.RequestJoin;
using TEDF.Application.Features.StudentGroups.Commands.RespondInvitation;
using TEDF.Application.Features.StudentGroups.Commands.RespondJoinRequest;
using TEDF.Application.Features.StudentGroups.Queries.GetGroupJoinRequests;
using TEDF.Application.Features.StudentGroups.Queries.GetInvitableStudents;
using TEDF.Application.Features.StudentGroups.Queries.GetMentorGroups;
using TEDF.Application.Features.StudentGroups.Queries.GetMyInvitations;
using TEDF.Application.Features.StudentGroups.Queries.GetMyPendingJoinRequest;
using TEDF.Application.Features.StudentGroups.Queries.GetOpenGroups;
using TEDF.Application.Features.StudentGroups.Queries.GetStudentGroup;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Groups;

public sealed class StudentGroupEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/groups").RequireAuthorization();

        group.MapGet("/my-group", GetStudentGroup).WithTags("Groups").WithName("GetStudentGroup").Produces(200).Produces(401);
        group.MapGet("/open", GetOpenGroups).WithTags("Groups").WithName("GetOpenGroups").Produces(200).Produces(401);
        group.MapGet("/my-invitations", GetMyInvitations).WithTags("Groups").WithName("GetMyInvitations").Produces(200).Produces(401);
        group.MapGet("/my-pending-join-request", GetMyPendingJoinRequest).WithTags("Groups").WithName("GetMyPendingJoinRequest").Produces(200).Produces(401);
        group.MapGet("/{groupId:guid}/join-requests", GetJoinRequests).WithTags("Groups").WithName("GetGroupJoinRequests").Produces(200).Produces(401);
        group.MapGet("/{groupId:guid}/invitable-students", GetInvitableStudents).WithTags("Groups").WithName("GetInvitableStudents").Produces(200).Produces(401);

        group.MapPost("", CreateGroup).WithTags("Groups").WithName("CreateGroup").Produces(201).Produces(401);
        group.MapPost("/{groupId:guid}/invitations", InviteMemberToGroup).RequireAuthorization(PolicyNames.GroupLeader).WithTags("Groups").WithName("InviteMember").Produces(201).Produces(401);
        group.MapPost("/{groupId:guid}/join-requests", RequestJoinGroup).WithTags("Groups").WithName("RequestJoinGroup").Produces(201).Produces(401);

        group.MapPut("/{groupId:guid}/invitations/{invitationId:int}/accept", AcceptInvitation).WithTags("Groups").WithName("AcceptInvitation").Produces(204).Produces(401);
        group.MapPut("/{groupId:guid}/invitations/{invitationId:int}/reject", RejectInvitation).WithTags("Groups").WithName("RejectInvitation").Produces(204).Produces(401);
        group.MapPut("/{groupId:guid}/join-requests/{requestId:int}/approve", ApproveJoinRequest).RequireAuthorization(PolicyNames.GroupLeader).WithTags("Groups").WithName("ApproveJoinRequest").Produces(204).Produces(401);
        group.MapPut("/{groupId:guid}/join-requests/{requestId:int}/reject", RejectJoinRequest).RequireAuthorization(PolicyNames.GroupLeader).WithTags("Groups").WithName("RejectJoinRequest").Produces(204).Produces(401);

        group.MapGet("/mentor", GetMentorGroups).RequireAuthorization(PolicyNames.RequireMentor).WithTags("Groups").WithName("GetMentorGroups").Produces(200).Produces(401);


    }

    private static async Task<IResult> GetStudentGroup(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetStudentGroupQuery(), ct));

    private static async Task<IResult> GetOpenGroups([FromQuery] int? semesterId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetOpenGroupsQuery(semesterId), ct));

    private static async Task<IResult> GetMyInvitations(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetMyInvitationsQuery(), ct));

    private static async Task<IResult> GetMyPendingJoinRequest([FromQuery] int? semesterId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetMyPendingJoinRequestQuery(semesterId), ct));

    private static async Task<IResult> GetJoinRequests(Guid groupId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetGroupJoinRequestsQuery(groupId), ct));

    private static async Task<IResult> GetInvitableStudents(Guid groupId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetInvitableStudentsQuery(groupId), ct));

    private static async Task<IResult> CreateGroup(CreateGroupRequest request, ISender sender, CancellationToken ct)
    {
        var groupId = await sender.Send(new CreateGroupCommand(request.Name), ct);
        return Created($"/api/groups/{groupId}", new { id = groupId });
    }

    private static async Task<IResult> InviteMemberToGroup(Guid groupId, InviteMemberRequest request, ISender sender, CancellationToken ct)
    {
        var invitationId = await sender.Send(new InviteMemberCommand(groupId, request.StudentCode, request.Message), ct);
        return Created($"/api/groups/{groupId}/invitations/{invitationId}", new { id = invitationId });
    }

    private static async Task<IResult> AcceptInvitation(Guid groupId, int invitationId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RespondInvitationCommand(groupId, invitationId, Accept: true), ct);
        return NoContent("Chấp nhận lời mời thành công.");
    }

    private static async Task<IResult> RejectInvitation(Guid groupId, int invitationId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RespondInvitationCommand(groupId, invitationId, Accept: false), ct);
        return NoContent("Từ chối lời mời thành công.");
    }

    private static async Task<IResult> RequestJoinGroup(Guid groupId, JoinGroupRequest request, ISender sender, CancellationToken ct)
    {
        var requestId = await sender.Send(new RequestJoinCommand(groupId, request.Message), ct);
        return Created($"/api/groups/{groupId}/join-requests/{requestId}", new { id = requestId });
    }

    private static async Task<IResult> ApproveJoinRequest(Guid groupId, int requestId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RespondJoinRequestCommand(groupId, requestId, Approve: true), ct);
        return NoContent("Chấp nhận yêu cầu tham gia thành công.");
    }

    private static async Task<IResult> RejectJoinRequest(Guid groupId, int requestId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RespondJoinRequestCommand(groupId, requestId, Approve: false), ct);
        return NoContent("Từ chối yêu cầu tham gia thành công.");
    }

    private static async Task<IResult> GetMentorGroups(int? semesterId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetMentorGroupsQuery(semesterId), ct));
}
