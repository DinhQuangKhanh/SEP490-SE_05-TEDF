using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.DirectRegistration.Queries.GetAvailableMentors;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DirectRegistration;

public class GetAvailableMentorsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/student/available-mentors", async (
                int? majorId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAvailableMentorsQuery(majorId), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization()
            .WithTags("DirectRegistration")
            .WithName("GetAvailableMentors")
            .Produces<List<AvailableMentorDto>>()
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
