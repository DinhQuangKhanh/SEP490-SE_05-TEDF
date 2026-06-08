using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.Application.Common;
using TEDF.Application.Features.Topics.Queries.GetMentorTopics;
using TEDF.Application.Features.Topics.DTOs;
using TEDF.Application.Features.Topics.Queries.GetTopicsInPool;
using TEDF.Application.Features.Topics.Queries.GetTopicDetail;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;
using TEDF.Application.Common.Interfaces;

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

        // Topics owned by the current mentor (moved from the Mentor role folder).
        // Literal "mentor" never matches the {topicId:guid} route, so there is no conflict.
        group.MapGet("/mentor", GetMentorTopics)
            .RequireAuthorization(PolicyNames.RequireMentor)
            .WithTags("Topics").WithName("GetMentorTopics")
            .Produces<ApiResponse<GetMentorTopicsResult>>().Produces(401);
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

    private static async Task<IResult> GetMentorTopics(
        [FromQuery] int? semesterId,
        [FromQuery] string? search,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ISender sender,
        CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;
        return Ok(await sender.Send(new GetMentorTopicsQuery(semesterId, search, page, pageSize), ct));
    }

    private static async Task<IResult> GetTopicDetail(ISender sender, Guid topicId, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetTopicDetailQuery(topicId), ct);
        return result is not null ? Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> GetTopicDocuments(
        Guid topicId,
        ITopicsQueryService queryService,
        CancellationToken ct)
    {
        var documents = await queryService.GetTopicDocumentsAsync(topicId, ct);
        return Ok(documents);
    }
}
