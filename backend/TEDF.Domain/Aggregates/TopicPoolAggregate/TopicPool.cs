using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate;

/// <summary>
/// TopicPool represents a permanent topic pool (kho đề tài) for a specific major.
/// Each major has exactly ONE topic pool that exists permanently.
/// The pool contains multiple projects/topics proposed by mentors.
/// Projects in the pool expire after 2 semesters if not registered.
/// </summary>
public class TopicPool : AggregateRoot<Guid>
{
    #region Properties

    /// <summary>
    /// Unique code for the topic pool (e.g., "KHO-SE", "KHO-AI").
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Name of the topic pool (e.g., "Kho đề tài Kỹ thuật phần mềm").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description of the topic pool.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The major this pool belongs to. One major = One pool (permanent).
    /// </summary>
    public int MajorId { get; private set; }

    /// <summary>
    /// Current status of the pool.
    /// </summary>
    public TopicPoolStatus Status { get; private set; }

    /// <summary>
    /// Maximum number of active topics a single mentor can have in this pool.
    /// </summary>
    public int MaxActiveTopicsPerMentor { get; private set; }

    /// <summary>
    /// Number of semesters before an unregistered topic expires (default: 2).
    /// </summary>
    public int ExpirationSemesters { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    #endregion

    #region Constructors

    private TopicPool() { }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new permanent topic pool for a major.
    /// Should only be called once per major.
    /// </summary>
    public static TopicPool Create(
        string code,
        string name,
        string? description,
        int majorId,
        int maxActiveTopicsPerMentor = 5,
        int expirationSemesters = 2,
        Guid? createdBy = null)
    {
        var pool = new TopicPool
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            MajorId = majorId,
            Status = TopicPoolStatus.Active,
            MaxActiveTopicsPerMentor = maxActiveTopicsPerMentor,
            ExpirationSemesters = expirationSemesters,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        pool.RaiseDomainEvent(new TopicPoolCreatedEvent(pool.Id, pool.Code, pool.MajorId));

        return pool;
    }

    #endregion

    #region Status Management

    /// <summary>
    /// Suspends the topic pool (temporarily stop accepting new topics/registrations).
    /// </summary>
    public void Suspend(Guid suspendedBy)
    {
        if (Status != TopicPoolStatus.Active)
            throw new BusinessRuleValidationException("Only active pools can be suspended.");

        Status = TopicPoolStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = suspendedBy;

        RaiseDomainEvent(new TopicPoolSuspendedEvent(Id));
    }

    /// <summary>
    /// Reactivates a suspended topic pool.
    /// </summary>
    public void Activate(Guid activatedBy)
    {
        if (Status != TopicPoolStatus.Suspended)
            throw new BusinessRuleValidationException("Only suspended pools can be activated.");

        Status = TopicPoolStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = activatedBy;

        RaiseDomainEvent(new TopicPoolActivatedEvent(Id));
    }

    /// <summary>
    /// Checks if the pool is accepting new topic proposals.
    /// </summary>
    public bool IsAcceptingProposals() => Status == TopicPoolStatus.Active;

    /// <summary>
    /// Checks if the pool is accepting registrations.
    /// </summary>
    public bool IsAcceptingRegistrations() => Status == TopicPoolStatus.Active;

    #endregion

    #region Update Methods

    /// <summary>
    /// Updates the topic pool information.
    /// </summary>
    public void Update(
        string? name = null,
        string? description = null,
        int? maxActiveTopicsPerMentor = null,
        int? expirationSemesters = null,
        Guid? updatedBy = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;

        if (description != null)
            Description = description;

        if (maxActiveTopicsPerMentor.HasValue && maxActiveTopicsPerMentor.Value > 0)
            MaxActiveTopicsPerMentor = maxActiveTopicsPerMentor.Value;

        if (expirationSemesters.HasValue && expirationSemesters.Value > 0)
            ExpirationSemesters = expirationSemesters.Value;

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    #endregion

    #region Code Generation Helper

    /// <summary>
    /// Generates a topic pool code from major code.
    /// </summary>
    public static string GenerateCode(string majorCode)
    {
        return $"KHO-{majorCode.ToUpperInvariant()}";
    }

    #endregion
}
