using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces
{
    public interface IMessageRepository
    {
        Task<MessageDocument> AddAsync(MessageDocument message, CancellationToken ct = default);
        Task<IEnumerable<MessageDocument>> GetByConversationIdAsync(Guid conversationId, int limit = 50, DateTime? before = null, CancellationToken ct = default);
        Task MarkAsReadAsync(Guid messageId, Guid userId, CancellationToken ct = default);
        Task DeleteAsync(Guid messageId, CancellationToken ct = default);
    }
}
