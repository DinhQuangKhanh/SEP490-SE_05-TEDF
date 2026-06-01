using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Application.Features.Notifications.Commands.MarkAllAsRead;

/// <summary>
/// Handles MarkAllNotificationsAsReadCommand by marking all notifications
/// as read for the current authenticated user via INotificationService.
/// </summary>
public class MarkAllNotificationsAsReadCommandHandler
    : ICommandHandler<MarkAllNotificationsAsReadCommand>
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public MarkAllNotificationsAsReadCommandHandler(
        INotificationService notificationService,
        ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(
        MarkAllNotificationsAsReadCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _notificationService.MarkAllAsReadAsync(
            _currentUser.UserId.Value, cancellationToken);

        return Unit.Value;
    }
}
