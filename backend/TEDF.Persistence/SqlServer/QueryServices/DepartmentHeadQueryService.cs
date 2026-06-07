using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Projects.DTOs;
using TEDF.Domain.Constants;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Domain.Enums.Project;

namespace TEDF.Persistence.SqlServer.QueryServices
{
    public class DepartmentHeadQueryService : IDepartmentHeadQueryService
    {
        private readonly AppDbContext _context;

        public DepartmentHeadQueryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DepartmentProjectsResponse> GetDepartmentProjectsAsync(
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var majorIds = await _context.Majors
                .Where(m => m.DepartmentId == departmentId && m.IsActive)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            // Get projects that are pending evaluation or have been evaluated
            var projects = await _context.Projects
                .Include(p => p.Mentors)
                .Where(p => majorIds.Contains(p.MajorId) &&
                           (p.Status == ProjectStatus.PendingEvaluation ||
                            p.Status == ProjectStatus.Approved ||
                            p.Status == ProjectStatus.NeedsModification ||
                            p.Status == ProjectStatus.Rejected))
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync(cancellationToken);

            var projectIds = projects.Select(p => p.Id).ToList();

            // Get all assignments for these projects
            var assignments = await _context.ProjectEvaluatorAssignments
                .Where(a => projectIds.Contains(a.ProjectId) && a.IsActive)
                .ToListAsync(cancellationToken);

            // Get evaluator names
            var evaluatorIds = assignments.Select(a => a.EvaluatorId).Distinct().ToList();
            var evaluatorNames = await _context.Users
                .Where(u => evaluatorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

            // Get mentor names
            var mentorIds = projects.SelectMany(p => p.Mentors.Where(m => m.IsActive).Select(m => m.MentorId)).Distinct().ToList();
            var mentorNames = await _context.Users
                .Where(u => mentorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

            // Get major and semester names
            var majorNames = await _context.Majors
                .Where(m => majorIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken);

            var semesterIds = projects.Select(p => p.SemesterId).Distinct().ToList();
            var semesterNames = await _context.Semesters
                .Where(s => semesterIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

            var items = projects.Select(p =>
            {
                var projectAssignments = assignments
                    .Where(a => a.ProjectId == p.Id)
                    .OrderBy(a => a.EvaluatorOrder)
                    .ToList();

                var submittedCount = projectAssignments.Count(a => a.HasSubmittedEvaluation);
                var distinctResults = projectAssignments
                    .Where(a => a.HasSubmittedEvaluation)
                    .Select(a => a.IndividualResult)
                    .Distinct()
                    .ToList();

                var hasConflict = submittedCount >= 2 && distinctResults.Count > 1;
                var needsDecision = hasConflict && p.Status == ProjectStatus.PendingEvaluation;

                return new DepartmentProjectDto
                {
                    ProjectId = p.Id,
                    ProjectCode = p.Code.Value,
                    NameVi = p.NameVi.Value,
                    NameEn = p.NameEn.Value,
                    MajorName = majorNames.GetValueOrDefault(p.MajorId, ""),
                    SemesterName = semesterNames.GetValueOrDefault(p.SemesterId, ""),
                    Status = p.Status.ToString(),
                    StatusValue = (int)p.Status,
                    SubmittedAt = p.SubmittedAt?.ToString("o"),
                    AssignedEvaluatorCount = projectAssignments.Count,
                    HasConflict = hasConflict,
                    NeedsFinalDecision = needsDecision,
                    Mentors = p.Mentors
                        .Where(m => m.IsActive)
                        .Select(m => new MentorSummaryDto
                        {
                            MentorId = m.MentorId,
                            MentorName = mentorNames.GetValueOrDefault(m.MentorId, "")
                        }).ToList(),
                    Evaluators = projectAssignments.Select(a => new EvaluatorAssignmentDto
                    {
                        AssignmentId = a.Id,
                        EvaluatorId = a.EvaluatorId,
                        EvaluatorName = evaluatorNames.GetValueOrDefault(a.EvaluatorId, ""),
                        EvaluatorOrder = a.EvaluatorOrder,
                        IndividualResult = a.IndividualResult?.ToString(),
                        IndividualResultValue = a.IndividualResult.HasValue ? (int?)a.IndividualResult.Value : null,
                        Feedback = a.Feedback,
                        EvaluatedAt = a.EvaluatedAt?.ToString("o"),
                        HasSubmitted = a.HasSubmittedEvaluation
                    }).ToList()
                };
            }).ToList();

            return new DepartmentProjectsResponse
            {
                Items = items,
                TotalCount = items.Count,
                PendingAssignmentCount = items.Count(i => i.AssignedEvaluatorCount < 2 && i.StatusValue == (int)ProjectStatus.PendingEvaluation),
                InEvaluationCount = items.Count(i => i.AssignedEvaluatorCount >= 2 && !i.NeedsFinalDecision && i.StatusValue == (int)ProjectStatus.PendingEvaluation),
                NeedsFinalDecisionCount = items.Count(i => i.NeedsFinalDecision),
                CompletedCount = items.Count(i => i.StatusValue != (int)ProjectStatus.PendingEvaluation)
            };
        }

        public async Task<List<DepartmentEvaluatorDto>> GetDepartmentEvaluatorsAsync(
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            // Get users with Evaluator role in this department
            var evaluators = await _context.Users
                .Where(u => u.DepartmentId.HasValue &&
                           u.DepartmentId.Value == departmentId &&
                           u.Roles.Any(r => r.RoleName == DomainRoleNames.Evaluator && r.IsActive))
                .ToListAsync(cancellationToken);

            var evaluatorIds = evaluators.Select(e => e.Id).ToList();

            // Get active assignment counts
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
                AcademicTitle = e.AcademicTitle,
                ActiveAssignmentCount = assignmentCounts.GetValueOrDefault(e.Id, 0)
            }).OrderBy(e => e.FullName).ToList();
        }

        public async Task<DepartmentHeadDashboardDto> GetDashboardAsync(
            int departmentId,
            Guid headId,
            CancellationToken cancellationToken = default)
        {
            // 1. Department name + head name
            var department = await _context.Departments.AsNoTracking()
                .Where(d => d.Id == departmentId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "";

            var headName = await _context.Users.AsNoTracking()
                .Where(u => u.Id == headId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken) ?? "";

            // 2. Active semester with phases
            var now = DateTime.UtcNow;
            var activeSemester = await _context.Semesters.AsNoTracking()
                .Include(s => s.Phases)
                .FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now, cancellationToken);

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
                        }).ToList()
                };
            }

            // 3. Major IDs for this department
            var majorIds = await _context.Majors.AsNoTracking()
                .Where(m => m.DepartmentId == departmentId && m.IsActive)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            // 4. Projects in relevant statuses
            var projects = await _context.Projects.AsNoTracking()
                .Where(p => majorIds.Contains(p.MajorId) &&
                           (p.Status == ProjectStatus.PendingEvaluation ||
                            p.Status == ProjectStatus.Approved ||
                            p.Status == ProjectStatus.NeedsModification ||
                            p.Status == ProjectStatus.Rejected))
                .Select(p => new { p.Id, p.Status })
                .ToListAsync(cancellationToken);

            var projectIds = projects.Select(p => p.Id).ToList();

            // 5. Evaluator assignments
            var assignments = await _context.ProjectEvaluatorAssignments.AsNoTracking()
                .Where(a => projectIds.Contains(a.ProjectId) && a.IsActive)
                .Select(a => new { a.ProjectId, a.IndividualResult, a.HasSubmittedEvaluation, a.EvaluatorId, a.AssignedAt, a.EvaluatedAt })
                .ToListAsync(cancellationToken);

            // 6. Compute stats
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

            // 7. Evaluation progress (from completed projects)
            var completedProjectIds = projectStats
                .Where(p => p.Status != ProjectStatus.PendingEvaluation)
                .Select(p => p.Id).ToList();

            var evalProgress = new EvaluationProgressDto
            {
                Approved = projects.Count(p => p.Status == ProjectStatus.Approved),
                Rejected = projects.Count(p => p.Status == ProjectStatus.Rejected),
                NeedsModification = projects.Count(p => p.Status == ProjectStatus.NeedsModification),
                Pending = projects.Count(p => p.Status == ProjectStatus.PendingEvaluation),
            };

            // 8. Recent activities (evaluator assignments + submissions, sorted by date)
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
    }
}
