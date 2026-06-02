using MediatR;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Topics.DTOs;
using TEDF.Application.Features.Topics.Queries.GetTopicDetail;
using TEDF.Application.Features.Topics.Queries.GetTopicsInPool;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Students.Topics;

public partial class TopicEndpoints : IEndpoint
{
    private static void MapQueryEndpoints(RouteGroupBuilder group)
    {
        // ─────────────────────────────────────────────────────────────
        // Queries: các endpoint chỉ để đọc dữ liệu, không làm thay đổi state
        // ─────────────────────────────────────────────────────────────

        #region Lấy danh sách đề tài trong pool

        // GET /api/topics
        // Trả về danh sách đề tài theo bộ lọc (ngành, tìm kiếm, trạng thái, sắp xếp, phân trang).
        group.MapGet("/", GetTopicsInPool)
            .WithName("GetTopicsInPool")
            .WithTags("Topics")
            .Produces<GetTopicsInPoolResult>()
            .Produces(401);

        #endregion

        #region Lấy chi tiết đề tài theo Id

        // GET /api/topics/{topicId}
        // Trả về chi tiết đề tài theo topicId.
        group.MapGet("{topicId:guid}", GetTopicDetail)
            .WithName("GetTopicDetail")
            .WithTags("Topics")
            .Produces<TopicDetailDto>()
            .Produces(404)
            .Produces(401);

        #endregion

        #region Lấy tài liệu của đề tài

        // GET /api/topics/{topicId}/documents
        // Trả về danh sách tài liệu của đề tài.
        group.MapGet("{topicId:guid}/documents", GetTopicDocuments)
            .WithName("GetTopicDocuments")
            .WithTags("Topics")
            .Produces<List<TopicDocumentDto>>()
            .Produces(401);

        #endregion
    }

    #region Handler: lấy danh sách đề tài trong pool

    private static async Task<IResult> GetTopicsInPool(
        ISender sender,
        int? majorId = null,
        string? search = null,
        int? poolStatus = null,
        string? sortBy = null,
        int page = 1,
        int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetTopicsInPoolQuery(majorId, search, poolStatus, sortBy, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Handler: lấy chi tiết đề tài theo Id

    private static async Task<IResult> GetTopicDetail(
        ISender sender,
        Guid topicId,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTopicDetailQuery(topicId), cancellationToken);
        return result is not null ? Ok(result) : Results.NotFound();
    }

    #endregion

    #region Handler: lấy tài liệu của đề tài

    private static async Task<IResult> GetTopicDocuments(
        Guid topicId,
        ITopicQueryService queryService,
        CancellationToken cancellationToken)
    {
        var documents = await queryService.GetTopicDocumentsAsync(topicId, cancellationToken);
        return Ok(documents);
    }

    #endregion
}
