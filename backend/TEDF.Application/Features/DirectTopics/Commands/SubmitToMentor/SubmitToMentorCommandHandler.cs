using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Project;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.DirectTopics.Commands.SubmitToMentor;

public class SubmitToMentorCommandHandler : ICommandHandler<SubmitToMentorCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SubmitToMentorCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SubmitToMentorCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var project = await _projectRepository.GetWithMentorsAsync(request.ProjectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.ProjectId);

        // Validate: user must be leader of the group
        if (!project.GroupId.HasValue)
            throw new BusinessRuleValidationException("Đề tài chưa được gán cho nhóm nào.");

        // Submit based on current status
        if (project.Status == ProjectStatus.Draft)
            project.SubmitToMentor(userId);
        else if (project.Status == ProjectStatus.NeedsModification)
            project.ResubmitToMentor(userId);
        else
            throw new BusinessRuleValidationException("Đề tài không ở trạng thái có thể gửi cho giảng viên.");

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
