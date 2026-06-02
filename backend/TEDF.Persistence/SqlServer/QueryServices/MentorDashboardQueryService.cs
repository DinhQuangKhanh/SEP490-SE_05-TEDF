using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Domain.Enums.Group;
using TEDF.Domain.Enums.Mentor;
using TEDF.Domain.Enums.Project;

namespace TEDF.Persistence.SqlServer.QueryServices;

public class MentorDashboardQueryService : IMentorDashboardQueryService
{
    private readonly AppDbContext _context;

    public MentorDashboardQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MentorDashboardDto> GetDashboardAsync(
        Guid mentorId, CancellationToken cancellationToken = default)
    {
        // 1. Mentor name
        var mentorName = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == mentorId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "";

        // 2. Active semester with phases
        var now = DateTime.UtcNow;
        var activeSemester = await _context.Semesters
            .AsNoTracking()
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now, cancellationToken);

        // 3. Mentor's projects for active semester (single query for stats + recent list)
        var projectsQuery = _context.ProjectMentors.AsNoTracking()
            .Where(pm => pm.MentorId == mentorId && pm.Status == ProjectMentorStatus.Active)
            .Join(
                _context.Projects.AsNoTracking(),
                pm => pm.ProjectId,
                p => p.Id,
                (pm, p) => p);

        if (activeSemester != null)
            projectsQuery = projectsQuery.Where(p => p.SemesterId == activeSemester.Id);

        var projects = await projectsQuery
            .Select(p => new
            {
                p.Id,
                Code = p.Code.Value,
                NameVi = p.NameVi.Value,
                NameEn = p.NameEn.Value,
                p.Status,
                p.SourceType,
                p.GroupId,
                p.CreatedAt,
                p.SubmittedAt,
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        // 4. Stats derived from projects (no extra query)
        var stats = new MentorStatsDto
        {
            TotalProjects = projects.Count,
            PendingEvaluation = projects.Count(p => p.Status == ProjectStatus.PendingEvaluation),
            ApprovedProjects = projects.Count(p => p.Status == ProjectStatus.Approved),
            InProgressProjects = projects.Count(p => p.Status == ProjectStatus.InProgress),
        };

        // 5. Groups + student count (single query for all groups linked to mentor's projects)
        var groupIds = projects
            .Where(p => p.GroupId.HasValue)
            .Select(p => p.GroupId!.Value)
            .Distinct()
            .ToList();

        var groupStats = groupIds.Count > 0
            ? await _context.Groups.AsNoTracking()
                .Where(g => groupIds.Contains(g.Id))
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    ActiveMembers = g.Members.Count(m => m.Status == GroupMemberStatus.Active),
                    LeaderName = g.Members
                        .Where(m => m.Role == GroupMemberRole.Leader && m.Status == GroupMemberStatus.Active)
                        .Join(_context.Users.AsNoTracking(), m => m.StudentId, u => u.Id, (m, u) => u.FullName)
                        .FirstOrDefault(),
                })
                .ToListAsync(cancellationToken)
            : [];

        stats = stats with
        {
            TotalGroups = groupStats.Count,
            TotalStudents = groupStats.Sum(g => g.ActiveMembers),
        };

        // 6. Recent projects (top 5) with group info
        var recentProjects = projects.Take(5).Select(p =>
        {
            var group = p.GroupId.HasValue
                ? groupStats.FirstOrDefault(g => g.Id == p.GroupId.Value)
                : null;

            return new RecentProjectDto
            {
                Id = p.Id,
                Code = p.Code,
                NameVi = p.NameVi,
                NameEn = p.NameEn,
                Status = (int)p.Status,
                SourceType = (int)p.SourceType,
                GroupName = group?.Name,
                LeaderName = group?.LeaderName,
                MemberCount = group?.ActiveMembers ?? 0,
                CreatedAt = p.CreatedAt,
                SubmittedAt = p.SubmittedAt,
            };
        }).ToList();

        // 7. Semester progress
        SemesterProgressDto? semesterProgress = null;
        if (activeSemester != null)
        {
            semesterProgress = new SemesterProgressDto
            {
                SemesterName = activeSemester.Name,
                Phases = activeSemester.Phases
                    .OrderBy(p => p.Order)
                    .Select(p => new SemesterPhaseDto
                    {
                        Name = p.Name,
                        Type = (int)p.Type,
                        Status = (int)p.Status,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Order = p.Order,
                    })
                    .ToList(),
            };
        }

        return new MentorDashboardDto
        {
            MentorName = mentorName,
            Stats = stats,
            SemesterProgress = semesterProgress,
            RecentProjects = recentProjects,
        };
    }
}
