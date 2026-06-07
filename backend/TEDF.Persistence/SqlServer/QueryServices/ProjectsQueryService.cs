using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Projects.DTOs;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Project;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// Read-side service for the Projects feature. See <see cref="IProjectsQueryService"/>.
/// </summary>
public class ProjectsQueryService : IProjectsQueryService
{
    private readonly AppDbContext _context;
    private readonly IProjectRepository _projectRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IMajorReadRepository _majorRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;

    public ProjectsQueryService(
        AppDbContext context,
        IProjectRepository projectRepository,
        ISemesterRepository semesterRepository,
        IMajorReadRepository majorRepository,
        IUserRepository userRepository,
        IGroupRepository groupRepository)
    {
        _context = context;
        _projectRepository = projectRepository;
        _semesterRepository = semesterRepository;
        _majorRepository = majorRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
    }

    public async Task<GetProjectsQueryResult> GetProjectsAsync(
        string? search, int? semesterId, string? status, int? majorId,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        ProjectStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var parsed))
            statusFilter = parsed;

        var (projects, totalCount) = await _projectRepository.GetPagedAsync(
            search, semesterId, statusFilter, majorId, page, pageSize, cancellationToken);

        var projectList = projects.ToList();

        var majorIds = projectList.Select(p => p.MajorId).Distinct().ToList();
        var mentorIds = projectList.SelectMany(p => p.Mentors.Where(m => m.IsActive).Select(m => m.MentorId)).Distinct().ToList();
        var groupIds = projectList.Where(p => p.GroupId.HasValue).Select(p => p.GroupId!.Value).Distinct().ToList();

        var semesters = await _semesterRepository.GetAllAsync(cancellationToken);
        var semesterMap = semesters.ToDictionary(s => s.Id, s => s.Name);

        var majorMap = new Dictionary<int, (string Name, string Code)>();
        foreach (var mid in majorIds)
        {
            var major = await _majorRepository.GetByIdAsync(mid, cancellationToken);
            if (major != null)
                majorMap[mid] = (major.Name, major.Code);
        }

        var mentorUsers = (await _userRepository.GetByIdsAsync(mentorIds, cancellationToken)).ToList();
        var mentorMap = mentorUsers.ToDictionary(u => u.Id, u => u.DisplayName);

        var groupMap = new Dictionary<Guid, (string? Code, List<Guid> StudentIds)>();
        foreach (var gid in groupIds)
        {
            var group = await _groupRepository.GetWithMembersAsync(gid, cancellationToken);
            if (group != null)
            {
                var activeStudentIds = group.Members.Where(m => m.IsActive).Select(m => m.StudentId).ToList();
                groupMap[gid] = (group.Code.Value, activeStudentIds);
            }
        }

        var allStudentIds = groupMap.Values.SelectMany(g => g.StudentIds).Distinct().ToList();
        var studentUsers = allStudentIds.Count > 0
            ? (await _userRepository.GetByIdsAsync(allStudentIds, cancellationToken)).ToList()
            : [];
        var studentMap = studentUsers.ToDictionary(u => u.Id, u => u.FullName);

        var items = projectList.Select(p =>
        {
            var mentorNames = p.Mentors
                .Where(m => m.IsActive)
                .Select(m => mentorMap.TryGetValue(m.MentorId, out var name) ? name : "N/A")
                .ToList();

            var studentNames = new List<string>();
            string? groupCode = null;
            if (p.GroupId.HasValue && groupMap.TryGetValue(p.GroupId.Value, out var groupInfo))
            {
                groupCode = groupInfo.Code;
                studentNames = groupInfo.StudentIds
                    .Select(sid => studentMap.TryGetValue(sid, out var name) ? name : "N/A")
                    .ToList();
            }

            var majorInfo = majorMap.TryGetValue(p.MajorId, out var mj) ? mj : ("N/A", "N/A");

            return new ProjectListItemDto(
                p.Id,
                p.Code.Value,
                p.NameVi.Value,
                p.NameEn?.Value,
                p.Status.ToString(),
                majorInfo.Item1,
                majorInfo.Item2,
                semesterMap.TryGetValue(p.SemesterId, out var semName) ? semName : "N/A",
                p.SourceType.ToString(),
                mentorNames,
                studentNames,
                groupCode,
                p.CreatedAt);
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new GetProjectsQueryResult(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<DepartmentProjectsResponse> GetDepartmentProjectsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var departmentId = await _context.Users.AsNoTracking()
            .Where(u => u.Id == currentUserId)
            .Select(u => u.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleValidationException("Current user is not assigned to any department.");

        var majorIds = await _context.Majors
            .Where(m => m.DepartmentId == departmentId && m.IsActive)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

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

        var assignments = await _context.ProjectEvaluatorAssignments
            .Where(a => projectIds.Contains(a.ProjectId) && a.IsActive)
            .ToListAsync(cancellationToken);

        var evaluatorIds = assignments.Select(a => a.EvaluatorId).Distinct().ToList();
        var evaluatorNames = await _context.Users
            .Where(u => evaluatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var mentorIds = projects.SelectMany(p => p.Mentors.Where(m => m.IsActive).Select(m => m.MentorId)).Distinct().ToList();
        var mentorNames = await _context.Users
            .Where(u => mentorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

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
}
