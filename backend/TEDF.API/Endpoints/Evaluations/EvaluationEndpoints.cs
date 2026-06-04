using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Endpoints.Evaluations.Requests;
using TEDF.Application.Common;
using TEDF.Application.Features.Evaluations.Commands.SubmitEvaluation;
using TEDF.Application.Features.Evaluations.DTOs;
using TEDF.Application.Features.Evaluations.Queries.CheckTitleSimilarity;
using TEDF.Application.Features.Evaluations.Queries.GetEvaluatorDashboard;
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
        var evaluatorGroup = app.MapGroup("/api/evaluations").RequireAuthorization(PolicyNames.RequireEvaluator);

        evaluatorGroup.MapGet("/dashboard", GetEvaluatorDashboard).WithTags("Evaluations").WithName("GetEvaluatorDashboard").Produces<ApiResponse<EvaluatorDashboardDto>>().Produces(401);
        evaluatorGroup.MapGet("/filter-options", GetEvaluatorFilterOptions).WithTags("Evaluations").WithName("GetEvaluatorFilterOptions").Produces<ApiResponse<EvaluatorFilterOptionsDto>>().Produces(401);
        evaluatorGroup.MapGet("/history", GetEvaluatorHistory).WithTags("Evaluations").WithName("GetEvaluatorHistory").Produces<ApiResponse<EvaluatorHistoryDto>>().Produces(401);
        evaluatorGroup.MapGet("/projects", GetEvaluatorProjects).WithTags("Evaluations").WithName("GetEvaluatorProjects").Produces<ApiResponse<EvaluatorProjectsDto>>().Produces(401);
        evaluatorGroup.MapGet("/projects/{projectId:guid}/review", GetProjectForReview).WithTags("Evaluations").WithName("GetProjectForReview").Produces<ApiResponse<ProjectReviewDetailDto>>().Produces(401).Produces(404);
        evaluatorGroup.MapGet("/projects/{projectId:guid}/similarity", CheckTitleSimilarity).WithTags("Evaluations").WithName("CheckTitleSimilarity").Produces<ApiResponse<List<SimilarTitleDto>>>().Produces(404);
        evaluatorGroup.MapPost("/projects/{projectId:guid}/evaluate", SubmitEvaluation).WithTags("Evaluations").WithName("SubmitEvaluation").Produces<ApiResponse<string>>().Produces(400).Produces(401);
    }

    private static async Task<IResult> GetEvaluatorDashboard(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetEvaluatorDashboardQuery(), ct));

    private static async Task<IResult> GetEvaluatorFilterOptions(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetEvaluatorFilterOptionsQuery(), ct));

    private static async Task<IResult> GetEvaluatorHistory(ISender sender, int page = 1, int pageSize = 10, string? search = null, string? result = null, string? dateRange = null, CancellationToken ct = default)
        => Ok(await sender.Send(new GetEvaluatorHistoryQuery(page, pageSize, search, result, dateRange), ct));

    private static async Task<IResult> GetEvaluatorProjects(ISender sender, int page = 1, int pageSize = 10, string? search = null, int? semesterId = null, int? majorId = null, string? result = null, CancellationToken ct = default)
        => Ok(await sender.Send(new GetEvaluatorProjectsQuery(page, pageSize, search, semesterId, majorId, result), ct));

    private static async Task<IResult> GetProjectForReview(Guid projectId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetProjectForReviewQuery(projectId), ct));

    private static async Task<IResult> CheckTitleSimilarity(Guid projectId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new CheckTitleSimilarityQuery(projectId), ct));

    private static async Task<IResult> SubmitEvaluation(Guid projectId, [FromBody] SubmitEvaluationRequest body, ISender sender, CancellationToken ct)
    {
        await sender.Send(new SubmitEvaluationCommand(projectId, body.Result, body.Feedback), ct);
        return Ok("Thẩm định đã được gửi thành công.");
    }
}
