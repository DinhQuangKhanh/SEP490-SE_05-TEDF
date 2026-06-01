using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IMongoCollection<MessageDocument> _collection;

        public MessageRepository(MongoDbContext context)
        {
            _collection = context.GetCollection<MessageDocument>(MongoDbContext.Collections.Messages);
        }

        public async Task<MessageDocument> AddAsync(MessageDocument message, CancellationToken ct = default)
        {
            await _collection.InsertOneAsync(message, cancellationToken: ct);
            return message;
        }

        public async Task<IEnumerable<MessageDocument>> GetByConversationIdAsync(Guid conversationId, int limit = 50, DateTime? before = null, CancellationToken ct = default)
        {
            var filter = Builders<MessageDocument>.Filter.Eq(m => m.ConversationId, conversationId) & Builders<MessageDocument>.Filter.Eq(m => m.IsDeleted, false);
            if (before.HasValue) filter &= Builders<MessageDocument>.Filter.Lt(m => m.CreatedAt, before.Value);
            return await _collection.Find(filter).SortByDescending(m => m.CreatedAt).Limit(limit).ToListAsync(ct);
        }

        public async Task MarkAsReadAsync(Guid messageId, Guid userId, CancellationToken ct = default)
            => await _collection.UpdateOneAsync(m => m.Id == messageId && !m.ReadByUserIds.Contains(userId), Builders<MessageDocument>.Update.AddToSet(m => m.ReadByUserIds, userId), cancellationToken: ct);

        public async Task DeleteAsync(Guid messageId, CancellationToken ct = default)
            => await _collection.UpdateOneAsync(m => m.Id == messageId, Builders<MessageDocument>.Update.Set(m => m.IsDeleted, true), cancellationToken: ct);
    }
}
