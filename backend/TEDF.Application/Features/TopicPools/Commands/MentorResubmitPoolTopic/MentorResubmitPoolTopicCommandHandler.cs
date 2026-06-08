using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.TopicPools.Commands.MentorResubmitPoolTopic;

public sealed class MentorResubmitPoolTopicCommandHandler(
    ITopicPoolsDomainService topicPools,
    ICurrentUserService currentUser)
    : ICommandHandler<MentorResubmitPoolTopicCommand>
{
    public async Task<Unit> Handle(MentorResubmitPoolTopicCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await topicPools.ResubmitPoolTopicAsync(request.ProjectId, currentUser.UserId.Value, cancellationToken);
        return Unit.Value;
    }
}
