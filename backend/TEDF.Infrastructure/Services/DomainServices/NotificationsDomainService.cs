using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the Notifications feature; wraps the Mongo-backed <see cref="INotificationService"/>.
/// </summary>
public class NotificationsDomainService : INotificationsDomainService
{
    private readonly INotificationService _notifications;

    public NotificationsDomainService(INotificationService notifications) => _notifications = notifications;

    public Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => _notifications.MarkAsReadAsync(notificationId, cancellationToken);

    public Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        => _notifications.MarkAllAsReadAsync(userId, cancellationToken);
}
