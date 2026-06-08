namespace TEDF.Domain.Enums.Evaluation;

/// <summary>
/// Status of an evaluation submission.
/// </summary>
public enum SubmissionStatus
{
    /// <summary>Submission is pending evaluator assignment.</summary>
    Pending = 0,

    /// <summary>Submission is being reviewed by the evaluator.</summary>
    InReview = 1,

    /// <summary>Submission review has been completed.</summary>
    Completed = 2,

    /// <summary>Submission has been cancelled.</summary>
    Cancelled = 3
}
