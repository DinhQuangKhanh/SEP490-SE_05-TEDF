using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;
using TEDF.Infrastructure.RealTime.Services;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the Notifications feature; wraps the Mongo-backed <see cref="INotificationService"/>.
/// </summary>
public class NotificationsDomainService : INotificationsDomainService
{
    private readonly INotificationService _notifications;
    private readonly IRealtimeNotificationService _realtime;

    public NotificationsDomainService(INotificationService notifications, IRealtimeNotificationService realtime)
    {
        _notifications = notifications;
        _realtime = realtime;
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _notifications.MarkAsReadAsync(notificationId, cancellationToken);
        await PushUnreadCountAsync(userId, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _notifications.MarkAllAsReadAsync(userId, cancellationToken);
        await PushUnreadCountAsync(userId, cancellationToken);
    }

    private async Task PushUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var count = await _notifications.GetUnreadCountAsync(userId, category: null, cancellationToken);
        await _realtime.SendUnreadCountUpdateAsync(userId, count, cancellationToken);
    }
}
