namespace TEDF.Application.Common.Interfaces
{
    public interface IFileStorageService
    {
        Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string folder, CancellationToken ct = default);
        Task<FileUploadResult> MoveAsync(string sourceFilePath, string destinationFolder, CancellationToken ct = default);
        Task<Stream?> DownloadAsync(string filePath, CancellationToken ct = default);
        Task<bool> DeleteAsync(string filePath, CancellationToken ct = default);
        Task<bool> ExistsAsync(string filePath, CancellationToken ct = default);
        string GetPublicUrl(string filePath);
        Task<FileInfo?> GetFileInfoAsync(string filePath, CancellationToken ct = default);
    }
}
