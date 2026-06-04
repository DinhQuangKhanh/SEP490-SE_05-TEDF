using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Endpoints.DirectTopics.Requests;
using TEDF.Application.Features.DirectRegistration.Commands.CreateDirectTopic;
using TEDF.Application.Features.DirectRegistration.Commands.SubmitToMentor;
using TEDF.Application.Features.DirectRegistration.Commands.UpdateDirectTopic;
using TEDF.Application.Features.DirectRegistration.Commands.MentorReviewProposedTopic;
using TEDF.Application.Features.DirectRegistration.Queries.GetAvailableMentors;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DirectTopics;

public sealed class DirectTopicEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/direct-topics").RequireAuthorization();

        group.MapGet("/available-mentors", GetAvailableMentors)
            .WithTags("DirectTopics")
            .WithName("GetAvailableMentors")
            .Produces(200).Produces(401);

        group.MapPost("/{groupId:guid}", CreateDirectTopic)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithTags("DirectTopics")
            .WithName("CreateDirectTopic")
            .Produces(201).Produces(400).Produces(401);

        group.MapPut("/{projectId:guid}", UpdateDirectTopic)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithTags("DirectTopics")
            .WithName("UpdateDirectTopic")
            .Produces(204).Produces(400).Produces(401).Produces(404);

        group.MapPut("/{projectId:guid}/submit-to-mentor/{groupId:guid}", SubmitToMentor)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithTags("DirectTopics")
            .WithName("SubmitToMentor")
            .Produces(204).Produces(400).Produces(401);

        group.MapPut("/{projectId:guid}/review", MentorReviewTopic)
            .RequireAuthorization(PolicyNames.MentorOfProject)
            .WithTags("DirectTopics")
            .WithName("MentorReviewProposedTopic")
            .Produces(204).Produces(400).Produces(401);
    }

    private static async Task<IResult> GetAvailableMentors(int? majorId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetAvailableMentorsQuery(majorId), ct));

    private static async Task<IResult> CreateDirectTopic(Guid groupId, [FromBody] CreateDirectTopicRequest request, ISender sender, CancellationToken ct)
    {
        var projectId = await sender.Send(new CreateDirectTopicCommand(
            request.NameVi, request.NameEn, request.NameAbbr, request.Description, request.Objectives,
            request.Scope, request.Technologies, request.ExpectedResults, request.MentorId, groupId, request.MajorId, request.MaxStudents
        ), ct);
        return Created($"/api/direct-topics/{projectId}", new { id = projectId }, "Đề xuất đề tài thành công.");
    }

    private static async Task<IResult> UpdateDirectTopic(Guid projectId, [FromBody] UpdateDirectTopicRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new UpdateDirectTopicCommand(
            projectId, request.NameVi, request.NameEn, request.NameAbbr, request.Description, request.Objectives,
            request.Scope, request.Technologies, request.ExpectedResults, request.MaxStudents
        ), ct);
        return NoContent("Cập nhật đề tài thành công.");
    }

    private static async Task<IResult> SubmitToMentor(Guid projectId, Guid groupId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new SubmitToMentorCommand(projectId, groupId), ct);
        return NoContent("Đã gửi đề tài cho giảng viên hướng dẫn.");
    }

    private static async Task<IResult> MentorReviewTopic(Guid projectId, MentorReviewRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new MentorReviewProposedTopicCommand(projectId, request.Action, request.Feedback), ct);
        return NoContent("Đã xử lý đề tài thành công.");
    }
}
