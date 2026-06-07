using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.DirectTopics.Commands.SubmitToMentor;

public class SubmitToMentorCommandHandler : ICommandHandler<SubmitToMentorCommand>
{
    private readonly IDirectTopicsDomainService _directTopics;
    private readonly ICurrentUserService _currentUser;

    public SubmitToMentorCommandHandler(IDirectTopicsDomainService directTopics, ICurrentUserService currentUser)
    {
        _directTopics = directTopics;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SubmitToMentorCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        await _directTopics.SubmitToMentorAsync(request.ProjectId, userId, cancellationToken);
        return Unit.Value;
    }
}
