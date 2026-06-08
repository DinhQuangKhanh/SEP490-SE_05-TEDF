namespace TEDF.Domain.Enums.TopicPool;

/// <summary>
/// Status of a Topic Pool (kho đề tài).
/// Each major has one permanent pool.
/// </summary>
public enum TopicPoolStatus
{
    /// <summary>
    /// Pool is active - accepting topic proposals and registrations.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Pool is temporarily suspended - not accepting new proposals/registrations.
    /// </summary>
    Suspended = 1
}