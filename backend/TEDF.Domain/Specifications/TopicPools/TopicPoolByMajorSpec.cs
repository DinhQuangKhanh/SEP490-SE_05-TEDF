using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Domain.Specifications.TopicPools;

/// <summary>
/// Specification to get topic pool by major.
/// Each major has exactly one permanent topic pool.
/// </summary>
public class TopicPoolByMajorSpec : BaseSpecification<TopicPool>
{
    /// <summary>
    /// Gets the topic pool for a specific major.
    /// </summary>
    /// <param name="majorId">Major ID</param>
    public TopicPoolByMajorSpec(int majorId)
        : base(tp => tp.MajorId == majorId)
    {
    }
}
