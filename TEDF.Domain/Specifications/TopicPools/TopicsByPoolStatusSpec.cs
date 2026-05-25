using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Specifications.TopicPools;

/// <summary>
/// Specification to get topics by their pool status.
/// </summary>
public class TopicsByPoolStatusSpec : BaseSpecification<Project>
{
    /// <summary>
    /// Gets topics by their status within the topic pool.
    /// </summary>
    /// <param name="poolStatus">Pool status to filter by</param>
    /// <param name="topicPoolId">Optional: Filter by specific topic pool</param>
    /// <param name="majorId">Optional: Filter by major</param>
    public TopicsByPoolStatusSpec(PoolTopicStatus poolStatus, Guid? topicPoolId = null, int? majorId = null)
        : base(p => p.SourceType == ProjectSourceType.FromPool &&
                    p.PoolStatus == poolStatus &&
                    (!topicPoolId.HasValue || p.TopicPoolId == topicPoolId.Value) &&
                    (!majorId.HasValue || p.MajorId == majorId.Value))
    {
        AddInclude(p => p.Mentors);
        ApplyOrderByDescending(p => p.CreatedAt);
    }
}
