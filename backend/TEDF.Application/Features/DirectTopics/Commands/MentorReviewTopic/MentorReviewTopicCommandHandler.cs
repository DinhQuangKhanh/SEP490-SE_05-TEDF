using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.DirectTopics.Commands.MentorReviewTopic;

public class MentorReviewProposedTopicCommandHandler : ICommandHandler<MentorReviewProposedTopicCommand>
{
    private readonly IDirectTopicsDomainService _directTopics;
    private readonly ICurrentUserService _currentUser;

    public MentorReviewProposedTopicCommandHandler(IDirectTopicsDomainService directTopics, ICurrentUserService currentUser)
    {
        _directTopics = directTopics;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MentorReviewProposedTopicCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        await _directTopics.MentorReviewAsync(request.ProjectId, userId, request.Action, request.Feedback, cancellationToken);
        return Unit.Value;
    }
}
