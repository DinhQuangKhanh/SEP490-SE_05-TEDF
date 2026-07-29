using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.DTOs;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;
using TEDF.Domain.Enums.Group;
using TEDF.Domain.Enums.Mentor;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// EF Core implementation of topic pool container read queries.
/// Handles pool-level queries only (pool metadata, statistics, department grouping).
/// For individual topic queries, see <see cref="TopicQueryService"/>.
/// </summary>
public class TopicPoolsQueryService : ITopicPoolsQueryService
{
    private readonly AppDbContext _context;

    public TopicPoolsQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TopicPoolDto>> GetTopicPoolsAsync(int? majorId, CancellationToken cancellationToken = default)
    {
        var query = _context.TopicPools.AsNoTracking();

        if (majorId.HasValue)
        {
            query = query.Where(tp => tp.MajorId == majorId.Value);
        }

        return await query
            .OrderBy(tp => tp.MajorId)
            .Select(tp => new TopicPoolDto
            {
                Id = tp.Id,
                Code = tp.Code,
                Name = tp.Name,
                Description = tp.Description,
                MajorId = tp.MajorId,
                Status = tp.Status,
                MaxActiveTopicsPerMentor = tp.MaxActiveTopicsPerMentor,
                ExpirationSemesters = tp.ExpirationSemesters,
                CreatedAt = tp.CreatedAt,
                UpdatedAt = tp.UpdatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DepartmentWithPoolsDto>> GetPoolsByDepartmentAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Code,
                d.Name,
                Majors = _context.Majors
                    .Where(m => m.DepartmentId == d.Id && m.IsActive)
                    .OrderBy(m => m.Name)
                    .Select(m => new
                    {
                        m.Id,
                        m.Code,
                        m.Name,
                        Pool = _context.TopicPools
                            .Where(tp => tp.MajorId == m.Id)
                            .OrderByDescending(tp => tp.UpdatedAt ?? tp.CreatedAt)
                            .Select(tp => new
                            {
                                tp.Id,
                                tp.Code,
                                tp.Name,
                                tp.Status,
                                TotalTopics = _context.Projects.Count(p =>
                                    p.TopicPoolId == tp.Id &&
                                    p.SourceType == ProjectSourceType.FromPool)
                            })
                            .FirstOrDefault()
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return departments.Select(d => new DepartmentWithPoolsDto
        {
            DepartmentId = d.Id,
            DepartmentCode = d.Code,
            DepartmentName = d.Name,
            Majors = d.Majors.Select(m => new MajorWithPoolDto
            {
                MajorId = m.Id,
                MajorCode = m.Code,
                MajorName = m.Name,
                Pool = m.Pool is null
                    ? null
                    : new TopicPoolSummaryDto
                    {
                        Id = m.Pool.Id,
                        Code = m.Pool.Code,
                        Name = m.Pool.Name,
                        StatusName = m.Pool.Status.ToString(),
                        TotalTopics = m.Pool.TotalTopics,
                    }
            }).ToList()
        }).ToList();
    }

    public async Task<TopicPoolDto?> GetTopicPoolByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TopicPools
            .AsNoTracking()
            .Where(tp => tp.Id == id)
            .Select(tp => new TopicPoolDto
            {
                Id = tp.Id,
                Code = tp.Code,
                Name = tp.Name,
                Description = tp.Description,
                MajorId = tp.MajorId,
                Status = tp.Status,
                MaxActiveTopicsPerMentor = tp.MaxActiveTopicsPerMentor,
                ExpirationSemesters = tp.ExpirationSemesters,
                CreatedAt = tp.CreatedAt,
                UpdatedAt = tp.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TopicPoolStatisticsDto> GetTopicPoolStatisticsAsync(Guid poolId, CancellationToken cancellationToken = default)
    {
        // Get pool info
        var pool = await _context.TopicPools
            .AsNoTracking()
            .Where(tp => tp.Id == poolId)
            .Select(tp => new { tp.Id, tp.Code, tp.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (pool is null)
        {
            return new TopicPoolStatisticsDto
            {
                PoolId = poolId,
                PoolCode = string.Empty,
                PoolName = string.Empty,
            };
        }

        // Query all projects in this pool
        var poolProjects = _context.Projects
            .AsNoTracking()
            .Where(p => p.TopicPoolId == poolId && p.SourceType == ProjectSourceType.FromPool);

        var statusCounts = await poolProjects
            .Where(p => p.PoolStatus.HasValue)
            .GroupBy(p => p.PoolStatus!.Value)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        var totalTopics = statusCounts.Values.Sum();
        var activeTopics = statusCounts.GetValueOrDefault(PoolTopicStatus.Available);
        var registeredTopics = statusCounts.GetValueOrDefault(PoolTopicStatus.Reserved)
                             + statusCounts.GetValueOrDefault(PoolTopicStatus.Assigned);
        var expiredTopics = statusCounts.GetValueOrDefault(PoolTopicStatus.Expired);

        var totalMentors = await poolProjects
            .SelectMany(p => p.Mentors.Where(m => m.Status == ProjectMentorStatus.Active))
            .Select(m => m.MentorId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new TopicPoolStatisticsDto
        {
            PoolId = pool.Id,
            PoolCode = pool.Code,
            PoolName = pool.Name,
            TotalMentors = totalMentors,
            TotalTopicsCount = totalTopics,
            ActiveTopicsCount = activeTopics,
            RegisteredTopicsCount = registeredTopics,
            ExpiredTopicsCount = expiredTopics,
        };
    }

    public Task<List<GroupRegistrationDto>> GetGroupRegistrationsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default) =>
        ProjectRegistrations(_context.TopicRegistrations.AsNoTracking().Where(r => r.GroupId == groupId))
            .ToListAsync(cancellationToken);

    public Task<GroupRegistrationDto?> GetProjectRegistrationAsync(
        Guid projectId,
        CancellationToken cancellationToken = default) =>
        // The confirmed registration is the group that was assigned this topic; its Note holds the
        // registration reason + attachment URLs the group submitted. Newest-first (via the shared
        // projection) in the unlikely event of more than one confirmed row.
        ProjectRegistrations(_context.TopicRegistrations.AsNoTracking()
                .Where(r => r.ProjectId == projectId && r.Status == TopicRegistrationStatus.Confirmed))
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Shared projection of a topic registration to <see cref="GroupRegistrationDto"/> (resolved
    /// topic name/code, active mentor name, status and note), newest-first. Callers compose their
    /// own filter (by group or by project) onto <paramref name="registrations"/> before projecting.
    /// </summary>
    private IQueryable<GroupRegistrationDto> ProjectRegistrations(IQueryable<TopicRegistration> registrations) =>
        from r in registrations
        join p in _context.Projects.AsNoTracking() on r.ProjectId equals p.Id into projectJoin
        from p in projectJoin.DefaultIfEmpty()
        orderby r.RegisteredAt descending
        select new GroupRegistrationDto
        {
            Id = r.Id,
            ProjectId = r.ProjectId,
            // Project value objects are mapped as string columns (NameVi/Code).
            ProjectName = p == null ? null : EF.Property<string>(p, "NameVi"),
            ProjectCode = p == null ? null : EF.Property<string>(p, "Code"),
            MentorName = p == null
                ? null
                : (from pm in _context.ProjectMentors.AsNoTracking()
                   where pm.ProjectId == p.Id && pm.Status == ProjectMentorStatus.Active
                   join u in _context.Users.AsNoTracking() on pm.MentorId equals u.Id
                   select u.FullName).FirstOrDefault(),
            Status = r.Status.ToString(),
            RegisteredAt = r.RegisteredAt,
            Note = r.Note,
            RejectReason = r.RejectReason,
        };

    public async Task<List<MentorRegistrationRequestDto>> GetMentorRegistrationsAsync(
        Guid mentorId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from r in _context.TopicRegistrations.AsNoTracking()
            where r.Status == TopicRegistrationStatus.Pending
            join pm in _context.ProjectMentors.AsNoTracking() on r.ProjectId equals pm.ProjectId
            where pm.MentorId == mentorId && pm.Status == ProjectMentorStatus.Active
            join p in _context.Projects.AsNoTracking() on r.ProjectId equals p.Id
            join g in _context.Groups.AsNoTracking() on r.GroupId equals g.Id
            join u in _context.Users.AsNoTracking() on r.RegisteredBy equals u.Id
            orderby r.RegisteredAt descending
            select new MentorRegistrationRequestDto
            {
                RegistrationId = r.Id,
                ProjectId = r.ProjectId,
                ProjectName = EF.Property<string>(p, "NameVi"),
                ProjectCode = EF.Property<string>(p, "Code"),
                GroupId = r.GroupId,
                GroupName = g.Name,
                GroupCode = EF.Property<string>(g, "Code"),
                RegisteredByName = u.FullName,
                MemberCount = _context.GroupMembers.Count(m => m.GroupId == r.GroupId && m.Status == GroupMemberStatus.Active),
                Note = r.Note,
                RegisteredAt = r.RegisteredAt,
            }
        ).ToListAsync(cancellationToken);
    }

}
