using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Notifications.DTOs;

namespace TEDF.Application.Features.Notifications.Queries.GetUserNotifications;

public class GetUserNotificationsQueryHandler : IQueryHandler<GetUserNotificationsQuery, NotificationListResponseDto>
{
    private readonly INotificationsQueryService _notifications;
    private readonly ICurrentUserService _currentUser;

    public GetUserNotificationsQueryHandler(INotificationsQueryService notifications, ICurrentUserService currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public Task<NotificationListResponseDto> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _notifications.GetUserNotificationsAsync(_currentUser.UserId.Value, request.Limit, cancellationToken);
    }
}
