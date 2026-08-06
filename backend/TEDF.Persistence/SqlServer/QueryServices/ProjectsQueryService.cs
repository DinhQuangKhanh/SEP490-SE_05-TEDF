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
using TEDF.Persistence.SqlServer.Extensions;

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

        var parsedMetadata = rows.Select(r => ParseMetadata(r.Log.MetadataJson)).ToList();
        var metadataNames = await LoadUserNamesAsync(CollectMetadataUserIds(parsedMetadata), cancellationToken);

        var dtos = rows.Select((r, i) => new ProjectAuditLogDto
        {
            Id = r.Log.Id,
            Action = r.Log.Action.ToString(),
            PerformedBy = r.Log.PerformedBy,
            PerformedByName = r.CurrentName ?? r.Log.PerformedByName,
            OldStatus = r.Log.OldStatus?.ToString(),
            NewStatus = r.Log.NewStatus?.ToString(),
            SubmissionNumber = r.Log.SubmissionNumber,
            Timestamp = r.Log.Timestamp,
            Metadata = RenderMetadata(parsedMetadata[i], metadataNames)
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
        Guid currentUserId, DepartmentAuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 10 : filter.PageSize;

        var departmentId = await _context.Users.AsNoTracking()
            .Where(u => u.Id == currentUserId)
            .Select(u => u.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleValidationException("Current user is not assigned to any department.");

        // Deactivated majors are included on purpose: an audit trail must not lose history
        // when a major is retired, otherwise past terms silently disappear from the log.
        var majorIds = await _context.Majors.AsNoTracking()
            .Where(m => m.DepartmentId == departmentId)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        // Scope: only projects belonging to the department's majors.
        var scoped = from log in _context.ProjectAuditLogs.AsNoTracking()
                     join project in _context.Projects.AsNoTracking() on log.ProjectId equals project.Id
                     where majorIds.Contains(project.MajorId)
                     select new { Log = log, Project = project };

        if (filter.SemesterId.HasValue)
            scoped = scoped.Where(x => x.Project.SemesterId == filter.SemesterId.Value);

        if (filter.From.HasValue)
            scoped = scoped.Where(x => x.Log.Timestamp >= filter.From.Value);

        if (filter.To.HasValue)
            scoped = scoped.Where(x => x.Log.Timestamp <= filter.To.Value);

        // Stats cover the semester/date scope but ignore the action and search filters, so the
        // cards stay stable while the action tabs drill into them.
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

        var parsedActions = ParseActions(filter.Actions);
        if (parsedActions.Count > 0)
            scoped = scoped.Where(x => parsedActions.Contains(x.Log.Action));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();

            // Project code and name are value objects, so they are matched by id (see
            // ProjectSearchExtensions); PerformedByName is a plain column and stays in SQL.
            var matchedProjectIds = await _context.Projects.AsNoTracking()
                .Where(p => majorIds.Contains(p.MajorId))
                .MatchSearchTermAsync(term, cancellationToken);

            scoped = scoped.Where(x =>
                matchedProjectIds.Contains(x.Project.Id) ||
                (x.Log.PerformedByName != null && x.Log.PerformedByName.Contains(term)));
        }

        var totalCount = await scoped.CountAsync(cancellationToken);

        var rows = await scoped
            .OrderByDescending(x => x.Log.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var parsedMetadata = rows.Select(r => ParseMetadata(r.Log.MetadataJson)).ToList();

        // One round-trip for both the performers and the users referenced inside the metadata.
        // Performer names fall back to the write-time snapshot when the user is gone.
        var performerIds = rows
            .Where(r => r.Log.PerformedBy.HasValue)
            .Select(r => r.Log.PerformedBy!.Value);

        var names = await LoadUserNamesAsync(
            performerIds.Concat(CollectMetadataUserIds(parsedMetadata)), cancellationToken);

        var items = rows.Select((r, i) => new DepartmentAuditLogItemDto
        {
            Id = r.Log.Id,
            ProjectId = r.Log.ProjectId,
            ProjectCode = r.Project.Code.Value,
            ProjectName = r.Project.NameVi.Value,
            Action = r.Log.Action.ToString(),
            PerformedBy = r.Log.PerformedBy,
            PerformedByName = r.Log.PerformedBy.HasValue
                ? names.GetValueOrDefault(r.Log.PerformedBy.Value) ?? r.Log.PerformedByName
                : r.Log.PerformedByName,
            OldStatus = r.Log.OldStatus?.ToString(),
            NewStatus = r.Log.NewStatus?.ToString(),
            SubmissionNumber = r.Log.SubmissionNumber,
            Timestamp = r.Log.Timestamp,
            Metadata = RenderMetadata(parsedMetadata[i], names)
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

    // ── Audit metadata rendering ────────────────────────────────────────────────
    // The audit interceptor stores raw user ids because an id is stable while a name is not.
    // The reader needs names, so they are resolved here in one batched lookup per query.

    /// <summary>Metadata keys holding a user id, mapped to the key their resolved name is written to.</summary>
    private static readonly Dictionary<string, string> UserIdMetadataKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MentorId"] = "mentorName",
        ["EvaluatorId"] = "evaluatorName",
        ["AssignedBy"] = "assignedByName",
        ["DeletedBy"] = "deletedByName"
    };

    /// <summary>Identifiers that mean nothing to a reader; dropped rather than shown as a raw GUID.</summary>
    private static readonly HashSet<string> HiddenMetadataKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "DocumentId", "PhaseId"
    };

    private static Dictionary<string, JsonElement>? ParseMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        }
        catch (JsonException)
        {
            // A malformed row must not break the whole trail.
            return null;
        }
    }

    /// <summary>Collects every user id referenced by the parsed metadata, so names load in one round-trip.</summary>
    private static IEnumerable<Guid> CollectMetadataUserIds(IEnumerable<Dictionary<string, JsonElement>?> parsed)
    {
        foreach (var metadata in parsed)
        {
            if (metadata is null) continue;

            foreach (var (key, value) in metadata)
            {
                if (!UserIdMetadataKeys.ContainsKey(key)) continue;
                if (value.ValueKind == JsonValueKind.String && value.TryGetGuid(out var userId))
                    yield return userId;
            }
        }
    }

    private async Task<Dictionary<Guid, string>> LoadUserNamesAsync(
        IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        return await _context.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
    }

    /// <summary>
    /// Renders raw metadata for display: user ids become names, opaque ids are dropped and the
    /// remaining keys are camelCased so the client sees one naming convention.
    /// </summary>
    private static Dictionary<string, object?>? RenderMetadata(
        Dictionary<string, JsonElement>? metadata, Dictionary<Guid, string> userNames)
    {
        if (metadata is null || metadata.Count == 0) return null;

        var rendered = new Dictionary<string, object?>();

        foreach (var (key, value) in metadata)
        {
            if (HiddenMetadataKeys.Contains(key)) continue;

            if (UserIdMetadataKeys.TryGetValue(key, out var nameKey))
            {
                // A user that no longer exists leaves the key out entirely rather than showing an id.
                if (value.ValueKind == JsonValueKind.String
                    && value.TryGetGuid(out var userId)
                    && userNames.TryGetValue(userId, out var name))
                {
                    rendered[nameKey] = name;
                }
                continue;
            }

            rendered[ToCamelCase(key)] = ToClrValue(value);
        }

        return rendered.Count == 0 ? null : rendered;
    }

    private static object? ToClrValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.TryGetInt64(out var number) ? number : value.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => value.ToString()
    };

    private static string ToCamelCase(string key) =>
        key.Length > 0 && char.IsUpper(key[0]) ? char.ToLowerInvariant(key[0]) + key[1..] : key;
}
