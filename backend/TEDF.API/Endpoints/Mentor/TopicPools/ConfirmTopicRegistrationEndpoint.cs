using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.TopicPools.Commands.ConfirmRegistration;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Mentor.TopicPools;

/// <summary>
/// Endpoint: PUT /api/topic-pools/registrations/{id}/confirm
/// Confirms a topic registration request, assigning the group to the topic.
/// </summary>
public class ConfirmTopicRegistrationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/topic-pools/registrations/{id:guid}/confirm", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new ConfirmTopicRegistrationCommand(id), cancellationToken);
                return NoContent("Xác nhận thành công.");
            })
            .RequireAuthorization()
            .WithTags("TopicPools")
            .WithName("ConfirmTopicRegistration")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }
}
