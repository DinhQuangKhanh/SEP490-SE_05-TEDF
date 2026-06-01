using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetTopicPoolStatistics;

public class GetTopicPoolStatisticsQueryHandler : IQueryHandler<GetTopicPoolStatisticsQuery, TopicPoolStatisticsDto>
{
    private readonly ITopicPoolQueryService _queryService;

    public GetTopicPoolStatisticsQueryHandler(ITopicPoolQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<TopicPoolStatisticsDto> Handle(GetTopicPoolStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetTopicPoolStatisticsAsync(request.PoolId, cancellationToken);
    }
}
