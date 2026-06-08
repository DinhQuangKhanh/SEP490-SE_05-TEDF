using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Constants;
using TEDF.Domain.Enums.Group;
using TEDF.Domain.Enums.Mentor;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// Read-side service for the Dashboard feature — consolidates the admin, mentor, department-head and
/// evaluator dashboards. See <see cref="IDashboardQueryService"/>.
/// </summary>
public class DashboardQueryService : IDashboardQueryService
{
    private readonly AppDbContext _context;
    private readonly IEvaluationsQueryService _evaluations;

    public DashboardQueryService(AppDbContext context, IEvaluationsQueryService evaluations)
    {
        _context = context;
        _evaluations = evaluations;
    }

    // The evaluator dashboard is evaluator-centric data owned by the Evaluations read service.
    public Task<EvaluatorDashboardDto> GetEvaluatorDashboardAsync(Guid evaluatorId, CancellationToken cancellationToken = default)
        => _evaluations.GetDashboardAsync(evaluatorId, cancellationToken);

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        var totalStudents = await _context.UserRoles.AsNoTracking()
            .CountAsync(r => r.RoleName == "Student" && r.IsActive, cancellationToken);

        var totalMentors = await _context.UserRoles.AsNoTracking()
            .CountAsync(r => r.RoleName == "Mentor" && r.IsActive, cancellationToken);

        var now = DateTime.UtcNow;
        var activeSemester = await _context.Semesters.AsNoTracking()
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now, cancellationToken);

        var totalRegisteredTopics = 0;
        var approvalRate = new ApprovalRateDto();

        if (activeSemester != null)
        {
            totalRegisteredTopics = await _context.Projects.AsNoTracking()
                .CountAsync(p => p.SemesterId == activeSemester.Id, cancellationToken);

            var statusCounts = await _context.Projects.AsNoTracking()
                .Where(p => p.SemesterId == activeSemester.Id)
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var approved = statusCounts.Where(s => s.Status == ProjectStatus.Approved).Sum(s => s.Count);
            var rejected = statusCounts.Where(s => s.Status == ProjectStatus.Rejected).Sum(s => s.Count);
            var inProgress = statusCounts.Where(s => s.Status == ProjectStatus.InProgress).Sum(s => s.Count);
            var pending = statusCounts
                .Where(s => s.Status is ProjectStatus.Draft or ProjectStatus.PendingEvaluation or ProjectStatus.NeedsModification)
                .Sum(s => s.Count);

            approvalRate = new ApprovalRateDto
            {
                Approved = approved,
                Rejected = rejected,
                InProgress = inProgress,
                Pending = pending,
                Total = totalRegisteredTopics,
            };
        }

        var highPriorityPending = await _context.SupportTickets.AsNoTracking()
            .CountAsync(t =>
                (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress) &&
                (t.Priority == TicketPriority.High || t.Priority == TicketPriority.Urgent),
                cancellationToken);

        SemesterProgressDto? semesterProgress = activeSemester != null
            ? BuildSemesterProgress(activeSemester.Name, activeSemester.Phases)
            : null;

        var recentTickets = await (
            from t in _context.SupportTickets.AsNoTracking()
            join u in _context.Users.AsNoTracking() on t.ReporterId equals u.Id
            orderby t.CreatedAt descending
            select new RecentTicketDto
            {
                Code = t.Code,
                Title = t.Title,
                ReporterName = u.FullName,
                Category = (int)t.Category,
                Priority = (int)t.Priority,
                Status = (int)t.Status,
                CreatedAt = t.CreatedAt,
            })
            .Take(5)
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto
        {
            Stats = new AdminStatsDto
            {
                TotalStudents = totalStudents,
                TotalMentors = totalMentors,
                TotalRegisteredTopics = totalRegisteredTopics,
                HighPriorityPending = highPriorityPending,
            },
            SemesterProgress = semesterProgress,
            ApprovalRate = approvalRate,
            RecentTickets = recentTickets,
        };
    }

    public async Task<MentorDashboardDto> GetMentorDashboardAsync(Guid mentorId, CancellationToken cancellationToken = default)
    {
        var mentorName = await _context.Users.AsNoTracking()
            .Where(u => u.Id == mentorId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "";

        var now = DateTime.UtcNow;
        var activeSemester = await _context.Semesters.AsNoTracking()
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now, cancellationToken);

        var projectsQuery = _context.ProjectMentors.AsNoTracking()
            .Where(pm => pm.MentorId == mentorId && pm.Status == ProjectMentorStatus.Active)
            .Join(_context.Projects.AsNoTracking(), pm => pm.ProjectId, p => p.Id, (pm, p) => p);

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

        var stats = new MentorStatsDto
        {
            TotalProjects = projects.Count,
            PendingEvaluation = projects.Count(p => p.Status == ProjectStatus.PendingEvaluation),
            ApprovedProjects = projects.Count(p => p.Status == ProjectStatus.Approved),
            InProgressProjects = projects.Count(p => p.Status == ProjectStatus.InProgress),
        };

        var groupIds = projects.Where(p => p.GroupId.HasValue).Select(p => p.GroupId!.Value).Distinct().ToList();

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

        var recentProjects = projects.Take(5).Select(p =>
        {
            var group = p.GroupId.HasValue ? groupStats.FirstOrDefault(g => g.Id == p.GroupId.Value) : null;
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

        SemesterProgressDto? semesterProgress = activeSemester != null
            ? BuildSemesterProgress(activeSemester.Name, activeSemester.Phases)
            : null;

        return new MentorDashboardDto
        {
            MentorName = mentorName,
            Stats = stats,
            SemesterProgress = semesterProgress,
            RecentProjects = recentProjects,
        };
    }

    public async Task<DepartmentHeadDashboardDto> GetDepartmentHeadDashboardAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var departmentId = await _context.Users.AsNoTracking()
            .Where(u => u.Id == currentUserId)
            .Select(u => u.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleValidationException("User is not assigned to any department.");

        var headId = currentUserId;

        var department = await _context.Departments.AsNoTracking()
            .Where(d => d.Id == departmentId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "";

        var headName = await _context.Users.AsNoTracking()
            .Where(u => u.Id == headId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "";

        var now = DateTime.UtcNow;
        var activeSemester = await _context.Semesters.AsNoTracking()
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now, cancellationToken);

        SemesterProgressDto? semesterProgress = activeSemester != null
            ? BuildSemesterProgress(activeSemester.Name, activeSemester.Phases)
            : null;

        var majorIds = await _context.Majors.AsNoTracking()
            .Where(m => m.DepartmentId == departmentId && m.IsActive)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var projects = await _context.Projects.AsNoTracking()
            .Where(p => majorIds.Contains(p.MajorId) &&
                       (p.Status == ProjectStatus.PendingEvaluation ||
                        p.Status == ProjectStatus.Approved ||
                        p.Status == ProjectStatus.NeedsModification ||
                        p.Status == ProjectStatus.Rejected))
            .Select(p => new { p.Id, p.Status })
            .ToListAsync(cancellationToken);

        var projectIds = projects.Select(p => p.Id).ToList();

        var assignments = await _context.ProjectEvaluatorAssignments.AsNoTracking()
            .Where(a => projectIds.Contains(a.ProjectId) && a.IsActive)
            .Select(a => new { a.ProjectId, a.IndividualResult, a.HasSubmittedEvaluation, a.EvaluatorId, a.AssignedAt, a.EvaluatedAt })
            .ToListAsync(cancellationToken);

        var projectStats = projects.Select(p =>
        {
            var pa = assignments.Where(a => a.ProjectId == p.Id).ToList();
            var submitted = pa.Count(a => a.HasSubmittedEvaluation);
            var distinct = pa.Where(a => a.HasSubmittedEvaluation).Select(a => a.IndividualResult).Distinct().Count();
            var hasConflict = submitted >= 2 && distinct > 1;
            var needsDecision = hasConflict && p.Status == ProjectStatus.PendingEvaluation;
            return new { p.Id, p.Status, AssignedCount = pa.Count, NeedsDecision = needsDecision };
        }).ToList();

        var stats = new DepartmentHeadStatsDto
        {
            TotalProjects = projectStats.Count,
            PendingAssignment = projectStats.Count(p => p.AssignedCount < 2 && p.Status == ProjectStatus.PendingEvaluation),
            InEvaluation = projectStats.Count(p => p.AssignedCount >= 2 && !p.NeedsDecision && p.Status == ProjectStatus.PendingEvaluation),
            NeedsFinalDecision = projectStats.Count(p => p.NeedsDecision),
            Completed = projectStats.Count(p => p.Status != ProjectStatus.PendingEvaluation),
            TotalEvaluators = await _context.Users.AsNoTracking()
                .CountAsync(u => u.DepartmentId == departmentId &&
                                 u.Roles.Any(r => r.RoleName == DomainRoleNames.Evaluator && r.IsActive), cancellationToken),
            TotalMentors = await _context.Users.AsNoTracking()
                .CountAsync(u => u.DepartmentId == departmentId &&
                                 u.Roles.Any(r => r.RoleName == DomainRoleNames.Mentor && r.IsActive), cancellationToken),
        };

        var evalProgress = new EvaluationProgressDto
        {
            Approved = projects.Count(p => p.Status == ProjectStatus.Approved),
            Rejected = projects.Count(p => p.Status == ProjectStatus.Rejected),
            NeedsModification = projects.Count(p => p.Status == ProjectStatus.NeedsModification),
            Pending = projects.Count(p => p.Status == ProjectStatus.PendingEvaluation),
        };

        var recentAssignments = assignments
            .OrderByDescending(a => a.EvaluatedAt ?? a.AssignedAt)
            .Take(10)
            .ToList();

        var activityProjectIds = recentAssignments.Select(a => a.ProjectId).Distinct().ToList();
        var activityProjects = await _context.Projects.AsNoTracking()
            .Where(p => activityProjectIds.Contains(p.Id))
            .Select(p => new { p.Id, Code = p.Code.Value, Name = p.NameVi.Value })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var activityEvaluatorIds = recentAssignments.Select(a => a.EvaluatorId).Distinct().ToList();
        var activityNames = await _context.Users.AsNoTracking()
            .Where(u => activityEvaluatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var recentActivities = recentAssignments.Select(a =>
        {
            var proj = activityProjects.GetValueOrDefault(a.ProjectId);
            var actorName = activityNames.GetValueOrDefault(a.EvaluatorId, "");
            var isSubmission = a.HasSubmittedEvaluation && a.EvaluatedAt.HasValue;

            return new RecentEvaluationActivityDto
            {
                ProjectId = a.ProjectId,
                ProjectCode = proj?.Code ?? "",
                ProjectName = proj?.Name ?? "",
                ActivityType = isSubmission ? "submitted" : "assigned",
                ActorName = actorName,
                OccurredAt = isSubmission ? a.EvaluatedAt!.Value : a.AssignedAt,
            };
        }).ToList();

        return new DepartmentHeadDashboardDto
        {
            DepartmentName = department,
            HeadName = headName,
            Stats = stats,
            SemesterProgress = semesterProgress,
            EvaluationProgress = evalProgress,
            RecentActivities = recentActivities,
        };
    }

    private static SemesterProgressDto BuildSemesterProgress(string semesterName, IEnumerable<TEDF.Domain.Aggregates.SemesterAggregate.Entities.SemesterPhase> phases)
        => new()
        {
            SemesterName = semesterName,
            Phases = phases
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
