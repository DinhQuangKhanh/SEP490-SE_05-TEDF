using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Common;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Dashboard.Queries.GetMentorDashboard;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Mentor;

public class GetMentorDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/mentor/dashboard", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetMentorDashboardQuery(), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization(PolicyNames.RequireMentor)
            .WithTags("Mentor")
            .WithName("GetMentorDashboard")
            .Produces<ApiResponse<MentorDashboardDto>>()
            .Produces(401);
    }
}
