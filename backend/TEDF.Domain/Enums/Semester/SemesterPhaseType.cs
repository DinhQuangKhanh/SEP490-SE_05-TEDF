namespace TEDF.Domain.Enums.Semester;

/// <summary>
/// Types of phases within a semester.
/// </summary>
public enum SemesterPhaseType
{
    /// <summary>Topic registration phase.</summary>
    Registration = 0,

    /// <summary>Topic evaluation/review phase.</summary>
    Evaluation = 1,

    /// <summary>Project implementation phase.</summary>
    Implementation = 2,

    /// <summary>Defense/presentation phase.</summary>
    Defense = 3
}