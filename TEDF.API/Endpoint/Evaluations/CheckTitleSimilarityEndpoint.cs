using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Common;
using TEDF.Application.Features.Evaluations.DTOs;
using TEDF.Application.Features.Evaluations.Queries.CheckTitleSimilarity;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Evaluations;

public class CheckTitleSimilarityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/evaluator/projects/{projectId:guid}/similarity", async (
                Guid projectId,
                ISender sender,
                ILogger<CheckTitleSimilarityEndpoint> logger,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var query = new CheckTitleSimilarityQuery(projectId);
                    var result = await sender.Send(query, cancellationToken);
                    return Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Json(ApiResponse.Fail(ex.Message), statusCode: 404);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lỗi khi kiểm tra trùng lặp tiêu đề");
                    return Results.Json(ApiResponse.Fail("Không thể kiểm tra trùng lặp. Vui lòng thử lại sau."), statusCode: 500);
                }
            })
            .RequireAuthorization(PolicyNames.RequireEvaluator)
            .WithTags("Evaluator")
            .WithName("CheckTitleSimilarity")
            .Produces<ApiResponse<List<SimilarTitleDto>>>()
            .Produces(404);
    }
}
