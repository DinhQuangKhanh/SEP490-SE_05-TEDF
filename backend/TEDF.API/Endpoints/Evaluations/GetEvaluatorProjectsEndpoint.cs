using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Common;
using TEDF.Application.Features.Evaluations.DTOs;
using TEDF.Application.Features.Evaluations.Queries.GetEvaluatorProjects;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Evaluations;

public class GetEvaluatorProjectsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/evaluator/projects", async (
                ISender sender,
                ILogger<GetEvaluatorProjectsEndpoint> logger,
                int page = 1,
                int pageSize = 10,
                string? search = null,
                int? semesterId = null,
                int? majorId = null,
                string? result = null,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var query = new GetEvaluatorProjectsQuery(page, pageSize, search, semesterId, majorId, result);
                    var dto = await sender.Send(query, cancellationToken);
                    return Ok(dto);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Json(ApiResponse.Fail("Bạn chưa đăng nhập hoặc phiên đã hết hạn."), statusCode: 401);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lỗi khi tải danh sách đề tài cần thẩm định");
                    return Results.Json(ApiResponse.Fail("Không thể tải danh sách đề tài cần thẩm định. Vui lòng thử lại sau."), statusCode: 500);
                }
            })
            .RequireAuthorization(PolicyNames.RequireEvaluator)
            .WithTags("Evaluator")
            .WithName("GetEvaluatorProjects")
            .Produces<ApiResponse<EvaluatorProjectsDto>>()
            .Produces(401);
    }
}
