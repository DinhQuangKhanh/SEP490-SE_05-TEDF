namespace TEDF.Domain.Services;

/// <summary>
/// Write-side service for the Notifications feature. Command handlers depend on this only.
/// </summary>
public interface INotificationsDomainService
{
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
