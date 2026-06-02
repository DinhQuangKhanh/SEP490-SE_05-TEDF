using MediatR;
using TEDF.Application.Common;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Dashboard.Queries.GetDepartmentHeadDashboard;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DepartmentHeads;

public class GetDepartmentHeadDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/department-head/dashboard", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetDepartmentHeadDashboardQuery(), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization()
            .WithTags("DepartmentHead")
            .WithName("GetDepartmentHeadDashboard")
            .Produces<ApiResponse<DepartmentHeadDashboardDto>>()
            .Produces(401);
    }
}
