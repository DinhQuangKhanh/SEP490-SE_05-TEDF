using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetTopicPoolStatistics;

public record GetTopicPoolStatisticsQuery(Guid PoolId) : ICachedQuery<TopicPoolStatisticsDto>
{
    public string? CacheKey => $"topic-pools:{PoolId}:stats";
}
