using MediatR;
using TEDF.Application.Features.Archives.Queries.GetProjectArchives;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admins.Archives;

public class GetProjectArchivesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/archives", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetProjectArchivesQuery(), ct);
                return Ok(result);
            })
            .RequireAuthorization(PolicyNames.RequireAdmin)
            .WithTags("Archives")
            .WithName("GetProjectArchives")
            .Produces(200)
            .Produces(401)
            .Produces(403);
    }
}
