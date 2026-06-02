using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Enums.Document;
using TEDF.Persistence.SqlServer;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation
{
    public sealed class ProjectDocumentWriteService : IProjectDocumentWriteService
    {
        private readonly AppDbContext _context;

        public ProjectDocumentWriteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> InsertDocumentAsync(
            Guid projectId,
            string fileName,
            string originalFileName,
            string fileType,
            long fileSize,
            string filePath,
            DocumentType documentType,
            Guid uploadedBy,
            CancellationToken cancellationToken = default)
        {
            var exists = await _context.Documents
                .AsNoTracking()
                .AnyAsync(d => d.FilePath == filePath, cancellationToken);

            if (exists) return true;

            var now = DateTime.UtcNow;
            var docId = Guid.NewGuid();

            var rows = await _context.Database.ExecuteSqlAsync(
                $"""
                 INSERT INTO Documents
                     (Id, ProjectId, FileName, OriginalFileName, FileType, FileSize,
                      FilePath, DocumentType, [Version], UploadedBy, UploadedAt, IsDeleted, [Description])
                 VALUES
                     ({docId}, {projectId}, {fileName}, {originalFileName}, {fileType}, {fileSize},
                      {filePath}, {(int)documentType}, {"1.0"}, {uploadedBy}, {now}, {false}, {(string?)null});

                 UPDATE Projects SET UpdatedAt = {now} WHERE Id = {projectId};
                 """,
                cancellationToken);

            return rows > 0;
        }
    }
}
