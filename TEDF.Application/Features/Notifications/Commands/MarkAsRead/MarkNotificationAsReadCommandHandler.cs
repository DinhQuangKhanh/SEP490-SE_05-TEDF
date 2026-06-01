using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Application.Features.Notifications.Commands.MarkAsRead;

/// <summary>
/// Handles MarkNotificationAsReadCommand by marking the specified notification
/// as read via INotificationService.
/// </summary>
public class MarkNotificationAsReadCommandHandler
    : ICommandHandler<MarkNotificationAsReadCommand>
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationAsReadCommandHandler(
        INotificationService notificationService,
        ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(
        MarkNotificationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _notificationService.MarkAsReadAsync(request.NotificationId, cancellationToken);

        return Unit.Value;
    }
}
