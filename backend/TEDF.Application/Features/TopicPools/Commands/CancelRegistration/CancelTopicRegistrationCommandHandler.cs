using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.TopicPools.Commands.CancelRegistration;

/// <summary>
/// Handles <see cref="CancelTopicRegistrationCommand"/>.
/// Cancels the registration and (if no other pending registrations remain) resets the topic back to Available.
/// Authorization (leader-of-group) is enforced in the domain service.
/// </summary>
public class CancelTopicRegistrationCommandHandler
    : ICommandHandler<CancelTopicRegistrationCommand>
{
    private readonly ITopicRegistrationService _domainService;
    private readonly ICurrentUserService _currentUser;

    public CancelTopicRegistrationCommandHandler(
        ITopicRegistrationService domainService,
        ICurrentUserService currentUser)
    {
        _domainService = domainService;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(
        CancelTopicRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _domainService.CancelRegistrationAsync(
            registrationId: request.RegistrationId,
            cancelledBy: _currentUser.UserId.Value,
            reason: request.Reason,
            cancellationToken: cancellationToken);

        return Unit.Value;
    }
}
