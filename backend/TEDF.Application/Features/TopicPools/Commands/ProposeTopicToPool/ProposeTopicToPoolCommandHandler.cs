using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.TopicPools.Commands.ProposeTopicToPool;

/// <summary>
/// Handles ProposeTopicToPoolCommand by delegating to <see cref="ITopicPoolsDomainService"/>.
/// </summary>
public class ProposeTopicToPoolCommandHandler : ICommandHandler<ProposeTopicToPoolCommand, Guid>
{
    private readonly ITopicPoolsDomainService _topicPools;
    private readonly ICurrentUserService _currentUser;

    public ProposeTopicToPoolCommandHandler(ITopicPoolsDomainService topicPools, ICurrentUserService currentUser)
    {
        _topicPools = topicPools;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(ProposeTopicToPoolCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _topicPools.ProposeTopicAsync(
            request.PoolId,
            _currentUser.UserId.Value,
            new PoolTopicContent(
                request.NameVi, request.NameEn, request.NameAbbr, request.Description,
                request.Objectives, request.Scope, request.Technologies, request.ExpectedResults, request.MaxStudents),
            cancellationToken);
    }
}
