namespace TEDF.Application.Common.Models
{
    public record FileAttachmentDto(
        string FileName,
        string FilePath,
        long FileSize,
        string ContentType);
}
