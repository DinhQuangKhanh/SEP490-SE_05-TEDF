using TEDF.API.Extensions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Topics.DTOs;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Topics;

public class GetTopicDocumentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/topics/{topicId:guid}/documents", async (
                Guid topicId,
                ITopicQueryService queryService,
                CancellationToken cancellationToken) =>
            {
                var documents = await queryService.GetTopicDocumentsAsync(topicId, cancellationToken);
                return Ok(documents);
            })
            .RequireAuthorization()
            .WithTags("Topics")
            .WithName("GetTopicDocuments")
            .Produces<List<TopicDocumentDto>>()
            .Produces(401);
    }
}
