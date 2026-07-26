using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Projects.DTOs;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Mentor;
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
        var mentorMap = mentorUsers.ToDictionary(u => u.Id, u => u.FullName);

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

    public async Task<GetMySupervisedProjectsResult> GetMySupervisedProjectsAsync(
        Guid mentorId, string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        // A mentor supervises a small set of projects, so load them and apply
        // search/sort/paging in memory (avoids translating value-object access into SQL).
        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Mentors.Any(m => m.MentorId == mentorId && m.Status == ProjectMentorStatus.Active))
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            projects = projects.Where(p =>
                p.NameVi.Value.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (p.NameEn != null && p.NameEn.Value.Contains(term, StringComparison.OrdinalIgnoreCase))
                || p.Code.Value.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        projects = sort switch
        {
            "name" => projects.OrderBy(p => p.NameVi.Value, StringComparer.OrdinalIgnoreCase).ToList(),
            "oldest" => projects.OrderBy(p => p.CreatedAt).ToList(),
            "status" => projects.OrderBy(p => p.Status).ThenByDescending(p => p.CreatedAt).ToList(),
            _ => projects.OrderByDescending(p => p.CreatedAt).ToList(),
        };

        var totalCount = projects.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var pageItems = projects.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var semesterIds = pageItems.Select(p => p.SemesterId).Distinct().ToList();
        var semesterNames = await _context.Semesters
            .Where(s => semesterIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var groupIds = pageItems.Where(p => p.GroupId.HasValue).Select(p => p.GroupId!.Value).Distinct().ToList();
        var groups = await _context.Set<Group>()
            .Where(g => groupIds.Contains(g.Id))
            .ToListAsync(cancellationToken);
        var groupCodes = groups.ToDictionary(g => g.Id, g => g.Code.Value);

        var items = pageItems.Select(p => new SupervisedProjectDto(
            p.Id,
            p.Code.Value,
            p.NameVi.Value,
            p.NameEn?.Value,
            p.Status.ToString(),
            (int)p.Status,
            semesterNames.GetValueOrDefault(p.SemesterId, "N/A"),
            p.GroupId.HasValue ? groupCodes.GetValueOrDefault(p.GroupId.Value) : null,
            p.Description,
            p.Objectives,
            p.StartDate,
            p.Deadline,
            p.CreatedAt)).ToList();

        return new GetMySupervisedProjectsResult(items, totalCount, page, pageSize, totalPages);
    }

    /// <remarks>
    /// Reads the approval trail from SQL Server: the rows are written in the same transaction as
    /// the state changes they describe, so counts here are exact rather than eventually consistent.
    /// </remarks>
    public async Task<GetProjectAuditLogsResponse> GetProjectAuditLogsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        // Left join on Users so a log row survives a removed user — PerformedByName is the
        // snapshot taken at write time, the join only refreshes it to the current name.
        var rows = await (
            from log in _context.ProjectAuditLogs.AsNoTracking()
            where log.ProjectId == projectId
            join user in _context.Users on log.PerformedBy equals user.Id into matches
            from user in matches.DefaultIfEmpty()
            orderby log.Timestamp descending
            select new { Log = log, CurrentName = user != null ? user.FullName : null })
            .ToListAsync(cancellationToken);

        var dtos = rows.Select(r => new ProjectAuditLogDto
        {
            Id = r.Log.Id,
            Action = r.Log.Action.ToString(),
            PerformedBy = r.Log.PerformedBy,
            PerformedByName = r.CurrentName ?? r.Log.PerformedByName,
            OldStatus = r.Log.OldStatus?.ToString(),
            NewStatus = r.Log.NewStatus?.ToString(),
            SubmissionNumber = r.Log.SubmissionNumber,
            Timestamp = r.Log.Timestamp,
            Metadata = DeserializeMetadata(r.Log.MetadataJson)
        }).ToList();

        var revisionCount = rows.Count(r =>
            r.Log.Action is ProjectAuditAction.NeedsModification or ProjectAuditAction.MentorNeedsModification);

        var submissionCount = rows
            .Where(r => r.Log.SubmissionNumber.HasValue)
            .Select(r => r.Log.SubmissionNumber!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return new GetProjectAuditLogsResponse
        {
            Logs = dtos,
            RevisionCount = revisionCount,
            SubmissionCount = submissionCount
        };
    }

    /// <inheritdoc />
    public async Task<GetDepartmentAuditLogsResponse> GetDepartmentAuditLogsAsync(
        Guid currentUserId, string? search, string? actions,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var departmentId = await _context.Users.AsNoTracking()
            .Where(u => u.Id == currentUserId)
            .Select(u => u.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleValidationException("Current user is not assigned to any department.");

        var majorIds = await _context.Majors.AsNoTracking()
            .Where(m => m.DepartmentId == departmentId && m.IsActive)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        // Scope: only projects belonging to the department's majors.
        var scoped = from log in _context.ProjectAuditLogs.AsNoTracking()
                     join project in _context.Projects.AsNoTracking() on log.ProjectId equals project.Id
                     where majorIds.Contains(project.MajorId)
                     select new { Log = log, Project = project };

        // Stats cover the whole department scope so the cards stay stable while paging/filtering.
        var stats = await scoped
            .GroupBy(_ => 1)
            .Select(g => new DepartmentAuditLogStatsDto
            {
                Total = g.Count(),
                Submitted = g.Count(x =>
                    x.Log.Action == ProjectAuditAction.Submitted ||
                    x.Log.Action == ProjectAuditAction.Resubmitted ||
                    x.Log.Action == ProjectAuditAction.SubmittedToMentor),
                Approved = g.Count(x =>
                    x.Log.Action == ProjectAuditAction.Approved ||
                    x.Log.Action == ProjectAuditAction.MentorApproved),
                Revision = g.Count(x =>
                    x.Log.Action == ProjectAuditAction.NeedsModification ||
                    x.Log.Action == ProjectAuditAction.MentorNeedsModification),
                Rejected = g.Count(x => x.Log.Action == ProjectAuditAction.Rejected)
            })
            .FirstOrDefaultAsync(cancellationToken) ?? new DepartmentAuditLogStatsDto();

        var parsedActions = ParseActions(actions);
        if (parsedActions.Count > 0)
            scoped = scoped.Where(x => parsedActions.Contains(x.Log.Action));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            scoped = scoped.Where(x =>
                x.Project.Code.Value.Contains(term) ||
                x.Project.NameVi.Value.Contains(term) ||
                (x.Log.PerformedByName != null && x.Log.PerformedByName.Contains(term)));
        }

        var totalCount = await scoped.CountAsync(cancellationToken);

        var rows = await scoped
            .OrderByDescending(x => x.Log.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Refresh performer names in one round-trip; fall back to the write-time snapshot.
        var performerIds = rows
            .Where(r => r.Log.PerformedBy.HasValue)
            .Select(r => r.Log.PerformedBy!.Value)
            .Distinct()
            .ToList();

        var performerNames = await _context.Users.AsNoTracking()
            .Where(u => performerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var items = rows.Select(r => new DepartmentAuditLogItemDto
        {
            Id = r.Log.Id,
            ProjectId = r.Log.ProjectId,
            ProjectCode = r.Project.Code.Value,
            ProjectName = r.Project.NameVi.Value,
            Action = r.Log.Action.ToString(),
            PerformedBy = r.Log.PerformedBy,
            PerformedByName = r.Log.PerformedBy.HasValue
                ? performerNames.GetValueOrDefault(r.Log.PerformedBy.Value) ?? r.Log.PerformedByName
                : r.Log.PerformedByName,
            OldStatus = r.Log.OldStatus?.ToString(),
            NewStatus = r.Log.NewStatus?.ToString(),
            SubmissionNumber = r.Log.SubmissionNumber,
            Timestamp = r.Log.Timestamp,
            Metadata = DeserializeMetadata(r.Log.MetadataJson)
        }).ToList();

        return new GetDepartmentAuditLogsResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Stats = stats
        };
    }

    /// <summary>Parses the comma-separated action filter, ignoring names that are not valid actions.</summary>
    private static List<ProjectAuditAction> ParseActions(string? actions)
    {
        if (string.IsNullOrWhiteSpace(actions)) return [];

        return actions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => Enum.TryParse<ProjectAuditAction>(name, ignoreCase: true, out var parsed)
                ? parsed
                : (ProjectAuditAction?)null)
            .Where(a => a.HasValue)
            .Select(a => a!.Value)
            .Distinct()
            .ToList();
    }

    private static object? DeserializeMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (JsonException)
        {
            // A malformed row must not break the whole trail.
            return null;
        }
    }
}
