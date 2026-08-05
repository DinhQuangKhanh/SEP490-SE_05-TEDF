namespace TEDF.Infrastructure.Services.Email.Firestore;

/// <summary>
/// Names of the documents in the Firestore <c>emailTemplates</c> collection. The subject/text/html
/// bodies live there, not in this repository — the backend only picks a name and supplies the
/// placeholder values. Renaming a constant here without renaming the Firestore document breaks
/// delivery, so these strings are the contract.
/// </summary>
public static class MailTemplateNames
{
    public const string EvaluationAssigned = "evaluation-assigned";
    public const string PublishedStudentList = "published-student-list";
    public const string PublishedLecturerList = "published-lecturer-list";
    public const string TopicProposed = "topic-proposed";
    public const string EvaluationCompleted = "evaluation-completed";
    public const string EvaluationConsensusApproved = "evaluation-consensus-approved";
    public const string EvaluationConsensusRejected = "evaluation-consensus-rejected";
    public const string TopicFinalDecision = "topic-final-decision";
}
