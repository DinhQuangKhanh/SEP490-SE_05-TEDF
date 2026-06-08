using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetTopicPools;

public class GetTopicPoolsQueryHandler : IQueryHandler<GetTopicPoolsQuery, List<TopicPoolDto>>
{
    private readonly ITopicPoolsQueryService _queryService;

    public GetTopicPoolsQueryHandler(ITopicPoolsQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<List<TopicPoolDto>> Handle(GetTopicPoolsQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetTopicPoolsAsync(request.MajorId, cancellationToken);
    }
}
