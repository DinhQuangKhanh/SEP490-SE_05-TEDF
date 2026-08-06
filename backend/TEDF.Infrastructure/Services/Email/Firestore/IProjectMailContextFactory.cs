namespace TEDF.Infrastructure.Services.Email.Firestore;

/// <summary>A person an email can be addressed to.</summary>
public sealed record MailRecipient(Guid UserId, string FullName, string Email);

/// <summary>
/// Everything the project-centred templates need about one project, resolved once so each email
/// handler does not repeat the same repository walk.
/// </summary>
/// <param name="Round">
/// <c>Project.EvaluationCount</c>. It increments on every submit/resubmit, so it separates one
/// evaluation cycle from the next and belongs in every dedupe key of a per-cycle email —
/// without it a resubmitted topic would be silently treated as an already-sent duplicate.
/// </param>
public sealed record ProjectMailContext(
    Guid ProjectId,
    string ProjectName,
    int Round,
    int SemesterId,
    DateTime CreatedAtUtc,
    MailRecipient? Mentor,
    MailRecipient? DepartmentHead,
    string DepartmentName,
    IReadOnlyList<MailRecipient> Students);

/// <summary>Resolves the people involved in a project for the transactional-mail handlers.</summary>
public interface IProjectMailContextFactory
{
    /// <summary>Returns null when the project no longer exists.</summary>
    Task<ProjectMailContext?> CreateAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Looks up a single user, e.g. the evaluator or the person who made an assignment.</summary>
    Task<MailRecipient?> GetUserAsync(Guid userId, CancellationToken ct = default);
}
