using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Specifications.TopicPools;

/// <summary>
/// Specification to get available topics (Projects) in a topic pool.
/// Topics are Projects with SourceType = FromPool and PoolStatus = Available.
/// </summary>
public class AvailableTopicsInPoolSpec : BaseSpecification<Project>
{
    /// <summary>
    /// Gets all available topics in a specific pool or across all pools.
    /// </summary>
    /// <param name="topicPoolId">Optional: Filter by specific topic pool</param>
    /// <param name="majorId">Optional: Filter by major</param>
    public AvailableTopicsInPoolSpec(Guid? topicPoolId = null, int? majorId = null)
        : base(p => p.SourceType == ProjectSourceType.FromPool &&
                    p.PoolStatus == PoolTopicStatus.Available &&
                    p.Status == ProjectStatus.Approved &&
                    (!topicPoolId.HasValue || p.TopicPoolId == topicPoolId.Value) &&
                    (!majorId.HasValue || p.MajorId == majorId.Value))
    {
        AddInclude(p => p.Mentors);
        ApplyOrderByDescending(p => p.CreatedAt);
    }
}
