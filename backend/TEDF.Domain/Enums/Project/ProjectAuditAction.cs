namespace TEDF.Domain.Enums.Project;

/// <summary>
/// Actions recorded in the project approval audit trail.
/// </summary>
/// <remarks>
/// Values are persisted as integers in <c>ProjectAuditLogs.Action</c>.
/// Never renumber or reuse an existing member — historical rows would change meaning.
/// </remarks>
public enum ProjectAuditAction
{
    /// <summary>Group submitted the project for evaluation.</summary>
    Submitted = 0,

    /// <summary>Group submitted the project again after a modification request.</summary>
    Resubmitted = 1,

    /// <summary>Group sent the project to its mentor for review (direct registration).</summary>
    SubmittedToMentor = 2,

    /// <summary>Mentor approved the project.</summary>
    MentorApproved = 3,

    /// <summary>Mentor requested a modification.</summary>
    MentorNeedsModification = 4,

    /// <summary>Evaluation board approved the project.</summary>
    Approved = 5,

    /// <summary>Evaluation board requested a modification.</summary>
    NeedsModification = 6,

    /// <summary>Evaluation board rejected the project.</summary>
    Rejected = 7,

    /// <summary>A mentor was assigned to the project.</summary>
    MentorAssigned = 8,

    /// <summary>An evaluator (reviewer) was assigned to the project.</summary>
    EvaluatorAssigned = 9,

    /// <summary>A document was uploaded to the project.</summary>
    DocumentUploaded = 10,

    /// <summary>A document was deleted from the project.</summary>
    DocumentDeleted = 11
}
