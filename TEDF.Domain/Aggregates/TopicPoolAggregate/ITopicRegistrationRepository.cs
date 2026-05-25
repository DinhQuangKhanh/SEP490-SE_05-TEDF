using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate;

/// <summary>
/// Repository interface for TopicRegistration entity.
/// </summary>
public interface ITopicRegistrationRepository : IRepository<TopicRegistration, Guid>
{
    /// <summary>
    /// Gets all registrations for a project/topic.
    /// </summary>
    Task<IEnumerable<TopicRegistration>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registrations by a group.
    /// </summary>
    Task<IEnumerable<TopicRegistration>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending registrations for a project.
    /// </summary>
    Task<IEnumerable<TopicRegistration>> GetPendingByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a group already has a pending registration for a project.
    /// </summary>
    Task<bool> HasPendingRegistrationAsync(Guid groupId, Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the confirmed registration for a project (if any).
    /// </summary>
    Task<TopicRegistration?> GetConfirmedByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all confirmed registrations for a group.
    /// </summary>
    Task<IEnumerable<TopicRegistration>> GetConfirmedByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending registrations for a mentor's projects.
    /// </summary>
    Task<IEnumerable<TopicRegistration>> GetPendingByMentorIdAsync(Guid mentorId, CancellationToken cancellationToken = default);

    Task<int> CountPendingByProjectIdExcludingAsync(Guid projectId, Guid excludeRegistrationId, CancellationToken cancellationToken = default);

    Task<Dictionary<TopicRegistrationStatus, int>> GetRegistrationStatusCountsByProjectIdsAsync(IEnumerable<Guid> projectIds, CancellationToken cancellationToken = default);
}
