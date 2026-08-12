using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Services.RegisterForm;

/// <summary>
/// Turns an uploaded "Capstone Project Register" form into roster rows. The format is detected from
/// the content's leading bytes and dispatched to the matching reader; everything after that —
/// deciding which rows count and who the leader is — is shared, so PDF and DOCX uploads of the same
/// form produce the same roster.
/// </summary>
public class RegisterFormParser : IRegisterFormParser
{
    private readonly ILogger<RegisterFormParser> _logger;

    public RegisterFormParser(ILogger<RegisterFormParser> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<RegisterRosterRow> ExtractRoster(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        IReadOnlyList<RegisterFormRow> formRows;
        try
        {
            formRows = ReadRows(stream);
        }
        catch (Exception ex)
        {
            // Best-effort by design: an unreadable form must not block the proposal.
            _logger.LogWarning(ex, "Could not read the attached register form; continuing without a roster.");
            return [];
        }

        var rows = MatchStudents(formRows);

        // The form marks the leader explicitly; if that cell was edited away, fall back to the
        // first row, which is the "Leader" slot in the template.
        if (rows.Count > 0 && !rows.Any(r => r.IsLeader))
            rows[0] = rows[0] with { IsLeader = true };

        return rows;
    }

    private IReadOnlyList<RegisterFormRow> ReadRows(Stream stream)
    {
        // Both readers need to seek, and the caller may hand us a forward-only stream.
        using var seekable = ToSeekable(stream);

        var format = RegisterFormFormatDetector.Detect(seekable);
        switch (format)
        {
            case RegisterFormFormat.Pdf:
                return PdfRegisterFormReader.Read(seekable);

            case RegisterFormFormat.Docx:
                return DocxRegisterFormReader.Read(seekable);

            default:
                _logger.LogWarning(
                    "The attached register form is neither a PDF nor a DOCX; continuing without a roster.");
                return [];
        }
    }

    private static MemoryStream ToSeekable(Stream stream)
    {
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);

        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        buffer.Seek(0, SeekOrigin.Begin);
        return buffer;
    }

    private static List<RegisterRosterRow> MatchStudents(IReadOnlyList<RegisterFormRow> formRows)
    {
        var rows = new List<RegisterRosterRow>();

        foreach (var row in formRows)
        {
            if (!RegisterRowMatcher.IsStudentRow(row.Text))
                continue;

            var code = RegisterRowMatcher.FindStudentCode(row.Text);
            var email = RegisterRowMatcher.FindEmail(row.Text);

            // A row with neither identifier is a blank template row.
            if (code is null && email is null)
                continue;

            rows.Add(new RegisterRosterRow(code, email, IsLeaderRow(row, code, email)));
        }

        return rows;
    }

    /// <summary>
    /// Reads the "Role in Group" cell. When the format preserved cells (DOCX) the code and e-mail
    /// cells are excluded first, so a student whose address happens to be leader@… is not promoted.
    /// The PDF reader supplies the whole row as one cell, where that refinement is not possible.
    /// </summary>
    private static bool IsLeaderRow(RegisterFormRow row, string? code, string? email)
    {
        if (row.Cells.Count <= 1)
            return RegisterRowMatcher.IsLeaderText(row.Text);

        return row.Cells.Any(cell =>
            !IsIdentifierCell(cell, code, email) && RegisterRowMatcher.IsLeaderText(cell));
    }

    private static bool IsIdentifierCell(string cell, string? code, string? email) =>
        (code is not null && RegisterRowMatcher.FindStudentCode(cell) == code)
        || (email is not null && RegisterRowMatcher.FindEmail(cell) == email);
}
