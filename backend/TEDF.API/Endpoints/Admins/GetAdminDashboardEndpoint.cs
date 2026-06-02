using MediatR;
using TEDF.Application.Common;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Dashboard.Queries.GetAdminDashboard;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admins;

public class GetAdminDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/dashboard", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetAdminDashboardQuery(), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Admin")
            .WithName("GetAdminDashboard")
            .Produces<ApiResponse<AdminDashboardDto>>()
            .Produces(401);
    }
}
