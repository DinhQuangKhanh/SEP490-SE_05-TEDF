using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly IMongoCollection<ConversationDocument> _collection;

        public ConversationRepository(MongoDbContext context)
        {
            _collection = context.GetCollection<ConversationDocument>(MongoDbContext.Collections.Conversations);
        }

        public async Task<ConversationDocument> AddAsync(ConversationDocument conversation, CancellationToken ct = default)
        {
            await _collection.InsertOneAsync(conversation, cancellationToken: ct);
            return conversation;
        }

        public async Task<ConversationDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _collection.Find(c => c.Id == id).FirstOrDefaultAsync(ct);

        public async Task<IEnumerable<ConversationDocument>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
            => await _collection.Find(c => c.ParticipantIds.Contains(userId)).SortByDescending(c => c.LastMessageAt).ToListAsync(ct);

        public async Task<ConversationDocument?> GetPrivateConversationAsync(Guid user1Id, Guid user2Id, CancellationToken ct = default)
            => await _collection.Find(c => c.Type == Domain.Enums.Message.ConversationType.Private && c.ParticipantIds.Contains(user1Id) && c.ParticipantIds.Contains(user2Id)).FirstOrDefaultAsync(ct);

        public async Task UpdateLastMessageAsync(Guid conversationId, Guid messageId, DateTime messageTime, CancellationToken ct = default)
            => await _collection.UpdateOneAsync(c => c.Id == conversationId, Builders<ConversationDocument>.Update.Set(c => c.LastMessageId, messageId).Set(c => c.LastMessageAt, messageTime).Set(c => c.UpdatedAt, DateTime.UtcNow), cancellationToken: ct);
    }
}
