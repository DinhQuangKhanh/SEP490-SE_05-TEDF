using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.Application.Features.StudentGroups.DTOs;
using TEDF.Application.Features.StudentGroups.Queries.GetGroupJoinRequests;
using TEDF.Application.Features.StudentGroups.Queries.GetInvitableStudents;
using TEDF.Application.Features.StudentGroups.Queries.GetMyInvitations;
using TEDF.Application.Features.StudentGroups.Queries.GetMyPendingJoinRequest;
using TEDF.Application.Features.StudentGroups.Queries.GetOpenGroups;
using TEDF.Application.Features.StudentGroups.Queries.GetStudentGroup;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.StudentGroups;

public partial class StudentGroupEndpoints : IEndpoint
{
    private static void MapQueryEndpoints(RouteGroupBuilder group)
    {
        // ─────────────────────────────────────────────────────────────
        // Queries: các endpoint chỉ để đọc dữ liệu, không làm thay đổi state
        // ─────────────────────────────────────────────────────────────

        #region Lấy thông tin nhóm hiện tại của sinh viên

        // GET /api/student-groups/my-group
        // Trả về nhóm hiện tại mà sinh viên đang tham gia.
        group.MapGet("my-group", GetMyCurrentGroupForStudent)
            .WithName("GetStudentGroup")
            .WithTags("StudentGroups")
            .Produces<StudentGroupDto>()
            .Produces(401);

        #endregion

        #region Lấy danh sách các nhóm đang mở để sinh viên xem và chọn tham gia

        // GET /api/student-groups/open?semesterId=...
        // Nếu semesterId không truyền lên thì backend nhận null.
        group.MapGet("open", GetAllOpenedGroupsForStudent)
            .WithName("GetOpenGroups")
            .WithTags("StudentGroups")
            .Produces<List<OpenGroupDto>>()
            .Produces(401);

        #endregion

        #region Lấy danh sách lời mời tham gia nhóm của sinh viên

        // GET /api/student-groups/my-invitations
        // Trả về toàn bộ invitation đang chờ xử lý của sinh viên hiện tại.
        group.MapGet("my-invitations", GetAllMyPendingJoinInvitations)
            .WithName("GetMyInvitations")
            .WithTags("StudentGroups")
            .Produces<List<InvitationDto>>()
            .Produces(401);

        #endregion

        #region Lấy yêu cầu tham gia nhóm mà sinh viên đã gửi

        // GET /api/student-groups/my-pending-join-request?semesterId=...
        // Trả về request tham gia đang pending của sinh viên trong học kỳ tương ứng.
        group.MapGet("my-pending-join-request", GetMyPendingJoinRequest)
            .WithName("GetMyPendingJoinRequest")
            .WithTags("StudentGroups")
            .Produces<PendingJoinRequestDto>()
            .Produces(401);

        #endregion

        #region Lấy danh sách yêu cầu tham gia của một nhóm (chỉ leader)

        // GET /api/student-groups/{groupId}/join-requests
        // Chỉ group leader mới được xem các request tham gia của nhóm.
        group.MapGet("{groupId:guid}/join-requests", GetJoinRequestsForGroup)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("GetGroupJoinRequests")
            .WithTags("StudentGroups")
            .Produces<List<JoinRequestDto>>()
            .Produces(401);

        #endregion

        #region Lấy danh sách sinh viên có thể mời vào nhóm (chỉ leader)

        // GET /api/student-groups/{groupId}/invitable-students
        // Trả về danh sách sinh viên chưa thuộc nhóm nào để leader có thể mời.
        group.MapGet("{groupId:guid}/invitable-students", GetInvitableStudentsForGroup)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("GetInvitableStudents")
            .WithTags("StudentGroups")
            .Produces<List<AvailableStudentDto>>()
            .Produces(401);

        #endregion
    }

    #region Handler: lấy nhóm hiện tại của sinh viên

    private static async Task<IResult> GetMyCurrentGroupForStudent(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetStudentGroupQuery(), ct);
        return Ok(result);
    }

    #endregion

    #region Handler: lấy danh sách nhóm đang mở theo học kỳ

    private static async Task<IResult> GetAllOpenedGroupsForStudent([FromQuery] int? semesterId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetOpenGroupsQuery(semesterId), ct);
        return Ok(result);
    }

    #endregion

    #region Handler: lấy danh sách lời mời của sinh viên

    private static async Task<IResult> GetAllMyPendingJoinInvitations(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetMyInvitationsQuery(), ct);
        return Ok(result);
    }

    #endregion

    #region Handler: lấy request tham gia đang pending của sinh viên

    private static async Task<IResult> GetMyPendingJoinRequest([FromQuery] int? semesterId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetMyPendingJoinRequestQuery(semesterId), ct);
        return Ok(result);
    }

    #endregion

    #region Handler: lấy request tham gia của một nhóm

    private static async Task<IResult> GetJoinRequestsForGroup(Guid groupId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetGroupJoinRequestsQuery(groupId), ct);
        return Ok(result);
    }

    #endregion

    #region Handler: lấy danh sách sinh viên có thể mời

    private static async Task<IResult> GetInvitableStudentsForGroup(Guid groupId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvitableStudentsQuery(groupId), ct);
        return Ok(result);
    }

    #endregion
}