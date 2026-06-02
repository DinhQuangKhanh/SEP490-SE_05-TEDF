using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Domain.Specifications.TopicPools;

/// <summary>
/// Specification to get all active topic pools.
/// </summary>
public class ActiveTopicPoolsSpec : BaseSpecification<TopicPool>
{
    /// <summary>
    /// Gets all active (non-suspended) topic pools.
    /// </summary>
    public ActiveTopicPoolsSpec()
        : base(tp => tp.Status == TopicPoolStatus.Active)
    {
        ApplyOrderBy(tp => tp.MajorId);
    }
}
