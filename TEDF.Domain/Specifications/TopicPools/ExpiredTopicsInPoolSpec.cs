using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Specifications.TopicPools;

/// <summary>
/// Specification to get expired topics in the pool.
/// These are topics that were not registered within the expiration period.
/// </summary>
public class ExpiredTopicsInPoolSpec : BaseSpecification<Project>
{
    /// <summary>
    /// Gets all expired topics.
    /// </summary>
    /// <param name="topicPoolId">Optional: Filter by specific topic pool</param>
    /// <param name="majorId">Optional: Filter by major</param>
    public ExpiredTopicsInPoolSpec(Guid? topicPoolId = null, int? majorId = null)
        : base(p => p.SourceType == ProjectSourceType.FromPool &&
                    p.PoolStatus == PoolTopicStatus.Expired &&
                    (!topicPoolId.HasValue || p.TopicPoolId == topicPoolId.Value) &&
                    (!majorId.HasValue || p.MajorId == majorId.Value))
    {
        AddInclude(p => p.Mentors);
        ApplyOrderByDescending(p => p.UpdatedAt);
    }
}
