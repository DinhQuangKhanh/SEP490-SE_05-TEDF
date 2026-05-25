namespace TEDF.Domain.Enums.Evaluation;

/// <summary>
/// Status of an evaluation process.
/// </summary>
public enum EvaluationStatus
{
    /// <summary>Evaluation is scheduled.</summary>
    Scheduled = 0,

    /// <summary>Evaluation is in progress.</summary>
    InProgress = 1,

    /// <summary>Evaluation has been completed.</summary>
    Completed = 2
}