using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.TopicPools.Commands.RejectRegistration;

/// <summary>
/// Handles RejectTopicRegistrationCommand.
/// Rejects the registration and (if no other pending registrations remain) resets the topic back to Available.
/// </summary>
public class RejectTopicRegistrationCommandHandler
    : ICommandHandler<RejectTopicRegistrationCommand>
{
    private readonly ITopicRegistrationService _domainService;
    private readonly ICurrentUserService _currentUser;

    public RejectTopicRegistrationCommandHandler(
        ITopicRegistrationService domainService,
        ICurrentUserService currentUser)
    {
        _domainService = domainService;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(
        RejectTopicRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _domainService.RejectRegistrationAsync(
            registrationId: request.RegistrationId,
            rejectedBy: _currentUser.UserId.Value,
            reason: request.Reason,
            cancellationToken: cancellationToken);

        return Unit.Value;
    }
}
