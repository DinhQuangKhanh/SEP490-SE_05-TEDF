using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.ProjectAggregate;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Project;

namespace TEDF.Application.Features.Mentor.Commands.MentorResubmitPoolTopic;

public sealed class MentorResubmitPoolTopicCommandHandler(
    IProjectRepository projectRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<MentorResubmitPoolTopicCommand>
{
    public async Task<Unit> Handle(MentorResubmitPoolTopicCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = currentUser.UserId.Value;

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.ProjectId);

        // Only pool-based topics can be resubmitted by mentor through this endpoint
        if (project.SourceType != ProjectSourceType.FromPool)
            throw new BusinessRuleValidationException(
                "Chỉ đề tài trong kho mới có thể được giảng viên gửi thẩm định qua chức năng này.");

        // First submission or resubmission
        if (project.Status == ProjectStatus.Draft && project.EvaluationCount == 0)
        {
            // First time: submit for evaluation
            project.SubmitForEvaluation(userId);
        }
        else
        {
            // Resubmission after NeedsModification (or Draft with previous evaluation)
            project.Resubmit(userId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
