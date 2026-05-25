namespace TEDF.Domain.Enums.TopicPool;

/// <summary>
/// Status of a topic registration request.
/// </summary>
public enum TopicRegistrationStatus
{
    /// <summary>
    /// Registration is pending mentor confirmation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Registration has been confirmed, group is assigned to the topic.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Registration was rejected by mentor.
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Registration was cancelled by the group.
    /// </summary>
    Cancelled = 3
}
