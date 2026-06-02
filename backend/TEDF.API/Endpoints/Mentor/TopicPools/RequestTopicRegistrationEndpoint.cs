using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.TopicPools.Commands.RequestRegistration;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;
using TEDF.API.Endpoints.Mentor.TopicPools.Requests;

namespace TEDF.API.Endpoints.Mentor.TopicPools;

/// <summary>
/// Endpoint: POST /api/student-groups/{groupId}/topic-registrations
/// Allows a group leader to request registration for a topic from the pool.
/// </summary>
public class RequestTopicRegistrationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/student-groups/{groupId:guid}/topic-registrations", async (
                Guid groupId,
                TopicRegistrationRequest body,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new RequestTopicRegistrationCommand(
                    body.ProjectId,
                    groupId,
                    body.Note);

                var registrationId = await sender.Send(command, cancellationToken);
                return Created($"/api/topic-pools/registrations/{registrationId}", new { id = registrationId }, "Tạo mới thành công.");
            })
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithTags("TopicPools")
            .WithName("RequestTopicRegistration")
            .Produces<object>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
