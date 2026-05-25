using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;

/// <summary>
/// Represents a group's registration request for a project/topic from the pool.
/// </summary>
public class TopicRegistration : Entity<Guid>
{
    #region Properties

    /// <summary>
    /// The project (topic) being registered for.
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// The group requesting to register.
    /// </summary>
    public Guid GroupId { get; private set; }

    /// <summary>
    /// The user who submitted the registration.
    /// </summary>
    public Guid RegisteredBy { get; private set; }

    /// <summary>
    /// When the registration was submitted.
    /// </summary>
    public DateTime RegisteredAt { get; private set; }

    /// <summary>
    /// Current status of the registration.
    /// </summary>
    public TopicRegistrationStatus Status { get; private set; }

    /// <summary>
    /// Priority/preference number if group registers for multiple topics
    /// (1 = first choice, 2 = second choice, etc.)
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Optional note from the group explaining why they want this topic.
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// User who processed (confirmed/rejected) the registration.
    /// </summary>
    public Guid? ProcessedBy { get; private set; }

    /// <summary>
    /// When the registration was processed.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Reason for rejection or cancellation.
    /// </summary>
    public string? RejectReason { get; private set; }

    #endregion

    #region Constructors

    private TopicRegistration() { }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new topic registration.
    /// </summary>
    public static TopicRegistration Create(
        Guid projectId,
        Guid groupId,
        Guid registeredBy,
        int priority = 1,
        string? note = null)
    {
        return new TopicRegistration
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            GroupId = groupId,
            RegisteredBy = registeredBy,
            RegisteredAt = DateTime.UtcNow,
            Status = TopicRegistrationStatus.Pending,
            Priority = priority,
            Note = note
        };
    }

    #endregion

    #region Methods

    /// <summary>
    /// Confirms the registration (by mentor).
    /// </summary>
    public void Confirm(Guid confirmedBy)
    {
        if (Status != TopicRegistrationStatus.Pending)
            throw new InvalidOperationException("Only pending registrations can be confirmed.");

        Status = TopicRegistrationStatus.Confirmed;
        ProcessedBy = confirmedBy;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the registration (by mentor).
    /// </summary>
    public void Reject(Guid rejectedBy, string reason)
    {
        if (Status != TopicRegistrationStatus.Pending)
            throw new InvalidOperationException("Only pending registrations can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        Status = TopicRegistrationStatus.Rejected;
        ProcessedBy = rejectedBy;
        ProcessedAt = DateTime.UtcNow;
        RejectReason = reason;
    }

    /// <summary>
    /// Cancels the registration (by the group).
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (Status == TopicRegistrationStatus.Confirmed)
            throw new InvalidOperationException("Confirmed registrations cannot be cancelled.");

        if (Status == TopicRegistrationStatus.Cancelled)
            throw new InvalidOperationException("Registration is already cancelled.");

        Status = TopicRegistrationStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
        RejectReason = reason;
    }

    #endregion
}
