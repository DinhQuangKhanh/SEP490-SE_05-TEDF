using MediatR;
using TEDF.Application.Features.TopicPools.DTOs;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPoolById;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Mentor.TopicPools;

public class GetTopicPoolByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/topic-pools/{id:guid}", async (
                ISender sender,
                Guid id,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.Send(new GetTopicPoolByIdQuery(id), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization()
            .WithTags("TopicPools")
            .WithName("GetTopicPoolById")
            .Produces<TopicPoolDto>()
            .Produces(401)
            .Produces(404);
    }
}
