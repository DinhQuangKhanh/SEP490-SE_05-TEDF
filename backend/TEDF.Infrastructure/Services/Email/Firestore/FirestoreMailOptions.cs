namespace TEDF.Infrastructure.Services.Email.Firestore;

/// <summary>
/// Settings for the transactional-mail transport. Mail is not sent over SMTP from this process:
/// the backend appends a document to a Firestore collection and the
/// <c>firebase/firestore-send-email</c> extension renders the template and talks to SMTP.
/// Credentials for that SMTP account live in the extension's secret, never in this repository.
/// </summary>
public class FirestoreMailOptions
{
    public const string SectionName = "FirestoreMail";

    /// <summary>
    /// Master switch. When false (or when the project id cannot be resolved) mail is skipped with a
    /// warning instead of throwing — a developer without Firestore credentials can still run the API.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Firestore project id. Falls back to <c>Firebase:ProjectId</c> when left blank.</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Service-account key used to reach Firestore. Falls back to <c>Firebase:ServiceAccountKeyPath</c>,
    /// then to application-default credentials. The file itself is git-ignored.
    /// </summary>
    public string ServiceAccountKeyPath { get; set; } = string.Empty;

    /// <summary>Collection watched by the Trigger Email extension.</summary>
    public string MailCollection { get; set; } = "mail";

    /// <summary>
    /// Origin of the SPA, used to turn in-app routes into absolute links for the
    /// <c>detailUrl</c> placeholder (e.g. <c>https://tedf.example.edu.vn</c>).
    /// </summary>
    public string FrontendBaseUrl { get; set; } = string.Empty;
}
