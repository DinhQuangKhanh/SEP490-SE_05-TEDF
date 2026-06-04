using MediatR;
using TEDF.Application.Common;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Dashboard.Queries.GetAdminDashboard;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admin;

public sealed class AdminDashboardEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/dashboard", GetAdminDashboard)
            .RequireAuthorization(PolicyNames.RequireAdmin)
            .WithTags("Admin")
            .WithName("GetAdminDashboard")
            .Produces<ApiResponse<AdminDashboardDto>>()
            .Produces(401);
    }

    private static async Task<IResult> GetAdminDashboard(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetAdminDashboardQuery(), ct));
}
