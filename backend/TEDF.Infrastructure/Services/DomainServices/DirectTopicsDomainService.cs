using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.GroupAggregate.Entities;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Rules;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Services;
using ISystemSettingsService = TEDF.Application.Common.Interfaces.ISystemSettingsService;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the DirectTopics feature. See <see cref="IDirectTopicsDomainService"/>.
/// </summary>
public class DirectTopicsDomainService : IDirectTopicsDomainService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly ITopicRegistrationRepository _registrationRepository;
    private readonly ISemestersDomainService _semesters;
    private readonly IProjectsDomainService _projects;
    private readonly ISystemSettingsService _settings;
    private readonly IUnitOfWork _unitOfWork;

    public DirectTopicsDomainService(
        IProjectRepository projectRepository,
        IGroupRepository groupRepository,
        ISemesterRepository semesterRepository,
        ITopicRegistrationRepository registrationRepository,
        ISemestersDomainService semesters,
        IProjectsDomainService projects,
        ISystemSettingsService settings,
        IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _projectRepository = projectRepository;
        _groupRepository = groupRepository;
        _semesterRepository = semesterRepository;
        _registrationRepository = registrationRepository;
        _semesters = semesters;
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateDirectTopicAsync(Guid createdBy, Guid groupId, Guid mentorId, int majorId, DirectTopicContent content, CancellationToken ct = default)
    {
        // Gate: students may only self-propose topics when the admin has enabled it.
        if (!await _settings.GetBoolAsync(SettingKeys.AllowDirectRegistration, true, ct))
            throw new BusinessRuleValidationException("Tính năng sinh viên tự đề xuất đề tài hiện đang bị tắt.");

        var group = await _groupRepository.GetWithJoinRequestsAndInvitationsAsync(groupId, ct)
            ?? throw new EntityNotFoundException(nameof(Group), groupId);

        if (group.ProjectId.HasValue)
            throw new BusinessRuleValidationException("Nhóm đã có đề tài, không thể đề xuất thêm.");

        // The topic belongs to the group's own semester (the thesis term), which is the source of
        // truth for both capacity and the created project — not "active + 1".
        var semesterId = group.SemesterId;

        // A student may only propose a topic in the major they study this semester (eligible-student roster).
        // The form disables the major field; this guards against a tampered request.
        var studentMajorId = await _semesterRepository.GetEligibleStudentMajorAsync(createdBy, semesterId, ct)
            ?? throw new BusinessRuleValidationException("Bạn chưa được gán chuyên ngành trong học kỳ này.");
        if (majorId != studentMajorId)
            throw new BusinessRuleValidationException("Đề tài phải thuộc đúng chuyên ngành bạn đang theo học.");

        // Capacity includes pending pool registrations (each reserves a future supervised group).
        var mentorGroupCount =
            await _projectRepository.CountMentorActiveProjectsInSemesterAsync(mentorId, semesterId, ct)
            + await _registrationRepository.CountPendingByMentorIdAsync(mentorId, ct);
        // Reserve one slot for the mentor's own pool-proposed topics: a student may pick this mentor
        // for a direct topic only while they stay at least one group below the per-semester cap
        // (e.g. blocked once already at 3 of 4). The +1 is that reserved slot.
        if (new MentorCannotExceedMaxGroupsPerSemesterRule(mentorGroupCount + 1).IsBroken())
            throw new BusinessRuleValidationException(
                $"Giảng viên đã đủ số nhóm hướng dẫn cho học kỳ này (tối đa {MentorCannotExceedMaxGroupsPerSemesterRule.MaxGroupsPerSemester} nhóm, trong đó phải để dành 1 suất cho đề tài trong kho đề tài chung).");

        var code = await _projects.GenerateProjectCodeAsync(semesterId, majorId, ct);

        var project = Project.CreateDirect(
            code,
            ProjectName.Create(content.NameVi),
            ProjectName.Create(content.NameEn),
            content.NameAbbr,
            content.Description,
            content.Objectives,
            content.Scope,
            content.Technologies != null ? TechnologyStack.Create(content.Technologies) : null,
            content.ExpectedResults,
            majorId,
            semesterId,
            // "Max students" is the group's member cap (default 5); the form no longer asks for it.
            group.MaxMembers,
            groupId: groupId);

        project.AddMentor(mentorId, createdBy);
        group.AssignProject(project.Id);

        await _projectRepository.AddAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return project.Id;
    }

    public async Task UpdateDirectTopicAsync(Guid projectId, DirectTopicContent content, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, ct)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        if (project.Status != ProjectStatus.Draft && project.Status != ProjectStatus.NeedsModification)
            throw new BusinessRuleValidationException("Topic can only be edited in Draft or NeedsModification status.");

        project.UpdateBasicInfo(
            nameVi: ProjectName.Create(content.NameVi),
            nameEn: ProjectName.Create(content.NameEn),
            nameAbbr: content.NameAbbr,
            description: content.Description,
            objectives: content.Objectives,
            scope: content.Scope,
            technologies: content.Technologies,
            expectedResults: content.ExpectedResults);

        // Only touch the member cap when the caller actually supplied one; the student edit
        // form omits it, and a missing value must not overwrite the cap with the default 0.
        if (content.MaxStudents.HasValue)
            project.SetMaxStudents(content.MaxStudents.Value);

        _projectRepository.Update(project);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task SubmitToMentorAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetWithMentorsAsync(projectId, ct)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        if (!project.GroupId.HasValue)
            throw new BusinessRuleValidationException("Đề tài chưa được gán cho nhóm nào.");

        var group = await _groupRepository.GetByIdAsync(project.GroupId.Value, ct)
                    ?? throw new EntityNotFoundException(nameof(Group), project.GroupId.Value);

        switch (project.Status)
        {
            case ProjectStatus.Draft:
                project.SubmitToMentor(userId);
                break;
            case ProjectStatus.NeedsModification:
                project.ResubmitToMentor(userId);
                break;
            default:
                throw new BusinessRuleValidationException("Đề tài không ở trạng thái có thể gửi cho giảng viên.");
        }

        var groupJoinRequestToReject = group.JoinRequests.Where(j => j.IsPending);
        var groupInvitationToReject = group.Invitations.Where(i => i.IsPending);
        var groupLeader = group.Leader;

        foreach (var groupJoinRequest in groupJoinRequestToReject)
        {
            group.RejectJoinRequest(groupJoinRequest.Id, groupLeader!.StudentId);
        }

        foreach (var groupInvitation in groupInvitationToReject)
        {
            group.RejectInvitation(groupInvitation.Id, groupInvitation.InviteeId);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task MentorReviewAsync(Guid projectId, Guid mentorUserId, string action, string? feedback, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetWithMentorsAsync(projectId, ct)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        switch (action.ToLowerInvariant())
        {
            case "approve":
                // A lecturer may only take on supervising if assigned to mentor this project's semester.
                if (!await _semesters.IsMentorAssignedAsync(mentorUserId, project.SemesterId, ct))
                    throw new BusinessRuleValidationException(
                        "Bạn chưa được phân công làm giảng viên hướng dẫn trong học kỳ này nên không thể duyệt đề tài.");
                project.MentorApproveAndSubmit(mentorUserId);
                break;
            case "requestmodification":
                project.MentorRequestModification(feedback);
                break;
            default:
                throw new BusinessRuleValidationException(
                    $"Hành động không hợp lệ: {action}. Chỉ chấp nhận 'approve' hoặc 'requestModification'.");
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
