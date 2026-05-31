using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Common;
using TEDF.Application.Features.Evaluations.DTOs;
using TEDF.Application.Features.Evaluations.Queries.GetEvaluatorFilterOptions;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Evaluations;

public class GetEvaluatorFilterOptionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/evaluator/filter-options", async (
                ISender sender,
                ILogger<GetEvaluatorFilterOptionsEndpoint> logger,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await sender.Send(
                        new GetEvaluatorFilterOptionsQuery(), cancellationToken);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lỗi khi tải dữ liệu bộ lọc (Học kỳ, Chuyên ngành)");
                    return Results.Json(
                        ApiResponse.Fail("Không thể tải dữ liệu bộ lọc (Học kỳ, Chuyên ngành). Vui lòng thử lại sau."),
                        statusCode: 500);
                }
            })
            .RequireAuthorization(PolicyNames.RequireEvaluator)
            .WithTags("Evaluator")
            .WithName("GetEvaluatorFilterOptions")
            .Produces<ApiResponse<EvaluatorFilterOptionsDto>>()
            .Produces(401);
    }
}
