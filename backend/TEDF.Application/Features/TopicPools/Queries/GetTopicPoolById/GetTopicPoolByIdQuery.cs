using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetTopicPoolById;

public record GetTopicPoolByIdQuery(Guid Id) : ICachedQuery<TopicPoolDto>
{
    public string? CacheKey => $"topic-pools:{Id}";
}
