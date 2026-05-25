using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Specifications.TopicPools;

/// <summary>
/// Specification to get topics that are expiring soon.
/// Topics expire after N semesters (default 2) without being registered.
/// </summary>
public class ExpiringTopicsInPoolSpec : BaseSpecification<Project>
{
    /// <summary>
    /// Gets topics that will expire in the next semester.
    /// </summary>
    /// <param name="currentSemesterId">Current semester ID</param>
    /// <param name="topicPoolId">Optional: Filter by specific topic pool</param>
    public ExpiringTopicsInPoolSpec(int currentSemesterId, Guid? topicPoolId = null)
        : base(p => p.SourceType == ProjectSourceType.FromPool &&
                    p.PoolStatus == PoolTopicStatus.Available &&
                    p.ExpirationSemesterId.HasValue &&
                    p.ExpirationSemesterId.Value <= currentSemesterId + 1 &&
                    (!topicPoolId.HasValue || p.TopicPoolId == topicPoolId.Value))
    {
        AddInclude(p => p.Mentors);
        ApplyOrderBy(p => p.ExpirationSemesterId!);
    }
}
