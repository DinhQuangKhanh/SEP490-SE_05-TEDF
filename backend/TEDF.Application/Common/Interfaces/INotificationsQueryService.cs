using TEDF.Application.Features.Notifications.DTOs;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-side service for the Notifications feature. Query handlers depend on this only.
/// </summary>
public interface INotificationsQueryService
{
    Task<NotificationListResponseDto> GetUserNotificationsAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task<long> GetUnreadCountAsync(Guid userId, NotificationCategory? category, CancellationToken cancellationToken = default);
}
