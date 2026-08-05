namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Reads the student roster off the "Capstone Project Register" PDF a mentor attaches when
/// proposing a topic. Parsing is best-effort: a form with an empty student table yields an empty
/// roster, which leaves the topic on the normal pool flow.
/// </summary>
public interface IRegisterFormParser
{
    /// <summary>
    /// Extracts the student rows of section "2. Register information for students".
    /// </summary>
    /// <param name="pdfStream">The uploaded PDF content.</param>
    /// <returns>One entry per filled-in row; empty when the table has no students.</returns>
    IReadOnlyList<RegisterRosterRow> ExtractRoster(Stream pdfStream);
}

/// <summary>
/// A student row read off the register form. Names are deliberately not captured: the Word-exported
/// PDFs mangle Vietnamese diacritics, so matching is done on the ASCII code/email only.
/// </summary>
/// <param name="StudentCode">The student code cell, if present.</param>
/// <param name="Email">The e-mail cell, if present.</param>
/// <param name="IsLeader">True when the row's "Role in Group" cell reads Leader.</param>
public record RegisterRosterRow(string? StudentCode, string? Email, bool IsLeader);
