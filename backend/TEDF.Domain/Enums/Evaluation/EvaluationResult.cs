namespace TEDF.Domain.Enums.Evaluation;

/// <summary>
/// Result of an evaluation.
/// </summary>
public enum EvaluationResult
{
    /// <summary>Evaluation is pending.</summary>
    Pending = 0,

    /// <summary>Project has been approved.</summary>
    Approved = 1,

    /// <summary>Project needs modification.</summary>
    NeedsModification = 2,

    /// <summary>Project has been rejected.</summary>
    Rejected = 3
}