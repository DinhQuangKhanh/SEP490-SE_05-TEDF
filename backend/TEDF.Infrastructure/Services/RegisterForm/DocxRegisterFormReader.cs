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
