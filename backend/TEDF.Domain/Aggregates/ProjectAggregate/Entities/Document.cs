using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Document;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Entities
{
    public class Document : Entity<Guid>
    {
        /// <summary>
        /// Gets the project identifier.
        /// </summary>
        public Guid ProjectId { get; private set; }

        /// <summary>
        /// Gets the identifier of the user who uploaded the document.
        /// </summary>
        public Guid UploadedBy { get; private set; }

        /// <summary>
        /// Gets the stored file name (system-generated).
        /// </summary>
        public string FileName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the original file name.
        /// </summary>
        public string OriginalFileName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the file type/extension.
        /// </summary>
        public string FileType { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the file size in bytes.
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        /// Gets the storage path of the file.
        /// </summary>
        public string FilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the document type.
        /// </summary>
        public DocumentType DocumentType { get; private set; }

        /// <summary>
        /// Gets the document version.
        /// </summary>
        public string? Version { get; private set; }

        /// <summary>
        /// Gets the document description.
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// Gets the upload date and time.
        /// </summary>
        public DateTime UploadedAt { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the document is deleted.
        /// </summary>
        public bool IsDeleted { get; private set; }

        private Document() { }

        /// <summary>
        /// Creates a new document for a project.
        /// </summary>
        internal static Document Create(
            Guid projectId,
            string fileName,
            string originalFileName,
            string fileType,
            long fileSize,
            string filePath,
            DocumentType documentType,
            Guid uploadedBy)
        {
            return new Document
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                FileName = fileName,
                OriginalFileName = originalFileName,
                FileType = fileType,
                FileSize = fileSize,
                FilePath = filePath,
                DocumentType = documentType,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                Version = "1.0",
                IsDeleted = false
            };
        }

        /// <summary>
        /// Updates the document version.
        /// </summary>
        /// <param name="version">The new version.</param>
        public void UpdateVersion(string version)
        {
            Version = version;
        }

        /// <summary>
        /// Sets the document description.
        /// </summary>
        /// <param name="description">The description.</param>
        public void SetDescription(string? description)
        {
            Description = description;
        }

        /// <summary>
        /// Marks the document as deleted.
        /// </summary>
        public void Delete()
        {
            IsDeleted = true;
        }

        /// <summary>
        /// Restores a deleted document.
        /// </summary>
        public void Restore()
        {
            IsDeleted = false;
        }
    }
}
