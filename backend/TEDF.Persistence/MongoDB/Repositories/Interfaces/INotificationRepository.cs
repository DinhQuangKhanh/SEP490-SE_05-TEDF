using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task AddAsync(NotificationDocument notification, CancellationToken ct = default);
        Task AddManyAsync(IEnumerable<NotificationDocument> notifications, CancellationToken ct = default);
        Task<IEnumerable<NotificationDocument>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken ct = default);
        Task<IEnumerable<NotificationDocument>> GetUnreadByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<long> GetUnreadCountAsync(Guid userId, TEDF.Domain.Enums.Notification.NotificationCategory? category = null, CancellationToken ct = default);
        Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
        Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    }
}
