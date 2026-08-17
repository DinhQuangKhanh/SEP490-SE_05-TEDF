namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Reads the student roster off the "Capstone Project Register" form a mentor attaches when
/// proposing a topic. Both the PDF export and the source DOCX are accepted; the format is detected
/// from the stream's leading bytes, so callers need not say which one they hold.
/// <para>
/// Attaching the form is mandatory, but reading its <em>contents</em> stays best-effort: a form with
/// an empty or unreadable student table yields an empty roster, which simply leaves the topic on the
/// normal pool flow instead of failing the proposal.
/// </para>
/// </summary>
public interface IRegisterFormParser
{
    /// <summary>
    /// Extracts the student rows of section "2. Register information for students".
    /// </summary>
    /// <param name="stream">The uploaded form content, in either PDF or DOCX format.</param>
    /// <returns>One entry per filled-in row; empty when the table has no students.</returns>
    IReadOnlyList<RegisterRosterRow> ExtractRoster(Stream stream);

    /// <summary>
    /// Reads the full proposable content off the register form: the supervisor(s), whether the
    /// "Kinds of person make registers" box is ticked as Lecturer, the section 3.1–3.4 project
    /// fields, and the student roster. Accepts DOCX / DOC / PDF (detected from the leading bytes).
    /// <para>
    /// Best-effort like <see cref="ExtractRoster"/>: fields that cannot be read come back null/empty
    /// rather than throwing, so the caller's validation layer decides what to reject on. Only a
    /// completely unreadable file throws (the caller maps that to a "cannot read the form" error).
    /// </para>
    /// </summary>
    RegisterFormContent ExtractContent(Stream stream);
}

/// <summary>Everything the propose flow reads off one "Capstone Project Register" form.</summary>
public sealed record RegisterFormContent(
    /// <summary>Section 1 supervisors that carry a name or e-mail (blank template rows dropped).</summary>
    IReadOnlyList<RegisterFormSupervisor> Supervisors,
    /// <summary>True/false = "Kinds of person" ticked Lecturer / not; null = state could not be read.</summary>
    bool? LecturerRegisterTicked,
    string? NameEn,        // 3.1 English
    string? NameVi,        // 3.1 Vietnamese
    string? NameAbbr,      // 3.1 Abbreviation (parenthetical hint stripped)
    string? Description,   // 3.2 brief introduction (Objectives & Technology excluded)
    string? Objectives,    // 3.2 Objectives
    IReadOnlyList<string> Technologies,  // 3.2 Technology/algorithm — flattened list of tech names
    string? ExpectedResults, // 3.3 "expected outputs" portion (falls back to the whole 3.3)
    string? Scope,           // 3.4 Expected features
    IReadOnlyList<RegisterRosterRow> Roster);  // section 2

/// <summary>A supervisor read off section 1. <see cref="Email"/> is what the mentor check matches on.</summary>
public sealed record RegisterFormSupervisor(string? FullName, string? Email, string? Title);

/// <summary>
/// A student row read off the register form. Names are deliberately not captured: the Word-exported
/// PDFs mangle Vietnamese diacritics, so matching is done on the ASCII code/email only.
/// </summary>
/// <param name="StudentCode">The student code cell, if present.</param>
/// <param name="Email">The e-mail cell, if present.</param>
/// <param name="IsLeader">True when the row's "Role in Group" cell reads Leader.</param>
public record RegisterRosterRow(string? StudentCode, string? Email, bool IsLeader);
