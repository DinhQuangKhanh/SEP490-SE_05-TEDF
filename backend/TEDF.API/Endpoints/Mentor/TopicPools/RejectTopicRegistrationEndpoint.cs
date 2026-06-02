using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.TopicPools.Commands.RejectRegistration;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Mentor.TopicPools;

/// <summary>
/// Endpoint: PUT /api/topic-pools/registrations/{id}/reject
/// Rejects a topic registration request (with a mandatory reason).
/// </summary>
public class RejectTopicRegistrationEndpoint : IEndpoint
{
    public sealed record RequestBody(string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/topic-pools/registrations/{id:guid}/reject", async (
                Guid id,
                RequestBody body,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new RejectTopicRegistrationCommand(id, body.Reason), cancellationToken);
                return NoContent("Từ chối yêu cầu thành công.");
            })
            .RequireAuthorization()
            .WithTags("TopicPools")
            .WithName("RejectTopicRegistration")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }
}
