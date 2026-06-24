using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;
using TEDF.Domain.Aggregates.ProjectAggregate;

namespace TEDF.Domain.Services;

/// <summary>
/// Domain service for topic pool-related business logic.
/// </summary>
public interface ITopicPoolsDomainService
{
    /// <summary>
    /// Generates a unique topic pool code for a major.
    /// </summary>
    Task<string> GeneratePoolCodeAsync(int majorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or gets the topic pool for a major (each major has exactly one pool).
    /// </summary>
    Task<TopicPool> GetOrCreatePoolAsync(int majorId, Guid createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active topics a mentor has in a specific pool.
    /// Active = Available or Reserved (not Assigned or Expired).
    /// </summary>
    Task<int> GetMentorActiveTopicCountAsync(Guid mentorId, Guid topicPoolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a mentor can propose a new topic to a pool.
    /// </summary>
    Task<(bool CanPropose, string? Reason)> CanMentorProposeTopicAsync(
        Guid mentorId,
        Guid topicPoolId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a topic registration request from a group.
    /// </summary>
    Task<TopicRegistration> RequestRegistrationAsync(
        Guid projectId,
        Guid groupId,
        Guid registeredBy,
        int priority = 1,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a topic registration and assigns the group to the project.
    /// </summary>
    Task ConfirmRegistrationAsync(
        Guid registrationId,
        Guid confirmedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a topic registration.
    /// </summary>
    Task RejectRegistrationAsync(
        Guid registrationId,
        Guid rejectedBy,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending topic registration. Only the leader of the registering group may cancel.
    /// </summary>
    Task CancelRegistrationAsync(
        Guid registrationId,
        Guid cancelledBy,
        string? reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for a topic pool.
    /// </summary>
    Task<TopicPoolStatistics> GetPoolStatisticsAsync(Guid topicPoolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires topics that have been in the pool for more than N semesters without registration.
    /// Should be called at the start of each semester.
    /// </summary>
    Task<int> ExpireOldTopicsAsync(int currentSemesterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available topics in a pool for registration (approved and Available status).
    /// </summary>
    Task<IEnumerable<Guid>> GetAvailableTopicsInPoolAsync(Guid topicPoolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets topics that will expire soon (e.g., within the next semester).
    /// Returned items are projects (topics) from the pool.
    /// </summary>
    Task<IEnumerable<Project>> GetExpiringTopicsAsync(int currentSemesterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Backfills expiration semester for approved pool topics that are still missing expiration metadata.
    /// </summary>
    Task<int> ResolveMissingExpirationSemestersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the expiration semester ID for a new topic.
    /// </summary>
    Task<int?> CalculateExpirationSemesterAsync(int createdSemesterId, int expirationSemesters, CancellationToken cancellationToken = default);

    // ── TopicPools feature write operations (moved in from command handlers) ──

    /// <summary>Mentor proposes a new topic into a pool; returns the new project id.</summary>
    Task<Guid> ProposeTopicAsync(Guid poolId, Guid mentorId, PoolTopicContent content, CancellationToken cancellationToken = default);

    /// <summary>Mentor edits a pool topic (allowed only while Draft/NeedsModification).</summary>
    Task UpdatePoolTopicAsync(Guid projectId, PoolTopicContent content, CancellationToken cancellationToken = default);

    /// <summary>Mentor submits/resubmits a pool topic for evaluation.</summary>
    Task ResubmitPoolTopicAsync(Guid projectId, Guid mentorId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for a topic pool.
/// </summary>
public record TopicPoolStatistics(
    int TotalTopics,
    int AvailableTopics,
    int ReservedTopics,
    int AssignedTopics,
    int ExpiredTopics,
    int TotalRegistrations,
    int PendingRegistrations,
    int ConfirmedRegistrations,
    int TopMentorTopicCount,
    double AverageTopicsPerMentor
);

/// <summary>Editable content of a pool topic (used by propose &amp; update).</summary>
public record PoolTopicContent(
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    int MaxStudents
);
