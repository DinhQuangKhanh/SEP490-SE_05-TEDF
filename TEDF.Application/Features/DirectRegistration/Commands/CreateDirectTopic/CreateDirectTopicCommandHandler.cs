using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.GroupAggregate;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Rules;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;

namespace TEDF.Application.Features.DirectRegistration.Commands.CreateDirectTopic;

public class CreateDirectTopicCommandHandler : ICommandHandler<CreateDirectTopicCommand, Guid>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateDirectTopicCommandHandler(
        IProjectRepository projectRepository,
        IGroupRepository groupRepository,
        ISemesterRepository semesterRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _projectRepository = projectRepository;
        _groupRepository = groupRepository;
        _semesterRepository = semesterRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateDirectTopicCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        // Validate: group must not already have a project
        var group = await _groupRepository.GetWithMembersAsync(request.GroupId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Group), request.GroupId);

        if (group.ProjectId.HasValue)
            throw new BusinessRuleValidationException("Nhóm đã có đề tài, không thể đề xuất thêm.");

        // Get active semester
        var activeSemester = await _semesterRepository.GetActiveAsync(cancellationToken)
            ?? throw new BusinessRuleValidationException("Không tìm thấy học kỳ đang hoạt động.");

        // Get next semester (groups register for next semester)
        var nextSemester = await _semesterRepository.GetSemesterAfterAsync(activeSemester.Id, 1, cancellationToken)
            ?? throw new BusinessRuleValidationException("Không tìm thấy học kỳ kế tiếp.");

        // Validate: mentor capacity
        var mentorGroupCount = await _projectRepository.CountMentorActiveProjectsInSemesterAsync(
            request.MentorId, nextSemester.Id, cancellationToken);

        if (new MentorCannotExceedMaxGroupsPerSemesterRule(mentorGroupCount).IsBroken())
            throw new BusinessRuleValidationException(
                new MentorCannotExceedMaxGroupsPerSemesterRule(mentorGroupCount).Message);

        // Generate project code
        var year = DateTime.UtcNow.Year;
        var seq = await _projectRepository.GetNextSequenceAsync(year, cancellationToken);
        var code = ProjectCode.Generate(year, seq);

        // Create project
        var project = Project.CreateDirect(
            code,
            ProjectName.Create(request.NameVi),
            ProjectName.Create(request.NameEn),
            request.NameAbbr,
            request.Description,
            request.Objectives,
            request.Scope,
            request.Technologies != null ? TechnologyStack.Create(request.Technologies) : null,
            request.ExpectedResults,
            request.MajorId,
            nextSemester.Id,
            request.MaxStudents,
            groupId: request.GroupId);

        // Add mentor
        project.AddMentor(request.MentorId, userId);

        // Assign project to group
        group.AssignProject(project.Id);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
