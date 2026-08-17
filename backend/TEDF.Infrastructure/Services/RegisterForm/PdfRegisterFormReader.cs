using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace TEDF.Infrastructure.Services.RegisterForm;

/// <summary>
/// Reads the register form's PDF export with PdfPig. Words are regrouped into visual lines by their
/// Y position, because the student table's cells otherwise extract out of order.
/// <para>
/// A row therefore arrives as one flattened line, e.g.
/// <c>"Student 1 Student LoadTest 33 LT000033 0987000033 student33@fpt.edu.vn Leader"</c> — the
/// column boundaries are lost, which is why the DOCX reader (which walks real table cells) is the
/// more precise path when the source document is available.
/// </para>
/// </summary>
internal static class PdfRegisterFormReader
{
    private const double LineTolerance = 4.0;

    public static IReadOnlyList<RegisterFormRow> Read(Stream stream)
    {
        var lines = ReadLines(stream);
        var section = SliceStudentSection(lines);

        // Each line is already a whole row, so the code/email/leader cells cannot be told apart.
        return [.. section.Select(line => new RegisterFormRow(line, Cells: [line]))];
    }

    /// <summary>
    /// Reads the whole PDF into the normalized <see cref="RegisterFormDoc"/> — visual lines only. The
    /// export loses table structure, so <see cref="RegisterFormDoc.Tables"/> stays empty and the
    /// content extractor recovers the supervisor / student rows from the lines instead.
    /// </summary>
    public static RegisterFormDoc ReadDocument(Stream stream) =>
        new(ReadLines(stream), []);

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
    /// <para>
    /// This slice is what keeps the supervisor's e-mail — printed in section 1, directly above —
    /// from being read as a student.
    /// </para>
    /// </summary>
    private static List<string> SliceStudentSection(List<string> lines)
    {
        var start = lines.FindIndex(l => l.Contains(RegisterFormHeadings.StudentSection, StringComparison.OrdinalIgnoreCase));
        if (start < 0)
            return lines;

        var end = lines.FindIndex(start + 1, l => l.Contains(RegisterFormHeadings.ContentSection, StringComparison.OrdinalIgnoreCase));
        if (end < 0)
            end = lines.Count;

        return lines.GetRange(start, end - start);
    }
}
