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
}

/// <summary>
/// A student row read off the register form. Names are deliberately not captured: the Word-exported
/// PDFs mangle Vietnamese diacritics, so matching is done on the ASCII code/email only.
/// </summary>
/// <param name="StudentCode">The student code cell, if present.</param>
/// <param name="Email">The e-mail cell, if present.</param>
/// <param name="IsLeader">True when the row's "Role in Group" cell reads Leader.</param>
public record RegisterRosterRow(string? StudentCode, string? Email, bool IsLeader);
