namespace TEDF.Application.Common.Interfaces
{
    public record FileUploadResult(bool Success, string? FilePath, string? PublicUrl, string? Error);
}
