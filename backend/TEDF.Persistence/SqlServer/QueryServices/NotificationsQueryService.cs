using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Notifications.DTOs;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// Read-side service for the Notifications feature; wraps the Mongo-backed <see cref="INotificationService"/>.
/// </summary>
public class NotificationsQueryService : INotificationsQueryService
{
    private readonly INotificationService _notifications;

    public NotificationsQueryService(INotificationService notifications) => _notifications = notifications;

    public async Task<NotificationListResponseDto> GetUserNotificationsAsync(Guid userId, int limit, CancellationToken cancellationToken = default)
    {
        var items = await _notifications.GetUserNotificationsAsync(userId, limit, cancellationToken);
        var unreadCount = await _notifications.GetUnreadCountAsync(userId, ct: cancellationToken);

        var itemList = items.ToList();

        return new NotificationListResponseDto
        {
            Items = itemList,
            TotalCount = itemList.Count,
            UnreadCount = unreadCount
        };
    }

    public Task<long> GetUnreadCountAsync(Guid userId, NotificationCategory? category, CancellationToken cancellationToken = default)
        => _notifications.GetUnreadCountAsync(userId, category, cancellationToken);
}
