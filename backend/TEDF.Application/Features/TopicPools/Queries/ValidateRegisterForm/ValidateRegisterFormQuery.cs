using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.TopicPools.Queries.ValidateRegisterForm;

/// <summary>
/// Step A of the propose flow: parse &amp; validate an uploaded register form WITHOUT creating anything,
/// so the modal can unlock "Gửi phê duyệt" only on a clean, complete form. Throws the specific
/// business-rule error (Kinds-of-person / mentor mismatch / missing 3.1–3.4) on failure; on success
/// returns the parsed fields for the mentor to review before submitting.
/// </summary>
public record ValidateRegisterFormQuery(Guid PoolId, byte[] RegisterForm) : IQuery<RegisterFormPreviewDto>;

/// <summary>The 3.1–3.4 fields read off the form, shown as a preview in the propose modal.</summary>
public sealed record RegisterFormPreviewDto(
    string NameEn,
    string NameVi,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Technologies,
    string? ExpectedResults,
    string? Scope,
    int MentorCount);
