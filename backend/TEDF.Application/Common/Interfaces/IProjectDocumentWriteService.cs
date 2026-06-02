using TEDF.Domain.Enums.Document;

namespace TEDF.Application.Common.Interfaces
{
    public interface IProjectDocumentWriteService
    {
        Task<bool> InsertDocumentAsync(
            Guid projectId,
            string fileName,
            string originalFileName,
            string fileType,
            long fileSize,
            string filePath,
            DocumentType documentType,
            Guid uploadedBy,
            CancellationToken cancellationToken = default);
    }
}
