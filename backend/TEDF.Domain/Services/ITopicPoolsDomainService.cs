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

    /// <summary>
    /// Mentor proposes a new topic into a pool; returns the new project id.
    /// </summary>
    /// <param name="registerForm">
    /// The capstone register form (PDF or DOCX), which is <b>required</b> to propose a topic.
    /// Reading its student table is nonetheless best-effort: when it lists students they are
    /// recorded as the topic's proposed roster and become a group once the topic passes evaluation,
    /// but a form with an empty or unreadable table simply leaves the topic on the normal pool flow.
    /// </param>
    Task<Guid> ProposeTopicAsync(Guid poolId, Guid mentorId, PoolTopicContent content, byte[] registerForm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads &amp; validates a register form for a proposal WITHOUT creating anything — used by the
    /// "validate before submit" step so the modal unlocks only on a clean, complete form. Throws a
    /// <see cref="TEDF.Domain.Common.Exceptions.BusinessRuleValidationException"/> with the specific
    /// reason (Kinds-of-person / mentor mismatch / missing 3.1–3.4) when the form is not acceptable;
    /// otherwise returns the mapped preview. <paramref name="currentUserId"/> is the logged-in lecturer,
    /// who must themselves be the mentor named on the form.
    /// </summary>
    Task<RegisterFormProposalResult> ValidateRegisterFormAsync(Guid poolId, byte[] registerForm, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mentor proposes a new topic by uploading the completed register form. Parses &amp; validates the
    /// form (same rules as <see cref="ValidateRegisterFormAsync"/>), maps its 3.1–3.4 fields onto the
    /// project, sets the mentor to the logged-in lecturer (<paramref name="currentUserId"/>, required to
    /// be the mentor named on the form), records the roster, stores the optional note, and returns the
    /// new project id.
    /// </summary>
    Task<(Guid ProjectId, RegisterFormProposalResult Content)> ProposeTopicFromFormAsync(Guid poolId, byte[] registerForm, string? mentorNote, Guid currentUserId, CancellationToken cancellationToken = default);

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

/// <summary>
/// The 3.1–3.4 content mapped off a register form plus the mentor(s) matched to the published
/// eligible-mentor list. Feeds both the "validate" preview and the actual proposal. All string fields
/// are already validated non-blank / clamped to their column limits by the mapping step.
/// </summary>
public record RegisterFormProposalResult(
    string NameEn,
    string NameVi,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Technologies,
    string? ExpectedResults,
    string? Scope,
    IReadOnlyList<Guid> MentorIds
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
