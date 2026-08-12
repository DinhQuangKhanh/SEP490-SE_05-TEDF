namespace TEDF.Infrastructure.Services.RegisterForm;

internal enum RegisterFormFormat
{
    Unknown = 0,
    Pdf = 1,
    Docx = 2,
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

        return RegisterFormFormat.Unknown;
    }
}
