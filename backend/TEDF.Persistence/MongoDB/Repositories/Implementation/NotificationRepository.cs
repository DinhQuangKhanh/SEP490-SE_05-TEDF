using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IMongoCollection<NotificationDocument> _collection;

        public NotificationRepository(MongoDbContext context)
        {
            _collection = context.GetCollection<NotificationDocument>(MongoDbContext.Collections.Notifications);
        }

        public async Task AddAsync(NotificationDocument notification, CancellationToken ct = default)
            => await _collection.InsertOneAsync(notification, cancellationToken: ct);

        public async Task AddManyAsync(IEnumerable<NotificationDocument> notifications, CancellationToken ct = default)
            => await _collection.InsertManyAsync(notifications, cancellationToken: ct);

        public async Task<IEnumerable<NotificationDocument>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken ct = default)
            => await _collection.Find(n => n.UserId == userId).SortByDescending(n => n.CreatedAt).Limit(limit).ToListAsync(ct);

        public async Task<IEnumerable<NotificationDocument>> GetUnreadByUserIdAsync(Guid userId, CancellationToken ct = default)
            => await _collection.Find(n => n.UserId == userId && !n.IsRead).SortByDescending(n => n.CreatedAt).ToListAsync(ct);

        public async Task<long> GetUnreadCountAsync(Guid userId, TEDF.Domain.Enums.Notification.NotificationCategory? category = null, CancellationToken ct = default)
        {
            var filter = Builders<NotificationDocument>.Filter.Eq(n => n.UserId, userId) & 
                         Builders<NotificationDocument>.Filter.Eq(n => n.IsRead, false);
            
            if (category.HasValue)
            {
                filter &= Builders<NotificationDocument>.Filter.Eq(n => n.Category, category.Value);
            }

            return await _collection.CountDocumentsAsync(filter, cancellationToken: ct);
        }

        public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
            => await _collection.UpdateOneAsync(n => n.Id == notificationId, Builders<NotificationDocument>.Update.Set(n => n.IsRead, true).Set(n => n.ReadAt, DateTime.UtcNow), cancellationToken: ct);

        public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
            => await _collection.UpdateManyAsync(n => n.UserId == userId && !n.IsRead, Builders<NotificationDocument>.Update.Set(n => n.IsRead, true).Set(n => n.ReadAt, DateTime.UtcNow), cancellationToken: ct);
    }
}
