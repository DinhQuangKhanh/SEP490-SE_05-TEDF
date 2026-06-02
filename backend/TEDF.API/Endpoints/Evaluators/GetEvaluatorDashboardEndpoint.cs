using MediatR;
using TEDF.Application.Common;
using TEDF.Application.Features.Evaluations.DTOs;
using TEDF.Application.Features.Evaluations.Queries.GetEvaluatorDashboard;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Evaluators;

public class GetEvaluatorDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/evaluator/dashboard", async (
                ISender sender,
                ILogger<GetEvaluatorDashboardEndpoint> logger,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await sender.Send(new GetEvaluatorDashboardQuery(), cancellationToken);
                    return Ok(result);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Json(ApiResponse.Fail("Bạn chưa đăng nhập hoặc phiên đã hết hạn."), statusCode: 401);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lỗi khi tải dữ liệu tổng quan thẩm định");
                    return Results.Json(ApiResponse.Fail("Không thể tải dữ liệu tổng quan. Vui lòng thử lại sau."), statusCode: 500);
                }
            })
            .RequireAuthorization(PolicyNames.RequireEvaluator)
            .WithTags("Evaluator")
            .WithName("GetEvaluatorDashboard")
            .Produces<ApiResponse<EvaluatorDashboardDto>>()
            .Produces(401);
    }
}
