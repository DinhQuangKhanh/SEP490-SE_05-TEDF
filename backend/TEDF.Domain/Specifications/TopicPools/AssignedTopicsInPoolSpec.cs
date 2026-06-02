using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Specifications.TopicPools;

/// <summary>
/// Specification to get topics that have been assigned to groups.
/// </summary>
public class AssignedTopicsInPoolSpec : BaseSpecification<Project>
{
    /// <summary>
    /// Gets all assigned topics (topics that have groups).
    /// </summary>
    /// <param name="topicPoolId">Optional: Filter by specific topic pool</param>
    /// <param name="semesterId">Optional: Filter by semester</param>
    public AssignedTopicsInPoolSpec(Guid? topicPoolId = null, int? semesterId = null)
        : base(p => p.SourceType == ProjectSourceType.FromPool &&
                    p.PoolStatus == PoolTopicStatus.Assigned &&
                    p.GroupId.HasValue &&
                    (!topicPoolId.HasValue || p.TopicPoolId == topicPoolId.Value) &&
                    (!semesterId.HasValue || p.SemesterId == semesterId.Value))
    {
        AddInclude(p => p.Mentors);
        ApplyOrderByDescending(p => p.UpdatedAt);
    }
}
