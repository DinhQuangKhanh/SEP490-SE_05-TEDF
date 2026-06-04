using MediatR;
using TEDF.Application.Common;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Dashboard.Queries.GetAdminDashboard;
using TEDF.Application.Features.Dashboard.Queries.GetDepartmentHeadDashboard;
using TEDF.Application.Features.Dashboard.Queries.GetMentorDashboard;
using TEDF.Application.Features.Evaluations.DTOs;
using TEDF.Application.Features.Evaluations.Queries.GetEvaluatorDashboard;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Dashboard;

/// <summary>Per-role dashboards, unified under the `/api/dashboard` feature group.</summary>
public sealed class DashboardEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").RequireAuthorization();

        group.MapGet("/admin", GetAdminDashboard)
            .RequireAuthorization(PolicyNames.RequireAdmin)
            .WithTags("Dashboard").WithName("GetAdminDashboard")
            .Produces<ApiResponse<AdminDashboardDto>>().Produces(401);

        group.MapGet("/mentor", GetMentorDashboard)
            .RequireAuthorization(PolicyNames.RequireMentor)
            .WithTags("Dashboard").WithName("GetMentorDashboard")
            .Produces<ApiResponse<MentorDashboardDto>>().Produces(401);

        group.MapGet("/department-head", GetDepartmentHeadDashboard)
            .WithTags("Dashboard").WithName("GetDepartmentHeadDashboard")
            .Produces<ApiResponse<DepartmentHeadDashboardDto>>().Produces(401);

        group.MapGet("/evaluator", GetEvaluatorDashboard)
            .RequireAuthorization(PolicyNames.RequireEvaluator)
            .WithTags("Dashboard").WithName("GetEvaluatorDashboard")
            .Produces<ApiResponse<EvaluatorDashboardDto>>().Produces(401);
    }

    private static async Task<IResult> GetAdminDashboard(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetAdminDashboardQuery(), ct));

    private static async Task<IResult> GetMentorDashboard(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetMentorDashboardQuery(), ct));

    private static async Task<IResult> GetDepartmentHeadDashboard(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetDepartmentHeadDashboardQuery(), ct));

    private static async Task<IResult> GetEvaluatorDashboard(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetEvaluatorDashboardQuery(), ct));
}
