using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate;

/// <summary>
/// Repository interface for TopicPool aggregate.
/// </summary>
public interface ITopicPoolRepository : IRepository<TopicPool, Guid>
{
    /// <summary>
    /// Gets a topic pool by its code.
    /// </summary>
    Task<TopicPool?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the topic pool for a specific major.
    /// Each major has exactly one permanent pool.
    /// </summary>
    Task<TopicPool?> GetByMajorIdAsync(int majorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active topic pools.
    /// </summary>
    Task<IEnumerable<TopicPool>> GetActivePoolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a topic pool code already exists.
    /// </summary>
    Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a topic pool already exists for a major.
    /// </summary>
    Task<bool> ExistsForMajorAsync(int majorId, CancellationToken cancellationToken = default);
}
