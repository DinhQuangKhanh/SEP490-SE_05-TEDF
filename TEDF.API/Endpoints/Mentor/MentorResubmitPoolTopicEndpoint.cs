using MediatR;
using TEDF.Application.Features.Mentor.Commands.MentorResubmitPoolTopic;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Mentor;

public class MentorResubmitPoolTopicEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/mentor/topics/{projectId:guid}/resubmit", async (
                Guid projectId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new MentorResubmitPoolTopicCommand(projectId);
                await sender.Send(command, cancellationToken);
                return Ok("Đã gửi đề tài đi thẩm định thành công.");
            })
            .RequireAuthorization(PolicyNames.MentorOfProject)
            .WithTags("Mentor")
            .WithName("MentorResubmitPoolTopic")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }
}
