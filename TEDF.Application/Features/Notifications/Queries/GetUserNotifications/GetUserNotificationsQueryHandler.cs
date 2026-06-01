using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Notifications.DTOs;

namespace TEDF.Application.Features.Notifications.Queries.GetUserNotifications;

/// <summary>
/// Handles GetUserNotificationsQuery by fetching notifications from MongoDB
/// via INotificationService for the current authenticated user.
/// </summary>
public class GetUserNotificationsQueryHandler
    : IQueryHandler<GetUserNotificationsQuery, NotificationListResponseDto>
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public GetUserNotificationsQueryHandler(
        INotificationService notificationService,
        ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public async Task<NotificationListResponseDto> Handle(
        GetUserNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = _currentUser.UserId.Value;

        var items = await _notificationService.GetUserNotificationsAsync(userId, request.Limit, cancellationToken);
        var unreadCount = await _notificationService.GetUnreadCountAsync(userId, ct: cancellationToken);

        var itemList = items.ToList();

        return new NotificationListResponseDto
        {
            Items = itemList,
            TotalCount = itemList.Count,
            UnreadCount = unreadCount
        };
    }
}
