namespace TEDF.Domain.Enums.Semester;

/// <summary>
/// Status of a semester.
/// </summary>
public enum SemesterStatus
{
    /// <summary>Semester is scheduled but not yet started.</summary>
    Upcoming = 0,

    /// <summary>Semester is currently active.</summary>
    Ongoing = 1,

    /// <summary>Semester has ended.</summary>
    Ended = 2
}