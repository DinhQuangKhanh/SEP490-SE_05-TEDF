using System.Text.RegularExpressions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Aggregates.SemesterAggregate.Entities;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.RegisterForm;

/// <summary>
/// Applies the propose-time rules (b → a → c, stop on first failure) to a parsed register form and maps
/// it onto a <see cref="RegisterFormProposalResult"/>. Pure — no I/O — so it is directly unit-testable.
/// Throws a <see cref="BusinessRuleValidationException"/> carrying the exact reason on the first failure.
/// </summary>
internal static partial class RegisterFormProposalValidator
{
    private const int ContentMaxLength = 4000;   // matches the widened Project content columns

    public static RegisterFormProposalResult ValidateAndMap(
        RegisterFormContent content,
        IReadOnlyList<EligibleMentor> eligibleMentors,
        Guid currentUserId)
    {
        // b — "Kinds of person make registers" must be ticked as Lecturer (null = box unread → treat as not).
        if (content.LecturerRegisterTicked != true)
            throw new BusinessRuleValidationException("Kinds of person make registers cần phải là Lecturer");

        // a1 — the supervisor block is COMPLETELY empty (the user left it blank): no supervisor carries
        // a name or an e-mail. A row with only an e-mail (the PDF export can't recover the name) is NOT
        // empty — it goes on to the a2 mentor-match check below.
        var hasAnySupervisorInfo = content.Supervisors.Any(s =>
            !string.IsNullOrWhiteSpace(s.FullName) || !string.IsNullOrWhiteSpace(s.Email));
        if (!hasAnySupervisorInfo)
            throw new BusinessRuleValidationException("Bạn cần bổ sung thông tin về Mentor của đề tài này");

        // a2 — the LOGGED-IN lecturer must be the mentor named on the form: match the form's supervisor
        // e-mails against the published eligible-mentor list to get their ids, then require the current
        // user's id to be among them. This blocks proposing a topic whose mentor is somebody else (a
        // supervisor that is unknown/not published, or a colleague rather than the person uploading).
        var mentorIdByEmail = eligibleMentors
            .Where(m => !string.IsNullOrWhiteSpace(m.Email))
            .GroupBy(m => m.Email!.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First().MentorId);

        var matchedMentorIds = content.Supervisors
            .Select(s => s.Email?.Trim().ToLowerInvariant())
            .Where(email => !string.IsNullOrEmpty(email) && mentorIdByEmail.ContainsKey(email))
            .Select(email => mentorIdByEmail[email!])
            .Distinct()
            .ToList();

        if (!matchedMentorIds.Contains(currentUserId))
            throw new BusinessRuleValidationException("Tài khoản đăng nhập và mentor của đề tài không khớp nhau");

        // c1 — 3.1 English / Vietnamese / Abbreviation (3–5 uppercase letters).
        if (string.IsNullOrWhiteSpace(content.NameEn)
            || string.IsNullOrWhiteSpace(content.NameVi)
            || !IsValidAbbreviation(content.NameAbbr))
            throw new BusinessRuleValidationException(
                "Vui lòng hoàn thành mục 3.1 (English/Vietnamese/Abbreviation) trong phiếu đăng ký");

        // c2 — 3.2 Objectives + Technology/algorithm.
        if (string.IsNullOrWhiteSpace(content.Objectives) || content.Technologies.Count == 0)
            throw new BusinessRuleValidationException(
                "Vui lòng hoàn thành mục 3.2 (Objectives, Technology/algorithm) trong phiếu đăng ký");

        // c3 — 3.3 Summarize contents & expected outputs.
        if (string.IsNullOrWhiteSpace(content.ExpectedResults))
            throw new BusinessRuleValidationException(
                "Vui lòng hoàn thành mục 3.3 (Summarize contents & expected outputs) trong phiếu đăng ký");

        // c4 — 3.4 Expected features.
        if (string.IsNullOrWhiteSpace(content.Scope))
            throw new BusinessRuleValidationException(
                "Vui lòng hoàn thành mục 3.4 (Expected features) trong phiếu đăng ký");

        // Description ← 3.2 brief intro. It is required in the DB, so fall back to another non-blank
        // field when the intro paragraph was left empty (3.2 only gates on Objectives + Technology).
        var description = FirstNonBlank(content.Description, content.ExpectedResults, content.NameEn)!;

        return new RegisterFormProposalResult(
            NameEn: Clamp(content.NameEn!.Trim(), ProjectName.MaxLength),
            NameVi: Clamp(content.NameVi!.Trim(), ProjectName.MaxLength),
            NameAbbr: content.NameAbbr!.Trim().ToUpperInvariant(),
            Description: Clamp(description.Trim(), ContentMaxLength),
            Objectives: Clamp(content.Objectives!.Trim(), ContentMaxLength),
            Technologies: Clamp(string.Join(", ", content.Technologies), ContentMaxLength),
            ExpectedResults: Clamp(content.ExpectedResults!.Trim(), ContentMaxLength),
            Scope: Clamp(content.Scope!.Trim(), ContentMaxLength),
            // The topic's mentor is the logged-in lecturer (already proven to be one of the form's
            // published supervisors above), not necessarily every supervisor listed on the form.
            MentorIds: [currentUserId]);
    }

    private static bool IsValidAbbreviation(string? value) =>
        value is not null && AbbreviationRegex().IsMatch(value.Trim());

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string Clamp(string value, int max) => value.Length <= max ? value : value[..max];

    [GeneratedRegex("^[A-Z]{3,5}$")]
    private static partial Regex AbbreviationRegex();
}
