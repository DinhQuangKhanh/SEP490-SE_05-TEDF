using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Topics.DTOs;
using TEDF.Domain.Aggregates.ProjectAggregate.Entities;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Mentor;
using TEDF.Domain.Enums.Project;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// EF Core implementation of thesis topic read queries.
/// Covers all topics regardless of source type (FromPool or DirectRegistration).
/// </summary>
public class TopicsQueryService : ITopicsQueryService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public TopicsQueryService(AppDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<GetTopicsInPoolResult> GetTopicsInPoolAsync(
        int? majorId, string? search, int? poolStatus, string? sortBy,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Base query: only FromPool topics that have PASSED evaluation. A pool topic gets
        // PoolStatus = Available at proposal time (before it is reviewed), so filtering on PoolStatus
        // alone would leak PendingEvaluation/Draft/NeedsModification/Rejected topics into the student
        // browse — only Approved topics belong in the pool (registration already enforces this too).
        var query = from p in _context.Projects.AsNoTracking()
                    where p.SourceType == ProjectSourceType.FromPool
                          && p.Status == ProjectStatus.Approved
                    join m in _context.Set<Major>() on p.MajorId equals m.Id
                    select new { Project = p, MajorName = m.Name, MajorCode = m.Code };

        if (majorId.HasValue)
            query = query.Where(x => x.Project.MajorId == majorId.Value);

        if (poolStatus.HasValue)
        {
            var status = (PoolTopicStatus)poolStatus.Value;
            query = query.Where(x => x.Project.PoolStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // NameVi/NameEn are value objects (value converter) — EF cannot translate string ops on
            // them. Materialize the names via .Value (a projection that DOES translate) and match
            // client-side; mentor names match on the plain FullName column.
            var lower = search.Trim().ToLower();
            var nameRows = await query
                .Select(x => new { x.Project.Id, NameVi = x.Project.NameVi.Value, NameEn = x.Project.NameEn.Value })
                .ToListAsync(cancellationToken);
            var matchedIds = nameRows
                .Where(n => (n.NameVi ?? "").ToLower().Contains(lower) || (n.NameEn ?? "").ToLower().Contains(lower))
                .Select(n => n.Id)
                .ToHashSet();

            var matchedMentorIds = await _context.Users.AsNoTracking()
                .Where(u => u.FullName.ToLower().Contains(lower))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(x =>
                matchedIds.Contains(x.Project.Id) ||
                x.Project.Mentors.Any(pm => pm.Status == ProjectMentorStatus.Active && matchedMentorIds.Contains(pm.MentorId)));
        }

        query = sortBy switch
        {
            "name" => query.OrderBy(x => x.Project.NameVi),
            "mentor" => query.OrderBy(x =>
                x.Project.Mentors
                    .Where(pm => pm.Status == ProjectMentorStatus.Active)
                    .Join(_context.Users, pm => pm.MentorId, u => u.Id, (pm, u) => u.FullName)
                    .FirstOrDefault() ?? ""),
            _ => query.OrderByDescending(x => x.Project.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // 2-step projection: anonymous first to avoid (int) cast on nvarchar PoolStatus in SQL
        var rawItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Project.Id,
                Code = x.Project.Code.Value,
                NameVi = x.Project.NameVi.Value,
                NameEn = x.Project.NameEn.Value,
                x.Project.Description,
                Technologies = x.Project.Technologies != null ? x.Project.Technologies.Value : null,
                x.Project.MajorId,
                x.MajorName,
                x.MajorCode,
                x.Project.PoolStatus,
                x.Project.MaxStudents,
                MentorName = x.Project.Mentors
                    .Where(pm => pm.Status == ProjectMentorStatus.Active)
                    .Join(_context.Users, pm => pm.MentorId, u => u.Id, (pm, u) => u.FullName)
                    .FirstOrDefault() ?? "Chưa có mentor",
                MentorId = x.Project.Mentors
                    .Where(pm => pm.Status == ProjectMentorStatus.Active)
                    .Select(pm => pm.MentorId)
                    .FirstOrDefault(),
                x.Project.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var items = rawItems.Select(x => new TopicInPoolItemDto
        {
            Id = x.Id,
            Code = x.Code,
            NameVi = x.NameVi,
            NameEn = x.NameEn,
            Description = x.Description,
            Technologies = x.Technologies,
            MajorId = x.MajorId,
            MajorName = x.MajorName,
            MajorCode = x.MajorCode,
            PoolStatus = x.PoolStatus.HasValue ? (int)x.PoolStatus.Value : 0,
            PoolStatusName = x.PoolStatus.HasValue ? x.PoolStatus.Value.ToString() : "Unknown",
            MaxStudents = x.MaxStudents,
            MentorName = x.MentorName,
            MentorId = x.MentorId,
            CreatedAt = x.CreatedAt,
        }).ToList();

        return new GetTopicsInPoolResult(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<TopicDetailDto?> GetTopicDetailAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        // No SourceType filter — works for both FromPool and DirectRegistration topics
        // 2-step projection to avoid (int) cast on nvarchar PoolStatus in SQL
        var raw = await (
            from p in _context.Projects.AsNoTracking()
            where p.Id == topicId
            join m in _context.Set<Major>() on p.MajorId equals m.Id
            select new
            {
                p.Id,
                Code = p.Code.Value,
                NameVi = p.NameVi.Value,
                NameEn = p.NameEn.Value,
                p.NameAbbr,
                p.Description,
                p.Objectives,
                p.Scope,
                Technologies = p.Technologies != null ? p.Technologies.Value : null,
                p.ExpectedResults,
                p.MajorId,
                MajorName = m.Name,
                MajorCode = m.Code,
                p.PoolStatus,
                p.MaxStudents,
                Mentors = p.Mentors
                    .Where(pm => pm.Status == ProjectMentorStatus.Active)
                    .Join(_context.Users, pm => pm.MentorId, u => u.Id, (pm, u) => new MentorSummaryDto
                    {
                        MentorId = pm.MentorId,
                        FullName = u.FullName,
                    })
                    .ToList(),
                p.MentorFeedback,
                p.CreatedAt,
                p.UpdatedAt,
            }
        ).FirstOrDefaultAsync(cancellationToken);

        if (raw is null) return null;

        return new TopicDetailDto
        {
            Id = raw.Id,
            Code = raw.Code,
            NameVi = raw.NameVi,
            NameEn = raw.NameEn,
            NameAbbr = raw.NameAbbr,
            Description = raw.Description,
            Objectives = raw.Objectives,
            Scope = raw.Scope,
            Technologies = raw.Technologies,
            ExpectedResults = raw.ExpectedResults,
            MajorId = raw.MajorId,
            MajorName = raw.MajorName,
            MajorCode = raw.MajorCode,
            PoolStatus = raw.PoolStatus.HasValue ? (int)raw.PoolStatus.Value : 0,
            PoolStatusName = raw.PoolStatus.HasValue ? raw.PoolStatus.Value.ToString() : "Unknown",
            MaxStudents = raw.MaxStudents,
            Mentors = raw.Mentors,
            MentorFeedback = raw.MentorFeedback,
            CreatedAt = raw.CreatedAt,
            UpdatedAt = raw.UpdatedAt,
        };
    }

    public async Task<List<TopicDocumentDto>> GetTopicDocumentsAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.Set<Document>()
            .AsNoTracking()
            .Where(d => d.ProjectId == topicId && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .Join(_context.Users, d => d.UploadedBy, u => u.Id, (d, u) => new
            {
                d.Id,
                d.FileName,
                d.OriginalFileName,
                d.FileType,
                d.FileSize,
                d.FilePath,
                d.DocumentType,
                d.Description,
                d.UploadedAt,
                UploadedByName = u.FullName,
            })
            .ToListAsync(cancellationToken);

        // The public URL is built from the storage path, so it cannot be part of the SQL projection.
        return rows.Select(d => new TopicDocumentDto
        {
            Id = d.Id,
            FileName = d.FileName,
            OriginalFileName = d.OriginalFileName,
            FileUrl = string.IsNullOrWhiteSpace(d.FilePath) ? string.Empty : _fileStorage.GetPublicUrl(d.FilePath),
            FileType = d.FileType,
            FileSize = d.FileSize,
            DocumentType = d.DocumentType.ToString(),
            Description = d.Description,
            UploadedAt = d.UploadedAt,
            UploadedByName = d.UploadedByName,
        }).ToList();
    }

    public async Task<GetMentorTopicsResult> GetMentorTopicsAsync(
        Guid mentorId, int? semesterId, string? search,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = from p in _context.Projects.AsNoTracking()
                    where p.Mentors.Any(pm => pm.MentorId == mentorId && pm.Status == ProjectMentorStatus.Active)
                    join m in _context.Set<Major>() on p.MajorId equals m.Id
                    join s in _context.Semesters.AsNoTracking() on p.SemesterId equals s.Id
                    select new { Project = p, MajorName = m.Name, SemesterName = s.Name };

        // SemesterId is the semester the topic runs in for every source type, so no per-source
        // shifting: pool topics are stamped with their target semester at proposal time.
        if (semesterId.HasValue)
        {
            var selectedSemesterId = semesterId.Value;
            query = query.Where(x => x.Project.SemesterId == selectedSemesterId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // NameVi/NameEn/Code are value objects (value converter) — EF can't translate string ops
            // on them. Materialize via .Value (translatable) and match client-side, then filter by id.
            var lower = search.Trim().ToLower();
            var nameRows = await query
                .Select(x => new { x.Project.Id, NameVi = x.Project.NameVi.Value, NameEn = x.Project.NameEn.Value, Code = x.Project.Code.Value })
                .ToListAsync(cancellationToken);
            var matchedIds = nameRows
                .Where(n => (n.NameVi ?? "").ToLower().Contains(lower)
                         || (n.NameEn ?? "").ToLower().Contains(lower)
                         || (n.Code ?? "").ToLower().Contains(lower))
                .Select(n => n.Id)
                .ToHashSet();

            query = query.Where(x => matchedIds.Contains(x.Project.Id));
        }

        query = query.OrderByDescending(x => x.Project.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var rawItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Project.Id,
                Code = x.Project.Code.Value,
                NameVi = x.Project.NameVi.Value,
                NameEn = x.Project.NameEn.Value,
                x.MajorName,
                x.Project.SourceType,
                x.Project.Status,
                x.Project.SubmittedAt,
                x.Project.CreatedAt,
                x.SemesterName,
            })
            .ToListAsync(cancellationToken);

        var items = rawItems.Select(x => new MentorTopicItemDto
        {
            Id = x.Id,
            Code = x.Code,
            NameVi = x.NameVi,
            NameEn = x.NameEn,
            MajorName = x.MajorName,
            SourceType = (int)x.SourceType,
            SourceTypeName = x.SourceType.ToString(),
            Status = (int)x.Status,
            StatusName = x.Status.ToString(),
            SubmittedAt = x.SubmittedAt,
            CreatedAt = x.CreatedAt,
            SemesterName = x.SemesterName,
        }).ToList();

        return new GetMentorTopicsResult(items, totalCount, page, pageSize, totalPages);
    }
}
