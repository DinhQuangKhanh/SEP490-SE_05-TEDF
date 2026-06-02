using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Specifications.TopicPools;

/// <summary>
/// Specification to get topics (Projects from pool) by major.
/// </summary>
public class TopicsByMajorSpec : BaseSpecification<Project>
{
    /// <summary>
    /// Gets all topics in a major's topic pool.
    /// </summary>
    /// <param name="majorId">Major ID</param>
    /// <param name="availableOnly">If true, only return available topics</param>
    /// <param name="includeExpired">If true, include expired topics</param>
    public TopicsByMajorSpec(int majorId, bool availableOnly = true, bool includeExpired = false)
        : base(p => p.SourceType == ProjectSourceType.FromPool &&
                    p.MajorId == majorId &&
                    (!availableOnly || p.PoolStatus == PoolTopicStatus.Available) &&
                    (includeExpired || p.PoolStatus != PoolTopicStatus.Expired))
    {
        AddInclude(p => p.Mentors);
        ApplyOrderByDescending(p => p.CreatedAt);
    }
}
