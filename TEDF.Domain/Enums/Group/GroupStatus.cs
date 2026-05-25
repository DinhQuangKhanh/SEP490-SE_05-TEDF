namespace TEDF.Domain.Enums.Group;

/// <summary>
/// Status of a student group.
/// </summary>
public enum GroupStatus
{
    /// <summary>Group is active.</summary>
    Active = 0,

    /// <summary>Group has completed its project.</summary>
    Completed = 1,

    /// <summary>Group has been disbanded.</summary>
    Disbanded = 2
}