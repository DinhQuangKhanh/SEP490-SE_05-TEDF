namespace TEDF.Domain.Enums.Semester;

/// <summary>
/// Status of a semester phase.
/// </summary>
public enum SemesterPhaseStatus
{
    /// <summary>Phase has not started yet.</summary>
    NotStarted = 0,

    /// <summary>Phase is currently in progress.</summary>
    InProgress = 1,

    /// <summary>Phase has been completed.</summary>
    Completed = 2
}