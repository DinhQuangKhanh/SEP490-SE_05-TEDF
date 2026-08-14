using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Endpoints.Evaluations.Requests;
using TEDF.Application.Common;
using TEDF.Application.Features.Evaluations.Commands.AssignEvaluator;
using TEDF.Application.Features.Evaluations.Commands.SubmitEvaluation;
using TEDF.Application.Features.Evaluations.Commands.SubmitFinalDecision;
using TEDF.Application.Features.Evaluations.DTOs;
using TEDF.Application.Features.Evaluations.Queries.CheckTitleSimilarity;
using TEDF.Application.Features.Evaluations.Queries.GetDepartmentEvaluators;
using TEDF.Application.Features.Evaluations.Queries.GetEvaluatorFilterOptions;
using TEDF.Application.Features.Evaluations.Queries.GetEvaluatorHistory;
using TEDF.Application.Features.Evaluations.Queries.GetEvaluatorProjects;
using TEDF.Application.Features.Evaluations.Queries.GetProjectForReview;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Evaluations;

public sealed class EvaluationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Mixed audience: evaluator self-service + department-head evaluation management.
        // Policy is applied per route rather than on the group. (Dashboard lives in Dashboard/.)
        var group = app.MapGroup("/api/evaluations").RequireAuthorization();

        // ── Evaluator self-service ────────────────────────────────────────────
        group.MapGet("/filter-options", GetEvaluatorFilterOptions).RequireAuthorization(PolicyNames.RequireEvaluator).WithTags("Evaluations").WithName("GetEvaluatorFilterOptions").Produces<ApiResponse<EvaluatorFilterOptionsDto>>().Produces(401);
        group.MapGet("/history", GetEvaluatorHistory).RequireAuthorization(PolicyNames.RequireEvaluator).WithTags("Evaluations").WithName("GetEvaluatorHistory").Produces<ApiResponse<EvaluatorHistoryDto>>().Produces(401);
        group.MapGet("/projects", GetEvaluatorProjects).RequireAuthorization(PolicyNames.RequireEvaluator).WithTags("Evaluations").WithName("GetEvaluatorProjects").Produces<ApiResponse<EvaluatorProjectsDto>>().Produces(401);
        group.MapGet("/projects/{projectId:guid}/review", GetProjectForReview).RequireAuthorization(PolicyNames.RequireEvaluator).WithTags("Evaluations").WithName("GetProjectForReview").Produces<ApiResponse<ProjectReviewDetailDto>>().Produces(401).Produces(404);
        group.MapPost("/projects/{projectId:guid}/similarity", CheckTitleSimilarity).RequireAuthorization(PolicyNames.RequireEvaluator).WithTags("Evaluations").WithName("CheckTitleSimilarity").Produces<ApiResponse<List<SimilarityMatchDto>>>().Produces(400).Produces(404);
        group.MapPost("/projects/{projectId:guid}/evaluate", SubmitEvaluation).RequireAuthorization(PolicyNames.RequireEvaluator).WithTags("Evaluations").WithName("SubmitEvaluation").Produces<ApiResponse<string>>().Produces(400).Produces(401);

        // ── Department-head evaluation management (moved from the DepartmentHead role folder) ──
        group.MapGet("/evaluators", GetDepartmentEvaluators).RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment).WithTags("Evaluations").WithName("GetDepartmentEvaluators").Produces(200).Produces(401).Produces(403);
        group.MapPost("/assign-evaluator", AssignEvaluator).RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment).WithTags("Evaluations").WithName("AssignEvaluator").Produces(204).Produces(400).Produces(401).Produces(403).Produces(404);
        group.MapPost("/projects/{projectId:guid}/final-decision", SubmitFinalDecision).RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment).WithTags("Evaluations").WithName("SubmitFinalDecision").Produces(204).Produces(400).Produces(401).Produces(403);
    }

    private static async Task<IResult> GetEvaluatorFilterOptions(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetEvaluatorFilterOptionsQuery(), ct));

    private static async Task<IResult> GetEvaluatorHistory(ISender sender, int page = 1, int pageSize = 10, string? search = null, string? result = null, string? dateRange = null, CancellationToken ct = default)
        => Ok(await sender.Send(new GetEvaluatorHistoryQuery(page, pageSize, search, result, dateRange), ct));

    private static async Task<IResult> GetEvaluatorProjects(ISender sender, int page = 1, int pageSize = 10, string? search = null, int? semesterId = null, int? majorId = null, string? result = null, CancellationToken ct = default)
        => Ok(await sender.Send(new GetEvaluatorProjectsQuery(page, pageSize, search, semesterId, majorId, result), ct));

    private static async Task<IResult> GetProjectForReview(Guid projectId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetProjectForReviewQuery(projectId), ct));

    private static async Task<IResult> CheckTitleSimilarity(Guid projectId, [FromBody] CheckSimilarityRequest body, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new CheckTitleSimilarityQuery(
            projectId, body.Title, body.Description, body.Scope, body.Objectives, body.ExpectedResult,
            body.Technologies ?? []), ct));

    private static async Task<IResult> SubmitEvaluation(Guid projectId, [FromBody] SubmitEvaluationRequest body, ISender sender, CancellationToken ct)
    {
        await sender.Send(new SubmitEvaluationCommand(projectId, body.Result, body.Feedback), ct);
        return Ok("Thẩm định đã được gửi thành công.");
    }

    private static async Task<IResult> GetDepartmentEvaluators(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetDepartmentEvaluatorsQuery(), ct));

    private static async Task<IResult> AssignEvaluator([FromBody] AssignEvaluatorRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new AssignEvaluatorCommand(request.ProjectId, request.PhaseId, request.EvaluatorId, request.EvaluatorOrder), ct);
        return NoContent("Gán người thẩm định thành công.");
    }

    private static async Task<IResult> SubmitFinalDecision(Guid projectId, [FromBody] SubmitFinalDecisionRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new SubmitFinalDecisionCommand(projectId, request.Result, request.Notes), ct);
        return NoContent("Quyết định cuối cùng đã được gửi thành công.");
    }
}
