using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Endpoints.Students.DirectRegistration.Requests;
using TEDF.Application.Features.DirectRegistration.Commands.CreateDirectTopic;
using TEDF.Application.Features.DirectRegistration.Commands.SubmitToMentor;
using TEDF.Application.Features.DirectRegistration.Commands.UpdateDirectTopic;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Students.DirectRegistration;

public partial class DirectRegistrationEndpoints : IEndpoint
{
    private static void MapCommandEndpoints(RouteGroupBuilder group)
    {
        // ─────────────────────────────────────────────────────────────
        // Commands: các endpoint làm thay đổi dữ liệu/state
        // ─────────────────────────────────────────────────────────────

        #region Tạo đề tài trực tiếp cho nhóm

        // POST /api/student/{groupId}/direct-topic
        // Group leader tạo đề tài trực tiếp cho nhóm mình.
        group.MapPost("{groupId:guid}/direct-topic", CreateDirectTopic)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("CreateDirectTopic")
            .WithTags("DirectRegistration")
            .Produces<object>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(401);

        #endregion

        #region Cập nhật đề tài trực tiếp

        // PUT /api/student/direct-topic/{projectId}/update
        // Group leader cập nhật thông tin đề tài đã tạo.
        group.MapPut("direct-topic/{projectId:guid}/update", UpdateDirectTopic)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("UpdateDirectTopic")
            .WithTags("DirectRegistration")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(401);

        #endregion

        #region Gửi đề tài cho giảng viên hướng dẫn

        // PUT /api/student/direct-topic/{groupId}/{projectId}/submit-to-mentor
        // Group leader gửi đề tài cho giảng viên hướng dẫn phê duyệt.
        group.MapPut("direct-topic/{groupId:guid}/{projectId:guid}/submit-to-mentor", SubmitToMentor)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithName("SubmitToMentor")
            .WithTags("DirectRegistration")
            .Produces(200)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(401);

        #endregion
    }

    #region Handler: tạo đề tài trực tiếp

    private static async Task<IResult> CreateDirectTopic(
        Guid groupId,
        [FromBody] CreateDirectTopicRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateDirectTopicCommand(
            request.NameVi,
            request.NameEn,
            request.NameAbbr,
            request.Description,
            request.Objectives,
            request.Scope,
            request.Technologies,
            request.ExpectedResults,
            request.MentorId,
            groupId,
            request.MajorId,
            request.MaxStudents
        );

        var projectId = await sender.Send(command, cancellationToken);
        return Created($"/api/student/direct-topic/{projectId}", new { id = projectId }, "Đề xuất đề tài thành công.");
    }

    #endregion

    #region Handler: cập nhật đề tài trực tiếp

    private static async Task<IResult> UpdateDirectTopic(
        Guid projectId,
        [FromBody] UpdateDirectTopicRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDirectTopicCommand(
            projectId,
            request.NameVi,
            request.NameEn,
            request.NameAbbr,
            request.Description,
            request.Objectives,
            request.Scope,
            request.Technologies,
            request.ExpectedResults,
            request.MaxStudents
        );

        await sender.Send(command, cancellationToken);
        return NoContent("Cập nhật đề tài thành công.");
    }

    #endregion

    #region Handler: gửi đề tài cho giảng viên hướng dẫn

    private static async Task<IResult> SubmitToMentor(
        Guid groupId,
        Guid projectId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitToMentorCommand(projectId, groupId), cancellationToken);
        return NoContent("Đã gửi đề tài cho giảng viên hướng dẫn.");
    }

    #endregion
}
