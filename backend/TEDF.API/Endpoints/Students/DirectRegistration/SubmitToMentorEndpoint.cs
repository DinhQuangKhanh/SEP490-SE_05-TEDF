using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.DirectRegistration.Commands.SubmitToMentor;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Students.DirectRegistration;

public class SubmitToMentorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/student/direct-topic/{groupId:guid}/{projectId:guid}/submit-to-mentor", async (
                Guid groupId,
                Guid projectId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new SubmitToMentorCommand(projectId, groupId), cancellationToken);
                return NoContent("Đã gửi đề tài cho giảng viên hướng dẫn.");
            })
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithTags("DirectRegistration")
            .WithName("SubmitToMentor")
            .Produces(200)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
