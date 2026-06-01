using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.Application.Features.Mentor.Commands.MentorUpdatePoolTopic;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Mentor;

public class MentorUpdatePoolTopicEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/mentor/topics/{projectId:guid}/update", async (
                Guid projectId,
                [FromBody] MentorUpdatePoolTopicRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new MentorUpdatePoolTopicCommand(
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
            })
            .RequireAuthorization(PolicyNames.MentorOfProject)
            .WithTags("Mentor")
            .WithName("MentorUpdatePoolTopic")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }
}

public record MentorUpdatePoolTopicRequest(
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    int MaxStudents
);
