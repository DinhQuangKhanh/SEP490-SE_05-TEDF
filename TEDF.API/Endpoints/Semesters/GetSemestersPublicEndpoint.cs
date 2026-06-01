using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.Semesters.Queries.GetAllSemesters;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Semesters;

public class GetSemestersPublicEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/semesters", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetAllSemestersQuery(null),
                    cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization()
            .WithTags("Semesters")
            .WithName("GetSemestersPublic")
            .Produces(200)
            .Produces(401);
    }
}
