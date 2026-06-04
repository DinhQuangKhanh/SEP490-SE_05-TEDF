using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Endpoints.DepartmentHead.Requests;
using TEDF.Application.Common;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Dashboard.Queries.GetDepartmentHeadDashboard;
using TEDF.Application.Features.Departments.Commands.AssignEvaluator;
using TEDF.Application.Features.Departments.Commands.SubmitFinalDecision;
using TEDF.Application.Features.Departments.Queries.GetDepartmentEvaluators;
using TEDF.Application.Features.Departments.Queries.GetDepartmentProjects;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DepartmentHead;

public sealed class DepartmentHeadEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/department-head").RequireAuthorization();

        group.MapGet("/dashboard", GetDashboard).WithTags("DepartmentHead").WithName("GetDepartmentHeadDashboard").Produces<ApiResponse<DepartmentHeadDashboardDto>>().Produces(401);
        group.MapGet("/evaluators", GetDepartmentEvaluators).RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment).WithTags("DepartmentHead").WithName("GetDepartmentEvaluators");
        group.MapGet("/projects", GetDepartmentProjects).RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment).WithTags("DepartmentHead").WithName("GetDepartmentProjects");
        group.MapPost("/assign-evaluator", AssignEvaluator).RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment).WithTags("DepartmentHead").WithName("AssignEvaluator").Produces(204).Produces(400).Produces(401).Produces(403).Produces(404);
        group.MapPost("/projects/{projectId:guid}/final-decision", SubmitFinalDecision).RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment).WithTags("DepartmentHead").WithName("SubmitFinalDecision").Produces(204).Produces(400).Produces(401).Produces(403);
    }

    private static async Task<IResult> GetDashboard(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetDepartmentHeadDashboardQuery(), ct));

    private static async Task<IResult> GetDepartmentEvaluators(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetDepartmentEvaluatorsQuery(), ct));

    private static async Task<IResult> GetDepartmentProjects(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetDepartmentProjectsQuery(), ct));

    private static async Task<IResult> AssignEvaluator([FromBody] AssignEvaluatorRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new AssignEvaluatorCommand(request.ProjectId, request.EvaluatorId, request.EvaluatorOrder), ct);
        return NoContent("Gán người thẩm định thành công.");
    }

    private static async Task<IResult> SubmitFinalDecision(Guid projectId, [FromBody] SubmitFinalDecisionRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new SubmitFinalDecisionCommand(projectId, request.Result, request.Notes), ct);
        return NoContent("Quyết định cuối cùng đã được gửi thành công.");
    }
}
