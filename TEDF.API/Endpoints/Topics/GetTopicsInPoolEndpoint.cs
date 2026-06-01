using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.Topics.DTOs;
using TEDF.Application.Features.Topics.Queries.GetTopicsInPool;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Topics;

public class GetTopicsInPoolEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/topics", async (
                ISender sender,
                int? majorId = null,
                string? search = null,
                int? poolStatus = null,
                string? sortBy = null,
                int page = 1,
                int pageSize = 12,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.Send(
                    new GetTopicsInPoolQuery(majorId, search, poolStatus, sortBy, page, pageSize),
                    cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization()
            .WithTags("Topics")
            .WithName("GetTopicsInPool")
            .Produces<GetTopicsInPoolResult>()
            .Produces(401);
    }
}
