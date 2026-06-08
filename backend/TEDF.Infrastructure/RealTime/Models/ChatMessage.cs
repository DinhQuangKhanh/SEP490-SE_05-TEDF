namespace TEDF.Infrastructure.RealTime.Models
{
    /// <summary>
    /// Chat message model for real-time messaging.
    /// </summary>
    public record ChatMessage(
        Guid ConversationId,
        Guid SenderId,
        string Content,
        DateTime SentAt
    );

    /// <summary>
    /// Typing indicator model for chat.
    /// </summary>
    public record TypingIndicator(
        Guid ConversationId,
        Guid UserId,
        bool IsTyping
    );
}
