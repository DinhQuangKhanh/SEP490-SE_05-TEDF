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

    public TopicsQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GetTopicsInPoolResult> GetTopicsInPoolAsync(
        int? majorId, string? search, int? poolStatus, string? sortBy,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Base query: only FromPool topics with major info
        var query = from p in _context.Projects.AsNoTracking()
                    where p.SourceType == ProjectSourceType.FromPool
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
            var term = search.Trim();
            query = query.Where(x =>
                EF.Property<string>(x.Project, "NameVi").Contains(term) ||
                EF.Property<string>(x.Project, "NameEn").Contains(term) ||
                x.Project.Mentors.Any(pm =>
                    pm.Status == ProjectMentorStatus.Active &&
                    _context.Users.Any(u => u.Id == pm.MentorId && u.FullName.Contains(term))));
        }

        query = sortBy switch
        {
            "name" => query.OrderBy(x => EF.Property<string>(x.Project, "NameVi")),
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
            CreatedAt = raw.CreatedAt,
            UpdatedAt = raw.UpdatedAt,
        };
    }

    public async Task<List<TopicDocumentDto>> GetTopicDocumentsAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Document>()
            .AsNoTracking()
            .Where(d => d.ProjectId == topicId && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .Join(_context.Users, d => d.UploadedBy, u => u.Id, (d, u) => new TopicDocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                OriginalFileName = d.OriginalFileName,
                FileType = d.FileType,
                FileSize = d.FileSize,
                DocumentType = d.DocumentType.ToString(),
                Description = d.Description,
                UploadedAt = d.UploadedAt,
                UploadedByName = u.FullName,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GetMentorTopicsResult> GetMentorTopicsAsync(
        Guid mentorId, int? semesterId, string? search,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // For selected semester N:
        // - Pool topics are from previous semester N-1
        // - Direct-registration topics are in semester N
        int? previousSemesterId = null;
        if (semesterId.HasValue)
        {
            var selectedSemester = await _context.Semesters.AsNoTracking()
                .Where(s => s.Id == semesterId.Value)
                .Select(s => new { s.Id, s.StartDate })
                .FirstOrDefaultAsync(cancellationToken);

            if (selectedSemester != null)
            {
                var previousSemester = await _context.Semesters.AsNoTracking()
                    .Where(s => s.StartDate < selectedSemester.StartDate)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                // If previous semester found (non-default int), keep it for pool-topic filtering
                if (previousSemester != 0)
                    previousSemesterId = previousSemester;
            }
        }

        var query = from p in _context.Projects.AsNoTracking()
                    where p.Mentors.Any(pm => pm.MentorId == mentorId && pm.Status == ProjectMentorStatus.Active)
                    join m in _context.Set<Major>() on p.MajorId equals m.Id
                    join s in _context.Semesters.AsNoTracking() on p.SemesterId equals s.Id
                    select new { Project = p, MajorName = m.Name, SemesterName = s.Name };

        if (semesterId.HasValue)
        {
            var selectedSemesterId = semesterId.Value;
            var targetPoolSemesterId = previousSemesterId;

            query = query.Where(x =>
                (x.Project.SourceType == ProjectSourceType.DirectRegistration && x.Project.SemesterId == selectedSemesterId) ||
                (x.Project.SourceType == ProjectSourceType.FromPool && targetPoolSemesterId.HasValue && x.Project.SemesterId == targetPoolSemesterId.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                EF.Property<string>(x.Project, "NameVi").Contains(term) ||
                EF.Property<string>(x.Project, "NameEn").Contains(term) ||
                EF.Property<string>(x.Project, "Code").Contains(term));
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
