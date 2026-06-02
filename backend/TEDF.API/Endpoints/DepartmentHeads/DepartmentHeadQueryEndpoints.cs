using MediatR;
using TEDF.Application.Common;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Dashboard.Queries.GetDepartmentHeadDashboard;
using TEDF.Application.Features.Departments.Queries.GetDepartmentEvaluators;
using TEDF.Application.Features.Departments.Queries.GetDepartmentProjects;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DepartmentHeads;

public partial class DepartmentHeadEndpoints : IEndpoint
{
    private static void MapQueryEndpoints(RouteGroupBuilder group)
    {
        // ─────────────────────────────────────────────────────────────
        // Queries: các endpoint chỉ để đọc dữ liệu, không làm thay đổi state
        // ─────────────────────────────────────────────────────────────

        #region Dashboard của chủ nhiệm bộ môn

        // GET /api/department-head/dashboard
        // Trả về số liệu tổng quan cho chủ nhiệm bộ môn (chỉ cần đăng nhập).
        group.MapGet("dashboard", GetDashboard)
            .WithTags("DepartmentHead")
            .WithName("GetDepartmentHeadDashboard")
            .Produces<ApiResponse<DepartmentHeadDashboardDto>>()
            .Produces(401);

        #endregion

        #region Danh sách người thẩm định trong bộ môn

        // GET /api/department-head/evaluators
        // Chỉ chủ nhiệm bộ môn của department tương ứng mới được xem.
        group.MapGet("evaluators", GetDepartmentEvaluators)
            .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags("DepartmentHead")
            .WithName("GetDepartmentEvaluators");

        #endregion

        #region Danh sách đề tài của bộ môn

        // GET /api/department-head/projects
        // Chỉ chủ nhiệm bộ môn của department tương ứng mới được xem.
        group.MapGet("projects", GetDepartmentProjects)
            .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags("DepartmentHead")
            .WithName("GetDepartmentProjects");

        #endregion
    }

    #region Handler: dashboard của chủ nhiệm bộ môn

    private static async Task<IResult> GetDashboard(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetDepartmentHeadDashboardQuery(), ct);
        return Ok(result);
    }

    #endregion

    #region Handler: danh sách người thẩm định trong bộ môn

    private static async Task<IResult> GetDepartmentEvaluators(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetDepartmentEvaluatorsQuery(), ct);
        return Ok(result);
    }

    #endregion

    #region Handler: danh sách đề tài của bộ môn

    private static async Task<IResult> GetDepartmentProjects(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetDepartmentProjectsQuery(), ct);
        return Ok(result);
    }

    #endregion
}
