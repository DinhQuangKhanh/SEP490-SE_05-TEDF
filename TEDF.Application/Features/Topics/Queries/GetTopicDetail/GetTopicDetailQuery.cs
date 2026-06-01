using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Topics.DTOs;

namespace TEDF.Application.Features.Topics.Queries.GetTopicDetail;

/// <summary>
/// Query to get full details of a single thesis topic by its ID.
/// Works for all source types (FromPool and DirectRegistration).
/// Cached per topic ID with longer TTL since topic details rarely change.
/// </summary>
public record GetTopicDetailQuery(Guid TopicId) : ICachedQuery<TopicDetailDto?>
{
    public string? CacheKey => $"topics:detail:{TopicId}";
    public TimeSpan? L1Expiration => TimeSpan.FromMinutes(5);
    public TimeSpan? L2Expiration => TimeSpan.FromMinutes(15);
}
