using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Entities
{
    public class ProjectArchive : Entity<Guid>
    {
        public string ProjectName { get; private set; } = string.Empty;
        public string StudentNames { get; private set; } = string.Empty;
        public int MajorId { get; private set; }
        public string AcademicYear { get; private set; } = string.Empty;
        public string? Summary { get; private set; }
        public string? DocumentUrl { get; private set; }
        public string? Tags { get; private set; }
        public long? FileSizeBytes { get; private set; }
        public int ViewCount { get; private set; }
        public int DownloadCount { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private ProjectArchive() { }

        public static ProjectArchive Create(
            string projectName,
            string studentNames,
            int majorId,
            string academicYear,
            string? summary = null,
            string? documentUrl = null,
            string? tags = null,
            long? fileSizeBytes = null)
        {
            return new ProjectArchive
            {
                Id = Guid.NewGuid(),
                ProjectName = projectName,
                StudentNames = studentNames,
                MajorId = majorId,
                AcademicYear = academicYear,
                Summary = summary,
                DocumentUrl = documentUrl,
                Tags = tags,
                FileSizeBytes = fileSizeBytes,
                ViewCount = 0,
                DownloadCount = 0,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void IncrementViewCount() => ViewCount++;
        public void IncrementDownloadCount() => DownloadCount++;

        public void Update(
            string? summary = null,
            string? documentUrl = null,
            string? tags = null)
        {
            if (summary != null) Summary = summary;
            if (documentUrl != null) DocumentUrl = documentUrl;
            if (tags != null) Tags = tags;
        }
    }
}
