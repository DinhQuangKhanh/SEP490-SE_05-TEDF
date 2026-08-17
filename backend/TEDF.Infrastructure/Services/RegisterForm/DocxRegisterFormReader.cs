using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TEDF.Infrastructure.Services.RegisterForm;

/// <summary>
/// Reads the register form straight out of the Word source with the OpenXML SDK.
/// <para>
/// This is the more precise of the two readers: Word keeps the student table as real
/// <see cref="TableRow"/>/<see cref="TableCell"/> elements, so each field arrives in its own cell.
/// The PDF export loses that structure — <see cref="PdfRegisterFormReader"/> has to rebuild rows
/// from glyph positions and ends up with one flattened line per row.
/// </para>
/// <para>
/// Reference layout, taken from the real filled form (SP26._BusDN-HanhNT54_filled-test.pdf):
/// a paragraph "2. Register information for students", then a table whose header reads
/// <c>No. | Full name | Student code | Phone | E-mail | Role in Group</c> and whose body rows read
/// <c>Student 1 | Student LoadTest 33 | LT000033 | 0987000033 | student33@fpt.edu.vn | Leader</c>,
/// closed by a paragraph "3. Register content of Capstone Project".
/// </para>
/// </summary>
internal static class DocxRegisterFormReader
{
    public static IReadOnlyList<RegisterFormRow> Read(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, isEditable: false);

        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
            return [];

        var rows = new List<RegisterFormRow>();
        foreach (var table in SelectStudentTables(body))
        {
            foreach (var row in table.Elements<TableRow>())
            {
                var cells = row.Elements<TableCell>().Select(CellText).ToList();
                if (cells.Count == 0)
                    continue;

                rows.Add(new RegisterFormRow(string.Join(' ', cells), cells));
            }
        }

        return rows;
    }

    /// <summary>
    /// Reads the whole form into the normalized <see cref="RegisterFormDoc"/>: every body paragraph as
    /// one or more lines (a soft line break inside a paragraph — e.g. "Vietnamese: …" then
    /// "Abbreviation: …" — is split onto its own line) plus every table as rows of cells.
    /// </summary>
    public static RegisterFormDoc ReadDocument(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, isEditable: false);

        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
            return new RegisterFormDoc([], []);

        var lines = new List<string>();
        var tables = new List<IReadOnlyList<IReadOnlyList<string>>>();

        foreach (var element in body.ChildElements)
        {
            switch (element)
            {
                case Paragraph paragraph:
                    lines.AddRange(ParagraphLines(paragraph));
                    break;
                case Table table:
                    tables.Add(ReadTable(table));
                    break;
            }
        }

        return new RegisterFormDoc(lines, tables);
    }

    /// <summary>
    /// Flattens a paragraph to text — a newline per soft break, a space per tab — then splits on the
    /// newlines so labels that share one paragraph each land on their own line. Internal whitespace is
    /// collapsed and empty lines dropped.
    /// <para>
    /// Walks the run tree in document order (rather than a flat <c>Descendants</c> pass) so a
    /// content-control checkbox can be read as a whole: Word stores the "Lecturer ✓" tick as an
    /// <c>&lt;w:sdt&gt;</c> whose state lives in <c>w14:checked</c> and whose visible mark is a
    /// <c>&lt;w:sym&gt;</c> (Wingdings) — neither of which is <c>&lt;w:t&gt;</c> text — so the naive
    /// reader saw no tick and reported the box as empty. Here the checkbox is emitted as a ☑ / ☐ glyph
    /// the content extractor already understands, and its inner symbol/text is not walked twice.
    /// </para>
    /// </summary>
    private static IEnumerable<string> ParagraphLines(Paragraph paragraph)
    {
        var builder = new StringBuilder();
        AppendText(paragraph, builder);

        foreach (var raw in builder.ToString().Split('\n'))
        {
            var line = string.Join(' ', raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (line.Length > 0)
                yield return line;
        }
    }

    private static void AppendText(OpenXmlElement parent, StringBuilder builder)
    {
        foreach (var node in parent.ChildElements)
        {
            switch (node)
            {
                case Text text: builder.Append(text.Text); break;
                case Break: builder.Append('\n'); break;
                case TabChar: builder.Append(' '); break;
                case SymbolChar sym: builder.Append(SymbolGlyph(sym)); break;
                case SdtElement sdt when TryReadCheckbox(sdt, out var glyph):
                    builder.Append(glyph);                 // authoritative state — do not walk the content
                    break;
                default:
                    AppendText(node, builder);             // Run, Hyperlink, SdtContent, … → recurse
                    break;
            }
        }
    }

    /// <summary>
    /// True when <paramref name="sdt"/> is a Word content-control checkbox; <paramref name="glyph"/> is
    /// ☑ when ticked, ☐ otherwise. State is read from <c>w14:checkbox/w14:checked/@w14:val</c> by local
    /// name so it works whether or not the SDK materialised the strongly-typed element.
    /// </summary>
    private static bool TryReadCheckbox(SdtElement sdt, out string glyph)
    {
        glyph = string.Empty;
        var checkbox = sdt.SdtProperties?.Descendants()
            .FirstOrDefault(e => e.LocalName == "checkbox");
        if (checkbox is null)
            return false;

        var checkedEl = checkbox.ChildElements.FirstOrDefault(e => e.LocalName == "checked");
        if (checkedEl is null)
        {
            glyph = "☐";                                   // checkbox present but no state → unticked
            return true;
        }

        var val = checkedEl.GetAttributes().FirstOrDefault(a => a.LocalName == "val").Value;
        var isChecked = val is null or "1" or "true" or "on";   // absent val on an OnOff element = true
        glyph = isChecked ? "☑" : "☐";
        return true;
    }

    /// <summary>Maps a Wingdings/Webdings symbol run to a ballot glyph, or "" for anything else.</summary>
    private static string SymbolGlyph(SymbolChar sym)
    {
        var font = sym.Font?.Value ?? string.Empty;
        var hex = sym.Char?.Value ?? string.Empty;          // e.g. "F0FE"
        if (hex.Length < 2
            || (!font.Contains("Wingdings", StringComparison.OrdinalIgnoreCase)
                && !font.Contains("Webdings", StringComparison.OrdinalIgnoreCase)))
            return string.Empty;

        return hex[^2..].ToUpperInvariant() switch
        {
            "FE" or "FD" or "FC" or "FB" => "☑",            // checked box / crossed box / check / X
            "6F" or "A8" or "A9" or "A7" => "☐",            // empty ballot boxes
            _ => string.Empty,
        };
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadTable(Table table) =>
        table.Elements<TableRow>()
            .Select(row => (IReadOnlyList<string>)row.Elements<TableCell>().Select(CellText).ToList())
            .ToList();

    /// <summary>
    /// Prefers the tables sitting between the "students" heading and the "content" heading. Falls
    /// back to every table in the document when the headings cannot be located, mirroring how the
    /// PDF reader falls back to the whole page — the "Student N" row label is selective enough.
    /// </summary>
    private static IEnumerable<Table> SelectStudentTables(Body body)
    {
        var elements = body.ChildElements.ToList();

        var start = elements.FindIndex(e =>
            e is Paragraph && ContainsHeading(e.InnerText, RegisterFormHeadings.StudentSection));

        if (start < 0)
            return body.Descendants<Table>();

        var end = elements.FindIndex(start + 1, e =>
            e is Paragraph && ContainsHeading(e.InnerText, RegisterFormHeadings.ContentSection));

        if (end < 0)
            end = elements.Count;

        // Bounded to the student section, so the supervisor table in section 1 — which also carries
        // an e-mail — can never contribute rows.
        return elements.GetRange(start, end - start).OfType<Table>();
    }

    private static bool ContainsHeading(string text, string heading) =>
        text.Contains(heading, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Flattens a cell to plain text. A cell holds paragraphs of runs, and Word splits a run
    /// wherever formatting changes, so "LT000033" can arrive as several <see cref="Text"/> nodes.
    /// </summary>
    private static string CellText(TableCell cell)
    {
        var text = string.Concat(cell.Descendants<Text>().Select(t => t.Text));
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
