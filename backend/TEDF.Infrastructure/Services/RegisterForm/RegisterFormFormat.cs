namespace TEDF.Infrastructure.Services.RegisterForm;

internal enum RegisterFormFormat
{
    Unknown = 0,
    Pdf = 1,
    Docx = 2,
    Doc = 3,
}

/// <summary>
/// Picks the reader from the stream's leading bytes. The parser only ever receives raw content —
/// no file name, no content type — so detection has to be based on the signature.
/// </summary>
internal static class RegisterFormFormatDetector
{
    private static readonly byte[] PdfSignature = "%PDF"u8.ToArray();

    // OOXML files are ZIP containers. This also matches .xlsx/.zip, which is fine: those fail later
    // when word/document.xml turns out to be missing, and the caller treats that as "no roster".
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    // Legacy .doc (Word 97-2003) is an OLE2 Compound File; its magic starts with D0 CF 11 E0.
    private static readonly byte[] Ole2Signature = [0xD0, 0xCF, 0x11, 0xE0];

    public static RegisterFormFormat Detect(Stream stream)
    {
        var header = new byte[4];
        var read = stream.Read(header, 0, header.Length);
        stream.Seek(0, SeekOrigin.Begin);

        if (read < header.Length)
            return RegisterFormFormat.Unknown;

        if (header.AsSpan().SequenceEqual(PdfSignature))
            return RegisterFormFormat.Pdf;

        if (header.AsSpan().SequenceEqual(ZipSignature))
            return RegisterFormFormat.Docx;

        if (header.AsSpan().SequenceEqual(Ole2Signature))
            return RegisterFormFormat.Doc;

        return RegisterFormFormat.Unknown;
    }
}
