namespace TEDF.Application.Common.Interfaces
{
    public record FileInfo(string FileName, string FilePath, long FileSize, string ContentType, DateTime UploadedAt);
}
