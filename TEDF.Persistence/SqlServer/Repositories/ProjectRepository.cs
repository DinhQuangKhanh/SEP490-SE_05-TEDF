using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Enums.Document;
using TEDF.Domain.Enums.Mentor;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Enums.TopicPool;
using TEDF.Domain.Specifications.Projects;
using TEDF.Domain.Specifications.TopicPools;
using TEDF.Persistence.Common;

namespace TEDF.Persistence.SqlServer.Repositories
{
    /// <summary>
    /// Repository implementation for Project aggregate using specifications.
    /// </summary>
    public class ProjectRepository : BaseRepository<Project, Guid>, IProjectRepository
    {
        public ProjectRepository(AppDbContext context) : base(context) { }

        public async Task<Project?> GetByCodeAsync(ProjectCode code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
        }

        public async Task<Project?> GetWithMentorsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.Mentors)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Project?> GetWithDocumentsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Project?> GetWithAllAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.Mentors)
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Project>> GetByMentorIdAsync(Guid mentorId, CancellationToken cancellationToken = default)
        {
            var spec = new ProjectByMentorSpec(mentorId);
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<IEnumerable<Project>> GetBySemesterIdAsync(int semesterId, CancellationToken cancellationToken = default)
        {
            var spec = new ProjectBySemesterSpec(semesterId, includeDetails: true);
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status, CancellationToken cancellationToken = default)
        {
            var spec = new ProjectByStatusSpec(status);
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<IEnumerable<Project>> GetPendingEvaluationAsync(CancellationToken cancellationToken = default)
        {
            var spec = new ProjectPendingEvaluationSpec();
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<Project?> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            var spec = new ProjectByGroupSpec(groupId);
            return await FirstOrDefaultAsync(spec, cancellationToken);
        }

        public async Task<IEnumerable<Project>> GetByMajorIdAsync(int majorId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.MajorId == majorId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsCodeAsync(ProjectCode code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(p => p.Code == code, cancellationToken);
        }

        public async Task<int> GetNextSequenceAsync(int year, CancellationToken cancellationToken = default)
        {
            var prefix = $"PROJ-{year}-";
            var lastCode = await _dbSet
                .Where(p => p.CreatedAt.Year == year)
                .OrderByDescending(p => p.Code)
                .Select(p => p.Code)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastCode == null) return 1;

            var sequencePart = lastCode.Value.Replace(prefix, "");
            return int.TryParse(sequencePart, out var seq) ? seq + 1 : 1;
        }

        public async Task<Dictionary<ProjectStatus, int>> GetStatusCountBySemesterAsync(int semesterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.SemesterId == semesterId)
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        }

        /// <summary>
        /// Gets projects that need modification using specification.
        /// </summary>
        public async Task<IEnumerable<Project>> GetNeedsModificationAsync(Guid? mentorId = null, CancellationToken cancellationToken = default)
        {
            var spec = new ProjectNeedsModificationSpec(mentorId);
            return await ListAsync(spec, cancellationToken);
        }

        /// <summary>
        /// Gets projects pending evaluation for a semester using specification.
        /// </summary>
        public async Task<IEnumerable<Project>> GetPendingEvaluationBySemesterAsync(int semesterId, CancellationToken cancellationToken = default)
        {
            var spec = new ProjectPendingEvaluationSpec(semesterId);
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<Dictionary<ProjectSourceType, int>> GetSourceTypeCountBySemesterAsync(int semesterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.SemesterId == semesterId)
                .GroupBy(p => p.SourceType)
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Source, x => x.Count, cancellationToken);
        }

        public async Task<int> CountActivePoolTopicsByMentorAsync(Guid topicPoolId, Guid mentorId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.TopicPoolId == topicPoolId &&
                           p.SourceType == ProjectSourceType.FromPool &&
                           (p.PoolStatus == PoolTopicStatus.Available || p.PoolStatus == PoolTopicStatus.Reserved) &&
                           p.Mentors.Any(m => m.MentorId == mentorId && m.Status == ProjectMentorStatus.Active))
                .CountAsync(cancellationToken);
        }

        public async Task<Dictionary<PoolTopicStatus, int>> GetPoolStatusCountsAsync(Guid topicPoolId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.TopicPoolId == topicPoolId && p.SourceType == ProjectSourceType.FromPool && p.PoolStatus.HasValue)
                .GroupBy(p => p.PoolStatus!.Value)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        }

        public async Task<List<Guid>> GetPoolProjectIdsAsync(Guid topicPoolId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.TopicPoolId == topicPoolId && p.SourceType == ProjectSourceType.FromPool)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<int>> GetMentorTopicCountsInPoolAsync(Guid topicPoolId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.TopicPoolId == topicPoolId && p.SourceType == ProjectSourceType.FromPool)
                .SelectMany(p => p.Mentors.Where(m => m.Status == ProjectMentorStatus.Active))
                .GroupBy(m => m.MentorId)
                .Select(g => g.Count())
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Project>> GetExpirablePoolTopicsAsync(int currentSemesterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.SourceType == ProjectSourceType.FromPool &&
                           p.PoolStatus == PoolTopicStatus.Available &&
                           p.ExpirationSemesterId.HasValue &&
                           p.ExpirationSemesterId.Value < currentSemesterId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Project>> GetPoolTopicsMissingExpirationAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.SourceType == ProjectSourceType.FromPool &&
                           p.PoolStatus == PoolTopicStatus.Available &&
                           p.Status == ProjectStatus.Approved &&
                           p.CreatedInSemesterId.HasValue &&
                           p.TopicPoolId.HasValue &&
                           !p.ExpirationSemesterId.HasValue)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetAvailableApprovedPoolTopicIdsAsync(Guid topicPoolId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.TopicPoolId == topicPoolId &&
                           p.SourceType == ProjectSourceType.FromPool &&
                           p.PoolStatus == PoolTopicStatus.Available &&
                           p.Status == ProjectStatus.Approved)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Project>> GetExpiringPoolTopicsWithMentorsAsync(int currentSemesterId, CancellationToken cancellationToken = default)
        {
            var spec = new ExpiringTopicsInPoolSpec(currentSemesterId);
            return await _dbSet
                .Where(spec.Criteria)
                .Include(p => p.Mentors)
                .ToListAsync(cancellationToken);
        }

        public async Task CancelRejectedProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var project = await _dbSet.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
            if (project is null) return;

            // Only cancel if still in Rejected status
            if (project.Status == ProjectStatus.Rejected)
            {
                project.Cancel();
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<Project>> GetPendingEvaluationByDepartmentAsync(int departmentId, CancellationToken cancellationToken = default)
        {
            // Get major IDs that belong to this department
            var majorIds = await _context.Majors
                .Where(m => m.DepartmentId == departmentId)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            return await _dbSet
                .Include(p => p.Mentors)
                .Where(p => majorIds.Contains(p.MajorId) &&
                           p.Status == ProjectStatus.PendingEvaluation)
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountMentorActiveProjectsInSemesterAsync(Guid mentorId, int semesterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.SemesterId == semesterId &&
                           p.Status != ProjectStatus.Cancelled &&
                           p.Status != ProjectStatus.Rejected &&
                           p.Mentors.Any(m => m.MentorId == mentorId && m.Status == ProjectMentorStatus.Active))
                .CountAsync(cancellationToken);
        }

        public async Task<(IEnumerable<Project> Items, int TotalCount)> GetPagedAsync(
            string? search, int? semesterId, ProjectStatus? status, int? majorId,
            int page, int pageSize, CancellationToken ct = default)
        {
            var query = _dbSet.AsNoTracking().Include(p => p.Mentors).AsQueryable();

            if (semesterId.HasValue)
                query = query.Where(p => p.SemesterId == semesterId.Value);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (majorId.HasValue)
                query = query.Where(p => p.MajorId == majorId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p =>
                    p.NameVi.Value.Contains(term) ||
                    p.NameEn.Value.Contains(term) ||
                    p.Code.Value.Contains(term) ||
                    p.NameAbbr.Contains(term));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        /// <inheritdoc />
        public async Task<bool> InsertDocumentAsync(
            Guid projectId, string fileName, string originalFileName,
            string fileType, long fileSize, string filePath,
            DocumentType documentType, Guid uploadedBy,
            CancellationToken cancellationToken = default)
        {
            // Idempotent: skip if a Document with this file path already exists
            var exists = await _context.Documents
                .AsNoTracking()
                .AnyAsync(d => d.FilePath == filePath, cancellationToken);

            if (exists) return true;

            // Direct SQL INSERT bypasses EF's change tracker + interceptors,
            // avoiding DbUpdateConcurrencyException when parallel scan jobs
            // hit the same Project aggregate.
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
