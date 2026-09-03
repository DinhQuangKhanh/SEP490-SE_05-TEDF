namespace TEDF.Domain.Services;

/// <summary>
/// Write-side service for a mentor proposing / editing a topic in a pool: validate the uploaded
/// register form, create the topic, edit a draft, and resubmit for evaluation. Split out of the old
/// god-service <c>ITopicPoolsDomainService</c> (single responsibility: proposal lifecycle).
/// </summary>
public interface ITopicProposalService
{
    /// <summary>
    /// Write-flow guard: whether a mentor may propose another topic to a pool (the pool is accepting
    /// proposals and the mentor is under the MaxTopicsPerMentor limit). Not a display read.
    /// </summary>
    Task<(bool CanPropose, string? Reason)> CanMentorProposeTopicAsync(
        Guid mentorId, Guid topicPoolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads &amp; validates a register form for a proposal WITHOUT creating anything — used by the
    /// "validate before submit" step so the modal unlocks only on a clean, complete form. Throws a
    /// <see cref="TEDF.Domain.Common.Exceptions.BusinessRuleValidationException"/> with the specific
    /// reason (Kinds-of-person / mentor mismatch / missing 3.1–3.4) when the form is not acceptable.
    /// </summary>
    Task<RegisterFormProposalResult> ValidateRegisterFormAsync(
        Guid poolId, byte[] registerForm, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mentor proposes a new topic by uploading the completed register form. Parses &amp; validates the
    /// form (same rules as <see cref="ValidateRegisterFormAsync"/>), maps its 3.1–3.4 fields onto the
    /// project, sets the mentor to the logged-in lecturer, records the roster, stores the optional
    /// note, and returns the new project id.
    /// <para>
    /// <paramref name="editedContent"/> lets the mentor correct the parsed 3.1–3.4 content in the modal
    /// before submitting (a null field keeps what was read from the form). The supervisor block and the
    /// "kinds of person" checkbox are NEVER taken from the client — the mentor-match still runs against
    /// the uploaded file, so a mentor cannot propose a topic they are not named on.
    /// </para>
    /// </summary>
    Task<(Guid ProjectId, RegisterFormProposalResult Content)> ProposeTopicFromFormAsync(
        Guid poolId, byte[] registerForm, string? mentorNote, Guid currentUserId,
        RegisterFormContentEdit? editedContent = null, CancellationToken cancellationToken = default);

    /// <summary>Mentor edits a pool topic (allowed only while Draft/NeedsModification).</summary>
    Task UpdatePoolTopicAsync(Guid projectId, PoolTopicContent content, CancellationToken cancellationToken = default);

    /// <summary>Mentor submits/resubmits a pool topic for evaluation.</summary>
    Task ResubmitPoolTopicAsync(Guid projectId, Guid mentorId, CancellationToken cancellationToken = default);
}

/// <summary>
/// The 3.1–3.4 content mapped off a register form plus the mentor(s) matched to the published
/// eligible-mentor list. Feeds both the "validate" preview and the actual proposal. All string fields
/// are already validated non-blank / clamped to their column limits by the mapping step.
/// </summary>
public record RegisterFormProposalResult(
    string NameEn,
    string NameVi,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Technologies,
    string? ExpectedResults,
    string? Scope,
    IReadOnlyList<Guid> MentorIds
);

/// <summary>
/// Mentor-supplied corrections to the parsed 3.1–3.4 content, applied on top of what was read off the
/// uploaded register form before validation. A <c>null</c> field keeps the parsed value. The supervisor
/// block and the "kinds of person" checkbox are intentionally NOT here: they gate the mentor-match and
/// must come from the file only.
/// </summary>
public record RegisterFormContentEdit(
    string? NameEn,
    string? NameVi,
    string? NameAbbr,
    string? Description,
    string? Objectives,
    string? Technologies,   // comma-separated list, as shown in the modal
    string? ExpectedResults,
    string? Scope
);

/// <summary>Editable content of a pool topic (used by propose &amp; update).</summary>
public record PoolTopicContent(
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    int MaxStudents
);
