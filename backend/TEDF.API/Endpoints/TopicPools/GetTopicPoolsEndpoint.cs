using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.TopicPools.DTOs;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPools;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.TopicPools;

public class GetTopicPoolsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/topic-pools", async (
                ISender sender,
                int? majorId = null,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.Send(new GetTopicPoolsQuery(majorId), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization()
            .WithTags("TopicPools")
            .WithName("GetTopicPools")
            .Produces<List<TopicPoolDto>>()
            .Produces(401);
    }
}
