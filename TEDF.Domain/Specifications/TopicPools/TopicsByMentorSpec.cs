using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Specifications.TopicPools;

/// <summary>
/// Specification to get topics (Projects from pool) proposed by a mentor.
/// </summary>
public class TopicsByMentorSpec : BaseSpecification<Project>
{
    /// <summary>
    /// Gets all topics proposed by a specific mentor in the topic pool.
    /// </summary>
    /// <param name="mentorId">Mentor's user ID</param>
    /// <param name="poolStatus">Optional: Filter by pool status</param>
    /// <param name="topicPoolId">Optional: Filter by specific topic pool</param>
    public TopicsByMentorSpec(Guid mentorId, PoolTopicStatus? poolStatus = null, Guid? topicPoolId = null)
        : base(p => p.SourceType == ProjectSourceType.FromPool &&
                    p.Mentors.Any(m => m.MentorId == mentorId && m.IsActive) &&
                    (!poolStatus.HasValue || p.PoolStatus == poolStatus.Value) &&
                    (!topicPoolId.HasValue || p.TopicPoolId == topicPoolId.Value))
    {
        AddInclude(p => p.Mentors);
        ApplyOrderByDescending(p => p.CreatedAt);
    }
}
