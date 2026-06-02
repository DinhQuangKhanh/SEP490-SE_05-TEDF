using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces
{
    public interface IConversationRepository
    {
        Task<ConversationDocument> AddAsync(ConversationDocument conversation, CancellationToken ct = default);
        Task<ConversationDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<ConversationDocument>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<ConversationDocument?> GetPrivateConversationAsync(Guid user1Id, Guid user2Id, CancellationToken ct = default);
        Task UpdateLastMessageAsync(Guid conversationId, Guid messageId, DateTime messageTime, CancellationToken ct = default);
    }
}
