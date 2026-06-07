using TEDF.Application.Common;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Rules;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Aggregates.SemesterAggregate;
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
    private readonly ISystemSettingsService _settings;
    private readonly IUnitOfWork _unitOfWork;

    public DirectTopicsDomainService(
        IProjectRepository projectRepository,
        IGroupRepository groupRepository,
        ISemesterRepository semesterRepository,
        ISystemSettingsService settings,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _groupRepository = groupRepository;
        _semesterRepository = semesterRepository;
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateDirectTopicAsync(Guid createdBy, Guid groupId, Guid mentorId, int majorId, DirectTopicContent content, CancellationToken ct = default)
    {
        // Gate: students may only self-propose topics when the admin has enabled it.
        if (!await _settings.GetBoolAsync(SettingKeys.AllowDirectRegistration, true, ct))
            throw new BusinessRuleValidationException("Tính năng sinh viên tự đề xuất đề tài hiện đang bị tắt.");

        var group = await _groupRepository.GetWithMembersAsync(groupId, ct)
            ?? throw new EntityNotFoundException(nameof(Group), groupId);

        if (group.ProjectId.HasValue)
            throw new BusinessRuleValidationException("Nhóm đã có đề tài, không thể đề xuất thêm.");

        var activeSemester = await _semesterRepository.GetActiveAsync(ct)
            ?? throw new BusinessRuleValidationException("Không tìm thấy học kỳ đang hoạt động.");

        var nextSemester = await _semesterRepository.GetSemesterAfterAsync(activeSemester.Id, 1, ct)
            ?? throw new BusinessRuleValidationException("Không tìm thấy học kỳ kế tiếp.");

        var mentorGroupCount = await _projectRepository.CountMentorActiveProjectsInSemesterAsync(mentorId, nextSemester.Id, ct);
        if (new MentorCannotExceedMaxGroupsPerSemesterRule(mentorGroupCount).IsBroken())
            throw new BusinessRuleValidationException(
                new MentorCannotExceedMaxGroupsPerSemesterRule(mentorGroupCount).Message);

        var year = DateTime.UtcNow.Year;
        var seq = await _projectRepository.GetNextSequenceAsync(year, ct);
        var code = ProjectCode.Generate(year, seq);

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
            nextSemester.Id,
            content.MaxStudents,
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

        project.SetMaxStudents(content.MaxStudents);

        _projectRepository.Update(project);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task SubmitToMentorAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetWithMentorsAsync(projectId, ct)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        if (!project.GroupId.HasValue)
            throw new BusinessRuleValidationException("Đề tài chưa được gán cho nhóm nào.");

        if (project.Status == ProjectStatus.Draft)
            project.SubmitToMentor(userId);
        else if (project.Status == ProjectStatus.NeedsModification)
            project.ResubmitToMentor(userId);
        else
            throw new BusinessRuleValidationException("Đề tài không ở trạng thái có thể gửi cho giảng viên.");

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task MentorReviewAsync(Guid projectId, Guid mentorUserId, string action, string? feedback, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetWithMentorsAsync(projectId, ct)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        switch (action.ToLowerInvariant())
        {
            case "approve":
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
