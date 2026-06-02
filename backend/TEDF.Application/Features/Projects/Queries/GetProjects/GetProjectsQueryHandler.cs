using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Projects.DTOs;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Project;

namespace TEDF.Application.Features.Projects.Queries.GetProjects;

public class GetProjectsQueryHandler : IQueryHandler<GetProjectsQuery, GetProjectsQueryResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IMajorReadRepository _majorRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;

    public GetProjectsQueryHandler(
        IProjectRepository projectRepository,
        ISemesterRepository semesterRepository,
        IMajorReadRepository majorRepository,
        IUserRepository userRepository,
        IGroupRepository groupRepository)
    {
        _projectRepository = projectRepository;
        _semesterRepository = semesterRepository;
        _majorRepository = majorRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
    }

    public async Task<GetProjectsQueryResult> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        // Parse status filter
        ProjectStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ProjectStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        var (projects, totalCount) = await _projectRepository.GetPagedAsync(
            request.Search, request.SemesterId, statusFilter, request.MajorId,
            page, pageSize, cancellationToken);

        var projectList = projects.ToList();

        // Build lookup maps for related entities
        var semesterIds = projectList.Select(p => p.SemesterId).Distinct().ToList();
        var majorIds = projectList.Select(p => p.MajorId).Distinct().ToList();
        var mentorIds = projectList.SelectMany(p => p.Mentors.Where(m => m.IsActive).Select(m => m.MentorId)).Distinct().ToList();
        var groupIds = projectList.Where(p => p.GroupId.HasValue).Select(p => p.GroupId!.Value).Distinct().ToList();

        // Load semesters
        var semesters = await _semesterRepository.GetAllAsync(cancellationToken);
        var semesterMap = semesters.ToDictionary(s => s.Id, s => s.Name);

        // Load majors
        var majorMap = new Dictionary<int, (string Name, string Code)>();
        foreach (var majorId in majorIds)
        {
            var major = await _majorRepository.GetByIdAsync(majorId, cancellationToken);
            if (major != null)
                majorMap[majorId] = (major.Name, major.Code);
        }

        // Load mentor users
        var mentorUsers = (await _userRepository.GetByIdsAsync(mentorIds, cancellationToken)).ToList();
        var mentorMap = mentorUsers.ToDictionary(u => u.Id, u => u.DisplayName);

        // Load groups with members for student names
        var groupMap = new Dictionary<Guid, (string? Code, List<Guid> StudentIds)>();
        foreach (var groupId in groupIds)
        {
            var group = await _groupRepository.GetWithMembersAsync(groupId, cancellationToken);
            if (group != null)
            {
                var activeStudentIds = group.Members
                    .Where(m => m.IsActive)
                    .Select(m => m.StudentId)
                    .ToList();
                groupMap[groupId] = (group.Code.Value, activeStudentIds);
            }
        }

        // Load student users
        var allStudentIds = groupMap.Values.SelectMany(g => g.StudentIds).Distinct().ToList();
        var studentUsers = allStudentIds.Count > 0
            ? (await _userRepository.GetByIdsAsync(allStudentIds, cancellationToken)).ToList()
            : [];
        var studentMap = studentUsers.ToDictionary(u => u.Id, u => u.FullName);

        // Map to DTOs
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
}
