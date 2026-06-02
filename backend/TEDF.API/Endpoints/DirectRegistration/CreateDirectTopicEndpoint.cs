using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Extensions;
using TEDF.Application.Features.DirectRegistration.Commands.CreateDirectTopic;
using TEDF.API.Endpoints.DirectRegistration.Requests;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DirectRegistration;

public class CreateDirectTopicEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/student/{groupId:guid}/direct-topic", async (
                Guid groupId,
                [FromBody] CreateDirectTopicRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
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
            })
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithTags("DirectRegistration")
            .WithName("CreateDirectTopic")
            .Produces<object>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
