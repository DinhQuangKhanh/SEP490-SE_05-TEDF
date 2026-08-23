using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.TopicPools.Commands.ConfirmRegistration;

/// <summary>
/// Handles ConfirmTopicRegistrationCommand.
/// Confirms the registration and assigns the group to the project.
/// </summary>
public class ConfirmTopicRegistrationCommandHandler
    : ICommandHandler<ConfirmTopicRegistrationCommand>
{
    private readonly ITopicRegistrationService _domainService;
    private readonly ICurrentUserService _currentUser;

    public ConfirmTopicRegistrationCommandHandler(
        ITopicRegistrationService domainService,
        ICurrentUserService currentUser)
    {
        _domainService = domainService;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(
        ConfirmTopicRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _domainService.ConfirmRegistrationAsync(
            registrationId: request.RegistrationId,
            confirmedBy: _currentUser.UserId.Value,
            cancellationToken: cancellationToken);

        return Unit.Value;
    }
}
