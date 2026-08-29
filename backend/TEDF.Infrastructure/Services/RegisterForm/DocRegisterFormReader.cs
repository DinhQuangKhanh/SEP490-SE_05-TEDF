using NPOI.XWPF.Extractor;
using NPOI.XWPF.UserModel;

namespace TEDF.Infrastructure.Services.RegisterForm;

/// <summary>
/// Reads a legacy binary <c>.doc</c> (Word 97-2003, an OLE2 file) with NPOI's HWPF text extractor.
/// <para>
/// The extractor flattens the document to text, so like the PDF path this reader returns visual
/// lines only (<see cref="RegisterFormDoc.Tables"/> stays empty) and the content extractor recovers
/// the supervisor / student rows from the lines. <c>.doc</c> is a legacy fallback — the source
/// <c>.docx</c> (walked cell-by-cell by <see cref="DocxRegisterFormReader"/>) is the precise path.
/// </para>
/// </summary>
internal static class DocRegisterFormReader
{
    // HWPF marks paragraph / cell / row ends with control chars: CR, LF, vertical tab (0x0B, a soft
    // break) and BEL (0x07, cell/row marker). Splitting on all keeps each logical line separate.
    private static readonly char[] LineBreaks = ['\r', '\n', (char)0x0B, (char)0x07];

    public static RegisterFormDoc ReadDocument(Stream stream)
    {
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);

        var document = new XWPFDocument(stream);
        var text = new XWPFWordExtractor(document).Text ?? string.Empty;

        var lines = new List<string>();
        foreach (var raw in text.Split(LineBreaks))
        {
            var line = string.Join(' ', raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (line.Length > 0)
                lines.Add(line);
        }

        return new RegisterFormDoc(lines, []);
    }
}
