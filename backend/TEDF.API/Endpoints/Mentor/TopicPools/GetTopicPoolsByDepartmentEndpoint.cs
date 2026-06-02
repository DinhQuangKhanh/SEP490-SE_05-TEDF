using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.TopicPools.DTOs;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPoolsByDepartment;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Mentor.TopicPools;

public class GetTopicPoolsByDepartmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/topic-pools/by-department", async (
                ISender sender,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.Send(new GetTopicPoolsByDepartmentQuery(), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization()
            .WithTags("TopicPools")
            .WithName("GetTopicPoolsByDepartment")
            .Produces<List<DepartmentWithPoolsDto>>()
            .Produces(401);
    }
}
