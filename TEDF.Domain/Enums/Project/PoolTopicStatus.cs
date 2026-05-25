namespace TEDF.Domain.Enums.Project;

/// <summary>
/// Status of a project/topic within the topic pool.
/// Only applicable when SourceType = FromPool.
/// </summary>
public enum PoolTopicStatus
{
    /// <summary>
    /// Topic is available for registration by groups.
    /// </summary>
    Available = 0,

    /// <summary>
    /// A group has requested to register this topic, pending mentor confirmation.
    /// </summary>
    Reserved = 1,

    /// <summary>
    /// Topic has been assigned to a group.
    /// </summary>
    Assigned = 2,

    /// <summary>
    /// Topic expired after 2 semesters without being registered.
    /// </summary>
    Expired = 3
}
