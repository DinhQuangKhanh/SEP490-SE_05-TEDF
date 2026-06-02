namespace TEDF.Domain.Enums.Project;

/// <summary>
/// Status of a project throughout its lifecycle.
/// </summary>
public enum ProjectStatus
{
    /// <summary>Project is in draft state, not yet submitted.</summary>
    Draft = 0,

    /// <summary>Project has been submitted and is pending evaluation.</summary>
    PendingEvaluation = 1,

    /// <summary>Project needs modification based on evaluator feedback.</summary>
    NeedsModification = 2,

    /// <summary>Project has been approved.</summary>
    Approved = 3,

    /// <summary>Project has been rejected.</summary>
    Rejected = 4,

    /// <summary>Project is in progress (being implemented).</summary>
    InProgress = 5,

    /// <summary>Project has been completed successfully.</summary>
    Completed = 6,

    /// <summary>Project has been cancelled.</summary>
    Cancelled = 7,

    /// <summary>Project submitted to mentor for review (direct registration only).</summary>
    PendingMentorReview = 8
}