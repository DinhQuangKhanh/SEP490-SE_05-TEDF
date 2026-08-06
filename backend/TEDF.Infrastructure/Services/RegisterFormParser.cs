using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace TEDF.Infrastructure.Services;

/// <summary>
/// Parses the capstone register PDF with PdfPig. Words are regrouped into visual lines by their
/// Y position, because the student table's cells otherwise extract out of order.
/// </summary>
public partial class RegisterFormParser : IRegisterFormParser
{
    private const double LineTolerance = 4.0;

    private readonly ILogger<RegisterFormParser> _logger;

    public RegisterFormParser(ILogger<RegisterFormParser> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<RegisterRosterRow> ExtractRoster(Stream pdfStream)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        List<string> lines;
        try
        {
            lines = ReadLines(pdfStream);
        }
        catch (Exception ex)
        {
            // Best-effort by design: an unreadable form must not block the proposal.
            _logger.LogWarning(ex, "Could not read the attached register PDF; continuing without a roster.");
            return [];
        }

        var section = SliceStudentSection(lines);
        var rows = new List<RegisterRosterRow>();

        foreach (var line in section)
        {
            if (!StudentRowRegex().IsMatch(line))
                continue;

            var code = FindStudentCode(line);
            var email = FindEmail(line);

            // A row with neither identifier is a blank template row.
            if (code is null && email is null)
                continue;

            rows.Add(new RegisterRosterRow(code, email, IsLeaderRow(line)));
        }

        // The form marks the leader explicitly; if that cell was edited away, fall back to the
        // first row, which is the "Leader" slot in the template.
        if (rows.Count > 0 && !rows.Any(r => r.IsLeader))
            rows[0] = rows[0] with { IsLeader = true };

        return rows;
    }

    /// <summary>Regroups every page's words into visual lines, top-to-bottom, left-to-right.</summary>
    private static List<string> ReadLines(Stream pdfStream)
    {
        if (pdfStream.CanSeek)
            pdfStream.Seek(0, SeekOrigin.Begin);

        var lines = new List<string>();
        using var document = PdfDocument.Open(pdfStream);

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords()
                .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                .ToList();

            foreach (var group in GroupIntoLines(words))
            {
                var text = string.Join(' ', group.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                if (!string.IsNullOrWhiteSpace(text))
                    lines.Add(text);
            }
        }

        return lines;
    }

    private static IEnumerable<List<Word>> GroupIntoLines(List<Word> words)
    {
        // PDF origin is bottom-left, so descending Y walks the page downwards.
        var remaining = words.OrderByDescending(w => w.BoundingBox.Bottom).ToList();
        var current = new List<Word>();
        double? baseline = null;

        foreach (var word in remaining)
        {
            var y = word.BoundingBox.Bottom;
            if (baseline is null || Math.Abs(baseline.Value - y) <= LineTolerance)
            {
                baseline ??= y;
                current.Add(word);
                continue;
            }

            yield return current;
            current = [word];
            baseline = y;
        }

        if (current.Count > 0)
            yield return current;
    }

    /// <summary>
    /// Narrows the lines to section 2 (the student table). Falls back to the whole document when
    /// the headings cannot be located, since the "Student N" row pattern is selective enough.
    /// </summary>
    private static List<string> SliceStudentSection(List<string> lines)
    {
        var start = lines.FindIndex(l => l.Contains("Register information for students", StringComparison.OrdinalIgnoreCase));
        if (start < 0)
            return lines;

        var end = lines.FindIndex(start + 1, l => l.Contains("Register content", StringComparison.OrdinalIgnoreCase));
        if (end < 0)
            end = lines.Count;

        return lines.GetRange(start, end - start);
    }

    private static bool IsLeaderRow(string line) =>
        line.Contains("leader", StringComparison.OrdinalIgnoreCase);

    private static string? FindStudentCode(string line)
    {
        var match = StudentCodeRegex().Match(line);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static string? FindEmail(string line)
    {
        var match = EmailRegex().Match(line);
        return match.Success ? match.Value.ToLowerInvariant() : null;
    }

    /// <summary>Matches the row labels the template prints: "Student 1" … "Student 5".</summary>
    [GeneratedRegex(@"\bStudent\s*\d\b", RegexOptions.IgnoreCase)]
    private static partial Regex StudentRowRegex();

    /// <summary>
    /// Student codes are letters followed by digits (e.g. HE160123). Requiring the leading letters
    /// keeps phone numbers — which are digits only — from being mistaken for a code.
    /// </summary>
    [GeneratedRegex(@"\b[A-Za-z]{2,3}\d{4,8}\b")]
    private static partial Regex StudentCodeRegex();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b")]
    private static partial Regex EmailRegex();
}
