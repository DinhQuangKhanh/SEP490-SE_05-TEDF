using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Evaluations.DTOs;
using TEDF.Application.Features.Projects.DTOs;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Constants;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Domain.Enums.Mentor;
using TEDF.Persistence.SqlServer.Extensions;

namespace TEDF.Persistence.SqlServer.QueryServices;

public class EvaluationsQueryService : IEvaluationsQueryService
{
    private readonly AppDbContext _context;

    public EvaluationsQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EvaluatorFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        var semesters = await _context.Semesters
            .AsNoTracking()
            .OrderByDescending(s => s.StartDate)
            .Select(s => new FilterOptionItemDto(s.Id, s.Name))
            .ToListAsync(cancellationToken);

        var majors = await _context.Majors
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new FilterOptionItemDto(m.Id, m.Name))
            .ToListAsync(cancellationToken);

        return new EvaluatorFilterOptionsDto(semesters, majors);
    }

    public async Task<EvaluatorDashboardDto> GetDashboardAsync(Guid evaluatorId, CancellationToken cancellationToken = default)
    {
        // Get all active assignments for this evaluator
        var assignments = await _context.ProjectEvaluatorAssignments
            .AsNoTracking()
            .Where(a => a.EvaluatorId == evaluatorId && a.IsActive)
            .ToListAsync(cancellationToken);

        // Projects where this evaluator already has ANY completed submission
        var completedProjectIds = assignments
            .Where(a => a.IndividualResult.HasValue && a.IndividualResult != EvaluationResult.Pending)
            .Select(a => a.ProjectId)
            .Distinct()
            .ToList();

        // Pending = projects with NO completed submission, one assignment per project
        var pendingAssignments = assignments
            .Where(a => !completedProjectIds.Contains(a.ProjectId))
            .DistinctBy(a => a.ProjectId)
            .ToList();

        // Completed = one assignment per project (prefer the submitted one)
        var completedAssignments = assignments
            .Where(a => a.IndividualResult.HasValue && a.IndividualResult != EvaluationResult.Pending)
            .DistinctBy(a => a.ProjectId)
            .ToList();

        var totalAssigned = pendingAssignments.Count + completedAssignments.Count;

        var approvedCount = completedAssignments.Count(a => a.IndividualResult == EvaluationResult.Approved);
        var rejectedCount = completedAssignments.Count(a => a.IndividualResult == EvaluationResult.Rejected);
        var needsModCount = completedAssignments.Count(a => a.IndividualResult == EvaluationResult.NeedsModification);

        double? avgReviewDays = null;
        var withDuration = completedAssignments
            .Where(a => a.EvaluatedAt.HasValue)
            .Select(a => (a.EvaluatedAt!.Value - a.AssignedAt).TotalDays)
            .ToList();
        if (withDuration.Count > 0)
            avgReviewDays = Math.Round(withDuration.Average(), 1);

        var stats = new EvaluatorStatsDto
        {
            TotalAssigned = totalAssigned,
            PendingCount = pendingAssignments.Count,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount,
            NeedsModificationCount = needsModCount,
            ReviewedCount = completedAssignments.Count,
            AvgReviewDays = avgReviewDays
        };

        // Get pending evaluations with project details
        var pendingProjectIds = pendingAssignments.Select(a => a.ProjectId).ToList();

        var pendingEvaluationsRaw = await (
            from a in _context.ProjectEvaluatorAssignments.AsNoTracking()
            where a.EvaluatorId == evaluatorId && a.IsActive
                  && pendingProjectIds.Contains(a.ProjectId)
            join p in _context.Projects.AsNoTracking() on a.ProjectId equals p.Id
            join m in _context.Set<Major>().AsNoTracking() on p.MajorId equals m.Id into majors
            from m in majors.DefaultIfEmpty()
            select new
            {
                a.Id,
                a.ProjectId,
                ProjectCode = p.Code,
                ProjectNameVi = p.NameVi,
                MajorName = m != null ? m.Name : "",
                p.GroupId,
                a.AssignedAt
            }
        ).ToListAsync(cancellationToken);

        // One row per project (avoid duplicate rows from same evaluator assigned multiple times)
        var pendingEvaluations = pendingEvaluationsRaw
            .GroupBy(x => x.ProjectId)
            .Select(g => g.First())
            .ToList();

        // Get student names for each project's group
        var groupIds = pendingEvaluations.Where(pe => pe.GroupId != null).Select(pe => pe.GroupId!.Value).Distinct().ToList();

        var groupLeaders = await (
            from gm in _context.GroupMembers.AsNoTracking()
            where groupIds.Contains(gm.GroupId) && gm.Role == Domain.Enums.Group.GroupMemberRole.Leader
            join u in _context.Users.AsNoTracking() on gm.StudentId equals u.Id
            select new { gm.GroupId, u.FullName, u.AvatarUrl }
        ).ToDictionaryAsync(x => x.GroupId, x => new { x.FullName, x.AvatarUrl }, cancellationToken);

        var now = DateTime.UtcNow;
        var pendingDtos = pendingEvaluations.Select(pe =>
        {
            var studentName = "";
            string? studentAvatar = null;
            if (pe.GroupId != null && groupLeaders.TryGetValue(pe.GroupId.Value, out var leader))
            {
                studentName = leader.FullName;
                studentAvatar = leader.AvatarUrl;
            }

            var daysElapsed = (int)(now - pe.AssignedAt).TotalDays;
            return new PendingEvaluationDto
            {
                AssignmentId = pe.Id,
                ProjectId = pe.ProjectId,
                ProjectCode = pe.ProjectCode,
                ProjectNameVi = pe.ProjectNameVi,
                MajorName = pe.MajorName,
                StudentName = studentName,
                StudentAvatar = studentAvatar,
                AssignedAt = pe.AssignedAt,
                DaysElapsed = daysElapsed,
                IsUrgent = daysElapsed > 5
            };
        }).OrderByDescending(pe => pe.IsUrgent).ThenByDescending(pe => pe.DaysElapsed).Take(5).ToList();

        // Get recently reviewed projects
        var recentReviewed = await (
            from a in _context.ProjectEvaluatorAssignments.AsNoTracking()
            where a.EvaluatorId == evaluatorId && a.IsActive
                  && a.IndividualResult.HasValue && a.IndividualResult != EvaluationResult.Pending
                  && a.EvaluatedAt.HasValue
            join p in _context.Projects.AsNoTracking() on a.ProjectId equals p.Id
            orderby a.EvaluatedAt descending
            select new RecentReviewedDto
            {
                ProjectId = a.ProjectId,
                ProjectNameVi = p.NameVi,
                Result = a.IndividualResult.ToString()!,
                EvaluatedAt = a.EvaluatedAt!.Value
            }
        ).Take(5).ToListAsync(cancellationToken);

        return new EvaluatorDashboardDto
        {
            Stats = stats,
            PendingEvaluations = pendingDtos,
            RecentReviewed = recentReviewed
        };
    }

    public async Task<EvaluatorHistoryDto> GetHistoryAsync(
        Guid evaluatorId,
        int page,
        int pageSize,
        string? search,
        string? result,
        string? dateRange,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var query =
            from a in _context.ProjectEvaluatorAssignments.AsNoTracking()
            where a.EvaluatorId == evaluatorId
                  && a.IsActive
                  && a.IndividualResult.HasValue
                  && a.IndividualResult != EvaluationResult.Pending
                  && a.EvaluatedAt.HasValue
            join p in _context.Projects.AsNoTracking() on a.ProjectId equals p.Id
            select new { a, p };

        if (!string.IsNullOrEmpty(dateRange))
        {
            var startOfWeek = now.AddDays(-((int)now.DayOfWeek == 0 ? 6 : (int)now.DayOfWeek - 1)).Date;
            var yesterday = now.AddDays(-1).Date;

            query = dateRange switch
            {
                "thisMonth" => query.Where(x =>
                    x.a.EvaluatedAt!.Value.Month == now.Month &&
                    x.a.EvaluatedAt.Value.Year == now.Year),
                "thisWeek" => query.Where(x => x.a.EvaluatedAt!.Value >= startOfWeek),
                "yesterday" => query.Where(x =>
                    x.a.EvaluatedAt!.Value >= yesterday &&
                    x.a.EvaluatedAt.Value < yesterday.AddDays(1)),
                _ => query
            };
        }

        if (!string.IsNullOrEmpty(result) && Enum.TryParse<EvaluationResult>(result, true, out var resultEnum))
        {
            query = query.Where(x => x.a.IndividualResult == resultEnum);
        }

        if (!string.IsNullOrEmpty(search))
        {
            // Code/NameVi are value objects and cannot be searched in SQL — see ProjectSearchExtensions.
            var matchedProjectIds = await query.Select(x => x.p).MatchSearchTermAsync(search, cancellationToken);
            query = query.Where(x => matchedProjectIds.Contains(x.p.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rawItems = await query
            .OrderByDescending(x => x.a.EvaluatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.a.ProjectId,
                ProjectCode = x.p.Code,
                ProjectNameVi = x.p.NameVi,
                x.p.GroupId,
                EvaluatedAt = x.a.EvaluatedAt!.Value,
                Result = x.a.IndividualResult!.Value.ToString(),
                x.a.Feedback
            })
            .ToListAsync(cancellationToken);

        var groupIds = rawItems
            .Where(x => x.GroupId != null)
            .Select(x => x.GroupId!.Value)
            .Distinct()
            .ToList();

        var groupLeaderMap = new Dictionary<Guid, (string FullName, string? AvatarUrl)>();
        if (groupIds.Count > 0)
        {
            var leaders = await (
                from gm in _context.GroupMembers.AsNoTracking()
                where groupIds.Contains(gm.GroupId) && gm.Role == Domain.Enums.Group.GroupMemberRole.Leader
                join u in _context.Users.AsNoTracking() on gm.StudentId equals u.Id
                select new { gm.GroupId, u.FullName, u.AvatarUrl }
            ).ToListAsync(cancellationToken);

            groupLeaderMap = leaders.ToDictionary(x => x.GroupId, x => (x.FullName, x.AvatarUrl));
        }

        var items = rawItems.Select(x =>
        {
            var studentName = "";
            string? studentAvatar = null;
            if (x.GroupId != null && groupLeaderMap.TryGetValue(x.GroupId.Value, out var leader))
            {
                studentName = leader.FullName;
                studentAvatar = leader.AvatarUrl;
            }

            return new EvaluatorHistoryItemDto
            {
                ProjectId = x.ProjectId,
                ProjectCode = x.ProjectCode,
                ProjectNameVi = x.ProjectNameVi,
                StudentName = studentName,
                StudentAvatar = studentAvatar,
                EvaluatedAt = x.EvaluatedAt,
                Result = x.Result,
                Feedback = x.Feedback
            };
        }).ToList();

        // Overall stats (not filtered)
        var statsBase = _context.ProjectEvaluatorAssignments
            .AsNoTracking()
            .Where(a => a.EvaluatorId == evaluatorId
                        && a.IsActive
                        && a.IndividualResult.HasValue
                        && a.IndividualResult != EvaluationResult.Pending);

        var resultCounts = await statsBase
            .GroupBy(a => a.IndividualResult!.Value)
            .Select(g => new { Result = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Result, x => x.Count, cancellationToken);

        var totalReviewed = resultCounts.Values.Sum();

        var historyStats = new EvaluatorHistoryStatsDto
        {
            TotalReviewed = totalReviewed,
            ApprovedCount = resultCounts.GetValueOrDefault(EvaluationResult.Approved),
            NeedsModificationCount = resultCounts.GetValueOrDefault(EvaluationResult.NeedsModification),
            RejectedCount = resultCounts.GetValueOrDefault(EvaluationResult.Rejected),
        };

        return new EvaluatorHistoryDto
        {
            Stats = historyStats,
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProjectReviewDetailDto?> GetProjectForReviewAsync(
        Guid projectId,
        Guid evaluatorId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Get the assignment first to verify evaluator has access
        var assignment = await _context.ProjectEvaluatorAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.ProjectId == projectId && a.EvaluatorId == evaluatorId && a.IsActive,
                cancellationToken);

        if (assignment is null)
            return null;

        // Get project with major and semester
        var projectData = await (
            from p in _context.Projects.AsNoTracking()
            where p.Id == projectId
            join m in _context.Majors.AsNoTracking() on p.MajorId equals m.Id into majors
            from m in majors.DefaultIfEmpty()
            join s in _context.Semesters.AsNoTracking() on p.SemesterId equals s.Id into semesters
            from s in semesters.DefaultIfEmpty()
            select new
            {
                p.Id,
                Code = p.Code.ToString(),
                NameVi = p.NameVi.ToString(),
                NameEn = p.NameEn.ToString(),
                NameAbbr = p.NameAbbr,
                Description = p.Description.ToString(),
                Objectives = p.Objectives.ToString(),
                p.Scope,
                Technologies = p.Technologies != null ? p.Technologies.ToString() : null,
                p.ExpectedResults,
                p.MaxStudents,
                p.SubmittedAt,
                p.EvaluationCount,
                p.GroupId,
                MajorName = m != null ? m.Name : "",
                MajorCode = m != null ? m.Code : "",
                SemesterName = s != null ? s.Name : ""
            }
        ).FirstOrDefaultAsync(cancellationToken);

        if (projectData is null)
            return null;

        // Get mentor name
        var mentorName = await (
            from pm in _context.ProjectMentors.AsNoTracking()
            where pm.ProjectId == projectId && pm.Status == ProjectMentorStatus.Active
            join u in _context.Users.AsNoTracking() on pm.MentorId equals u.Id
            select u.FullName
        ).FirstOrDefaultAsync(cancellationToken) ?? "";

        // Get student (group leader) name
        var studentName = "";
        string? studentAvatar = null;
        if (projectData.GroupId != null)
        {
            var leader = await (
                from gm in _context.GroupMembers.AsNoTracking()
                where gm.GroupId == projectData.GroupId.Value && gm.Role == Domain.Enums.Group.GroupMemberRole.Leader
                join u in _context.Users.AsNoTracking() on gm.StudentId equals u.Id
                select new { u.FullName, u.AvatarUrl }
            ).FirstOrDefaultAsync(cancellationToken);

            if (leader != null)
            {
                studentName = leader.FullName;
                studentAvatar = leader.AvatarUrl;
            }
        }

        var daysElapsed = (int)(now - assignment.AssignedAt).TotalDays;

        return new ProjectReviewDetailDto
        {
            ProjectId = projectData.Id,
            ProjectCode = projectData.Code,
            NameVi = projectData.NameVi,
            NameEn = projectData.NameEn,
            NameAbbr = projectData.NameAbbr,
            Description = projectData.Description,
            Objectives = projectData.Objectives,
            Scope = projectData.Scope,
            Technologies = projectData.Technologies,
            ExpectedResults = projectData.ExpectedResults,
            MaxStudents = projectData.MaxStudents,
            SubmittedAt = projectData.SubmittedAt,
            EvaluationCount = projectData.EvaluationCount,
            MajorName = projectData.MajorName,
            MajorCode = projectData.MajorCode,
            SemesterName = projectData.SemesterName,
            MentorName = mentorName,
            StudentName = studentName,
            StudentAvatar = studentAvatar,
            AssignmentId = assignment.Id,
            AssignedAt = assignment.AssignedAt,
            DaysElapsed = daysElapsed,
            ExistingFeedback = assignment.Feedback,
            ExistingResult = assignment.IndividualResult?.ToString()
        };
    }

    public async Task<EvaluatorProjectsDto> GetProjectsAsync(
        Guid evaluatorId,
        int page,
        int pageSize,
        string? search,
        int? semesterId,
        int? majorId,
        string? result,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Base query: all active assignments for this evaluator
        var query =
            from a in _context.ProjectEvaluatorAssignments.AsNoTracking()
            where a.EvaluatorId == evaluatorId && a.IsActive
            join p in _context.Projects.AsNoTracking() on a.ProjectId equals p.Id
            join m in _context.Majors.AsNoTracking() on p.MajorId equals m.Id into majors
            from m in majors.DefaultIfEmpty()
            join s in _context.Semesters.AsNoTracking() on p.SemesterId equals s.Id into semesters
            from s in semesters.DefaultIfEmpty()
            select new { a, p, m, s };

        // Filter by semester
        if (semesterId.HasValue)
            query = query.Where(x => x.p.SemesterId == semesterId.Value);

        // Filter by major
        if (majorId.HasValue)
            query = query.Where(x => x.p.MajorId == majorId.Value);

        // Filter by result
        if (!string.IsNullOrEmpty(result))
        {
            if (result == "Pending")
            {
                query = query.Where(x => x.a.IndividualResult == null || x.a.IndividualResult == EvaluationResult.Pending);
            }
            else if (Enum.TryParse<EvaluationResult>(result, true, out var resultEnum))
            {
                query = query.Where(x => x.a.IndividualResult == resultEnum);
            }
        }

        // Filter by search (project code or name).
        // Code/NameVi are value objects and cannot be searched in SQL — see ProjectSearchExtensions.
        if (!string.IsNullOrEmpty(search))
        {
            var matchedProjectIds = await query.Select(x => x.p).MatchSearchTermAsync(search, cancellationToken);
            query = query.Where(x => matchedProjectIds.Contains(x.p.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Fetch paginated raw items
        var rawItems = await query
            .OrderByDescending(x => x.a.IndividualResult == null || x.a.IndividualResult == EvaluationResult.Pending)
            .ThenBy(x => x.a.AssignedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                AssignmentId = x.a.Id,
                x.a.ProjectId,
                ProjectCode = x.p.Code,
                ProjectNameVi = x.p.NameVi,
                MajorName = x.m != null ? x.m.Name : "",
                x.p.GroupId,
                x.p.SubmittedAt,
                x.a.AssignedAt,
                x.a.IndividualResult,
            })
            .ToListAsync(cancellationToken);

        // Fetch group leaders for student info
        var groupIds = rawItems
            .Where(x => x.GroupId != null)
            .Select(x => x.GroupId!.Value)
            .Distinct()
            .ToList();

        var groupLeaderMap = new Dictionary<Guid, (string FullName, string? AvatarUrl)>();
        if (groupIds.Count > 0)
        {
            var leaders = await (
                from gm in _context.GroupMembers.AsNoTracking()
                where groupIds.Contains(gm.GroupId) && gm.Role == Domain.Enums.Group.GroupMemberRole.Leader
                join u in _context.Users.AsNoTracking() on gm.StudentId equals u.Id
                select new { gm.GroupId, u.FullName, u.AvatarUrl }
            ).ToListAsync(cancellationToken);

            groupLeaderMap = leaders.ToDictionary(x => x.GroupId, x => (x.FullName, x.AvatarUrl));
        }

        // Fetch mentor names
        var projectIds = rawItems.Select(x => x.ProjectId).Distinct().ToList();

        var mentorMap = new Dictionary<Guid, string>();
        if (projectIds.Count > 0)
        {
            var mentors = await (
                from pm in _context.ProjectMentors.AsNoTracking()
                where projectIds.Contains(pm.ProjectId) && pm.Status == Domain.Enums.Mentor.ProjectMentorStatus.Active
                join u in _context.Users.AsNoTracking() on pm.MentorId equals u.Id
                select new { pm.ProjectId, u.FullName }
            ).ToListAsync(cancellationToken);

            // Take first active mentor per project
            foreach (var m in mentors)
            {
                mentorMap.TryAdd(m.ProjectId, m.FullName);
            }
        }

        // Build items
        var items = rawItems.Select(x =>
        {
            var studentName = "";
            string? studentAvatar = null;
            if (x.GroupId != null && groupLeaderMap.TryGetValue(x.GroupId.Value, out var leader))
            {
                studentName = leader.FullName;
                studentAvatar = leader.AvatarUrl;
            }

            mentorMap.TryGetValue(x.ProjectId, out var mentorName);

            var isPending = x.IndividualResult == null || x.IndividualResult == EvaluationResult.Pending;

            return new EvaluatorProjectItemDto
            {
                AssignmentId = x.AssignmentId,
                ProjectId = x.ProjectId,
                ProjectCode = x.ProjectCode,
                ProjectNameVi = x.ProjectNameVi,
                MajorName = x.MajorName,
                StudentName = studentName,
                StudentAvatar = studentAvatar,
                MentorName = mentorName ?? "",
                SubmittedAt = x.SubmittedAt,
                AssignedAt = x.AssignedAt,
                IndividualResult = isPending ? "Pending" : x.IndividualResult!.Value.ToString(),
                IsUrgent = isPending && (now - x.AssignedAt).TotalDays > 5,
            };
        }).ToList();

        return new EvaluatorProjectsDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // Department-head evaluator management (moved in from DepartmentHeadQueryService).
    // Resolves the caller's department internally so the handler stays thin.
    public async Task<List<DepartmentEvaluatorDto>> GetDepartmentEvaluatorsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var departmentId = await _context.Users.AsNoTracking()
            .Where(u => u.Id == currentUserId)
            .Select(u => u.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleValidationException("Current user is not assigned to any department.");

        // Offer lecturers on the eligible-mentor roster of the active OR any upcoming semester
        // (a roster is published ahead of the term it applies to), scoped to this department.
        var now = DateTime.UtcNow;
        var evaluators = await _context.Users
            .Include(u => u.Lecturer)
            .Where(u => u.DepartmentId.HasValue && u.DepartmentId.Value == departmentId &&
                        u.Roles.Any(r => r.IsActive &&
                            _context.Roles.Any(dbRole => dbRole.Id == r.RoleId && dbRole.Name == DomainRoleNames.Evaluator)) &&
                        _context.EligibleMentors.Any(m => m.MentorId == u.Id && m.IsAssigned &&
                            _context.Semesters.Any(s => s.Id == m.SemesterId && s.EndDate >= now)))
            .ToListAsync(cancellationToken);

        var evaluatorIds = evaluators.Select(e => e.Id).ToList();

        var assignmentCounts = await _context.ProjectEvaluatorAssignments
            .Where(a => evaluatorIds.Contains(a.EvaluatorId) && a.IsActive &&
                        (!a.IndividualResult.HasValue || a.IndividualResult.Value == EvaluationResult.Pending))
            .GroupBy(a => a.EvaluatorId)
            .Select(g => new { EvaluatorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EvaluatorId, x => x.Count, cancellationToken);

        return evaluators.Select(e => new DepartmentEvaluatorDto
        {
            UserId = e.Id,
            FullName = e.FullName,
            Email = e.Email.Value,
            AcademicTitle = e.Lecturer?.AcademicTitle,
            ActiveAssignmentCount = assignmentCounts.GetValueOrDefault(e.Id, 0)
        }).OrderBy(e => e.FullName).ToList();
    }
}
