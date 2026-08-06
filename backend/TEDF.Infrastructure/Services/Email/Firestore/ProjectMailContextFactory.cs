using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Entities;

namespace TEDF.Infrastructure.Services.Email.Firestore;

/// <summary>
/// Reads the project, its mentor, the head of the owning department and the registered group's
/// students through the existing repositories — no separate roster or file parsing.
/// </summary>
public sealed class ProjectMailContextFactory : IProjectMailContextFactory
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMajorReadRepository _majorRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IGroupRepository _groupRepository;

    public ProjectMailContextFactory(
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IMajorReadRepository majorRepository,
        IDepartmentRepository departmentRepository,
        IGroupRepository groupRepository)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _majorRepository = majorRepository;
        _departmentRepository = departmentRepository;
        _groupRepository = groupRepository;
    }

    public async Task<ProjectMailContext?> CreateAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetWithMentorsAsync(projectId, ct);
        if (project is null) return null;

        var mentorId = project.Mentors.FirstOrDefault(m => m.IsActive)?.MentorId;
        var mentor = mentorId.HasValue ? await GetUserAsync(mentorId.Value, ct) : null;

        // Major → Department → head of department, the same trace ProjectCreatedEventHandler uses.
        var major = await _majorRepository.GetByIdAsync(project.MajorId, ct);
        var department = major is null ? null : await _departmentRepository.GetByIdAsync(major.DepartmentId, ct);
        var departmentHead = department?.HeadOfDepartmentId is Guid headId
            ? await GetUserAsync(headId, ct)
            : null;

        return new ProjectMailContext(
            ProjectId: project.Id,
            ProjectName: project.NameVi.Value,
            Round: project.EvaluationCount,
            SemesterId: project.SemesterId,
            CreatedAtUtc: project.CreatedAt,
            Mentor: mentor,
            DepartmentHead: departmentHead,
            DepartmentName: department?.Name ?? string.Empty,
            Students: await GetGroupStudentsAsync(project.GroupId, ct));
    }

    public async Task<MailRecipient?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return null;
        var user = await _userRepository.GetByIdAsync(userId, ct);
        return user is null ? null : new MailRecipient(user.Id, user.FullName, user.Email.Value);
    }

    /// <summary>
    /// Members of the group that registered the topic. Empty while a pool topic is still unclaimed —
    /// the group is only attached once a registration is confirmed.
    /// </summary>
    private async Task<IReadOnlyList<MailRecipient>> GetGroupStudentsAsync(Guid? groupId, CancellationToken ct)
    {
        if (groupId is null) return [];

        var group = await _groupRepository.GetWithMembersAsync(groupId.Value, ct);
        if (group is null) return [];

        var studentIds = group.Members.Where(m => m.IsActive).Select(m => m.StudentId).Distinct().ToList();
        if (studentIds.Count == 0) return [];

        var students = await _userRepository.GetByIdsAsync(studentIds, ct);
        return students.Select(s => new MailRecipient(s.Id, s.FullName, s.Email.Value)).ToList();
    }
}
