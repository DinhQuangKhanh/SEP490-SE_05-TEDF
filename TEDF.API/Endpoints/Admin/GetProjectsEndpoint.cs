using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.Projects.Queries.GetProjects;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admin;

public class GetProjectsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/projects", async (
                ISender sender,
                string? search,
                int? semesterId,
                string? status,
                int? majorId,
                int page = 1,
                int pageSize = 20,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.Send(
                    new GetProjectsQuery(search, semesterId, status, majorId, page, pageSize), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Admin")
            .WithName("GetProjects")
            .Produces(200)
            .Produces(401);
    }
}
