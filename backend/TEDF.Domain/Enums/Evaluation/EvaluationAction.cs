namespace TEDF.Domain.Enums.Evaluation;

/// <summary>
/// Actions that can occur during the evaluation process (for logging).
/// </summary>
public enum EvaluationAction
{
    /// <summary>Project was submitted for evaluation.</summary>
    Submitted = 0,

    /// <summary>Evaluator was assigned to the submission.</summary>
    Assigned = 1,

    /// <summary>Evaluator was reassigned.</summary>
    Reassigned = 2,

    /// <summary>Evaluator started reviewing.</summary>
    ReviewStarted = 3,

    /// <summary>Feedback was added by the evaluator.</summary>
    FeedbackAdded = 4,

    /// <summary>Project was approved.</summary>
    Approved = 5,

    /// <summary>Project needs modification.</summary>
    NeedsModification = 6,

    /// <summary>Project was rejected.</summary>
    Rejected = 7,

    /// <summary>Project was modified by the mentor.</summary>
    Modified = 8,

    /// <summary>Project was resubmitted after modification.</summary>
    Resubmitted = 9,

    /// <summary>Evaluation was cancelled.</summary>
    Cancelled = 10,

    /// <summary>Evaluation was completed.</summary>
    Completed = 11
}