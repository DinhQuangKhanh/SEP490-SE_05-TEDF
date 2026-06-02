using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetTopicPools;

public record GetTopicPoolsQuery(int? MajorId = null) : ICachedQuery<List<TopicPoolDto>>
{
    public string? CacheKey => $"topic-pools:list:{MajorId}";
}
