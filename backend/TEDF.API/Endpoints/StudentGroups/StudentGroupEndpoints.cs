using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.Application.Features.StudentGroups.Commands.CreateGroup;
using TEDF.Application.Features.StudentGroups.Commands.InviteMember;
using TEDF.Application.Features.StudentGroups.Commands.RequestJoin;
using TEDF.Application.Features.StudentGroups.Commands.RespondInvitation;
using TEDF.Application.Features.StudentGroups.Commands.RespondJoinRequest;
using TEDF.Application.Features.StudentGroups.DTOs;
using TEDF.Application.Features.StudentGroups.Queries.GetGroupJoinRequests;
using TEDF.Application.Features.StudentGroups.Queries.GetMyInvitations;
using TEDF.Application.Features.StudentGroups.Queries.GetMyPendingJoinRequest;
using TEDF.Application.Features.StudentGroups.Queries.GetOpenGroups;
using TEDF.Application.Features.StudentGroups.Queries.GetStudentGroup;
using TEDF.API.Endpoints.StudentGroups.Requests;
using TEDF.API.Extensions;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.StudentGroups;

public class StudentGroupEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/student-groups").RequireAuthorization();

        // ── Queries ─────────────────────────────────────────────────

        // GET /api/student-groups/my-group - Get current student's group
        group.MapGet("my-group", async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStudentGroupQuery(), ct);
                return Ok(result);
            })
            .WithName("GetStudentGroup")
            .WithTags("StudentGroups")
            .Produces<StudentGroupDto>()
            .Produces(401);

        // GET /api/student-groups/open - Browse open groups for joining
        group.MapGet("open", async (
                [FromQuery] int? semesterId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetOpenGroupsQuery(semesterId), ct);
                return Ok(result);
            })
            .WithName("GetOpenGroups")
            .WithTags("StudentGroups")
            .Produces<List<OpenGroupDto>>()
            .Produces(401);

        // GET /api/student-groups/my-invitations - Get pending invitations for current student
        group.MapGet("my-invitations", async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyInvitationsQuery(), ct);
                return Ok(result);
            })
            .WithName("GetMyInvitations")
            .WithTags("StudentGroups")
            .Produces<List<InvitationDto>>()
            .Produces(401);

        // GET /api/student-groups/my-pending-join-request - Get current student's pending join request in active semester
        group.MapGet("my-pending-join-request", async (
                [FromQuery] int? semesterId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyPendingJoinRequestQuery(semesterId), ct);
                return Ok(result);
            })
            .WithName("GetMyPendingJoinRequest")
            .WithTags("StudentGroups")
            .Produces<PendingJoinRequestDto>()
            .Produces(401);

        // GET /api/student-groups/{groupId}/join-requests - Get join requests for a group (leader only)
        group.MapGet("{groupId:guid}/join-requests", async (
                Guid groupId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetGroupJoinRequestsQuery(groupId), ct);
                return Ok(result);
            })
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("GetGroupJoinRequests")
            .WithTags("StudentGroups")
            .Produces<List<JoinRequestDto>>()
            .Produces(401);

        // ── Commands ────────────────────────────────────────────────

        // POST /api/student-groups - Create a new group (student becomes leader)
        group.MapPost("", async (
                [FromBody] CreateGroupRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var groupId = await sender.Send(new CreateGroupCommand(request.Name), ct);
                return Created($"/api/student-groups/{groupId}", new { id = groupId });
            })
            .WithName("CreateGroup")
            .WithTags("StudentGroups")
            .Produces<object>(201)
            .Produces(401);

        // POST /api/student-groups/{groupId}/invitations - Invite a student (leader only)
        group.MapPost("{groupId:guid}/invitations", async (
                Guid groupId,
                [FromBody] InviteMemberRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var invitationId = await sender.Send(
                    new InviteMemberCommand(groupId, request.StudentCode, request.Message), ct);
                return Created($"/api/student-groups/{groupId}/invitations/{invitationId}",
                    new { id = invitationId });
            })
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("InviteMember")
            .WithTags("StudentGroups")
            .Produces<object>(201)
            .Produces(401);

        // PUT /api/student-groups/{groupId}/invitations/{invitationId}/accept
        group.MapPut("{groupId:guid}/invitations/{invitationId:int}/accept", async (
                Guid groupId,
                int invitationId,
                ISender sender,
                CancellationToken ct) =>
            {
                await sender.Send(new RespondInvitationCommand(groupId, invitationId, Accept: true), ct);
                return NoContent("Chấp nhận lời mời thành công.");
            })
            .WithName("AcceptInvitation")
            .WithTags("StudentGroups")
            .Produces(204)
            .Produces(401);

        // PUT /api/student-groups/{groupId}/invitations/{invitationId}/reject
        group.MapPut("{groupId:guid}/invitations/{invitationId:int}/reject", async (
                Guid groupId,
                int invitationId,
                ISender sender,
                CancellationToken ct) =>
            {
                await sender.Send(new RespondInvitationCommand(groupId, invitationId, Accept: false), ct);
                return NoContent("Từ chối lời mời thành công.");
            })
            .WithName("RejectInvitation")
            .WithTags("StudentGroups")
            .Produces(204)
            .Produces(401);

        // POST /api/student-groups/{groupId}/join-requests - Request to join a group
        group.MapPost("{groupId:guid}/join-requests", async (
                Guid groupId,
                [FromBody] JoinGroupRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var requestId = await sender.Send(new RequestJoinCommand(groupId, request.Message), ct);
                return Created($"/api/student-groups/{groupId}/join-requests/{requestId}",
                    new { id = requestId });
            })
            .WithName("RequestToJoinGroup")
            .WithTags("StudentGroups")
            .Produces<object>(201)
            .Produces(401);

        // PUT /api/student-groups/{groupId}/join-requests/{requestId}/approve
        group.MapPut("{groupId:guid}/join-requests/{requestId:int}/approve", async (
                Guid groupId,
                int requestId,
                ISender sender,
                CancellationToken ct) =>
            {
                await sender.Send(new RespondJoinRequestCommand(groupId, requestId, Approve: true), ct);
                return NoContent("Chấp nhận yêu cầu tham gia thành công.");
            })
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("ApproveJoinRequest")
            .WithTags("StudentGroups")
            .Produces(204)
            .Produces(401);

        // PUT /api/student-groups/{groupId}/join-requests/{requestId}/reject
        group.MapPut("{groupId:guid}/join-requests/{requestId:int}/reject", async (
                Guid groupId,
                int requestId,
                ISender sender,
                CancellationToken ct) =>
            {
                await sender.Send(new RespondJoinRequestCommand(groupId, requestId, Approve: false), ct);
                return NoContent("Từ chối yêu cầu tham gia thành công.");
            })
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("RejectJoinRequest")
            .WithTags("StudentGroups")
            .Produces(204)
            .Produces(401);
    }
}
