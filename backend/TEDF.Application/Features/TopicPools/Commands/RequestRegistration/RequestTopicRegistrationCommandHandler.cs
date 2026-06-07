using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.TopicPools.Commands.RequestRegistration;

/// <summary>
/// Handles RequestTopicRegistrationCommand.
/// Delegates business logic to ITopicPoolsDomainService and returns the new registration ID.
/// </summary>
public class RequestTopicRegistrationCommandHandler
    : ICommandHandler<RequestTopicRegistrationCommand, Guid>
{
    private readonly ITopicPoolsDomainService _domainService;
    private readonly ICurrentUserService _currentUser;

    public RequestTopicRegistrationCommandHandler(
        ITopicPoolsDomainService domainService,
        ICurrentUserService currentUser)
    {
        _domainService = domainService;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        RequestTopicRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var registration = await _domainService.RequestRegistrationAsync(
            projectId: request.ProjectId,
            groupId: request.GroupId,
            registeredBy: _currentUser.UserId.Value,
            note: request.Note,
            cancellationToken: cancellationToken);

        return registration.Id;
    }
}
