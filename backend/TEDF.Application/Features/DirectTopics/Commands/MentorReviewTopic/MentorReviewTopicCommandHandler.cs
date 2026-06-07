using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.DirectTopics.Commands.MentorReviewTopic;

public class MentorReviewProposedTopicCommandHandler : ICommandHandler<MentorReviewProposedTopicCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public MentorReviewProposedTopicCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MentorReviewProposedTopicCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var project = await _projectRepository.GetWithMentorsAsync(request.ProjectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.ProjectId);

        switch (request.Action.ToLowerInvariant())
        {
            case "approve":
                project.MentorApproveAndSubmit(userId);
                break;
            case "requestmodification":
                project.MentorRequestModification(request.Feedback);
                break;
            default:
                throw new BusinessRuleValidationException($"Hành động không hợp lệ: {request.Action}. Chỉ chấp nhận 'approve' hoặc 'requestModification'.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
