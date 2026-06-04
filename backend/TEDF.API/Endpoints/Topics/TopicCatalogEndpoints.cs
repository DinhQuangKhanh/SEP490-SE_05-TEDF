using MediatR;
using TEDF.Application.Features.Topics.DTOs;
using TEDF.Application.Features.Topics.Queries.GetTopicsInPool;
using TEDF.Application.Features.Topics.Queries.GetTopicDetail;
using TEDF.Application.Features.Topics.Services;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Topics;

public sealed class TopicCatalogEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics").RequireAuthorization();

        group.MapGet("", GetTopicsInPool)
            .WithTags("Topics")
            .WithName("GetTopicsInPool")
            .Produces(200).Produces(401);

        group.MapGet("/{topicId:guid}", GetTopicDetail)
            .WithTags("Topics")
            .WithName("GetTopicDetail")
            .Produces<TopicDetailDto>().Produces(401).Produces(404);

        group.MapGet("/{topicId:guid}/documents", GetTopicDocuments)
            .WithTags("Topics")
            .WithName("GetTopicDocuments")
            .Produces(200).Produces(401);
    }

    private static async Task<IResult> GetTopicsInPool(
        ISender sender,
        int? majorId = null,
        string? search = null,
        int? poolStatus = null,
        string? sortBy = null,
        int page = 1,
        int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetTopicsInPoolQuery(majorId, search, poolStatus, sortBy, page, pageSize), ct);
        return Ok(result);
    }

    private static async Task<IResult> GetTopicDetail(ISender sender, Guid topicId, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetTopicDetailQuery(topicId), ct);
        return result is not null ? Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> GetTopicDocuments(
        Guid topicId,
        ITopicQueryService queryService,
        CancellationToken ct)
    {
        var documents = await queryService.GetTopicDocumentsAsync(topicId, ct);
        return Ok(documents);
    }
}
