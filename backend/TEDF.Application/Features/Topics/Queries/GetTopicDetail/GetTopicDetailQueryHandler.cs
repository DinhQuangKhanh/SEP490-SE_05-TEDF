using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Topics.DTOs;

namespace TEDF.Application.Features.Topics.Queries.GetTopicDetail;

public class GetTopicDetailQueryHandler : IQueryHandler<GetTopicDetailQuery, TopicDetailDto?>
{
    private readonly ITopicQueryService _queryService;

    public GetTopicDetailQueryHandler(ITopicQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<TopicDetailDto?> Handle(GetTopicDetailQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetTopicDetailAsync(request.TopicId, cancellationToken);
    }
}
