using TEDF.Domain.Aggregates.SupportAggregate.ValueObjects;
using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.SupportAggregate
{
    /// <summary>
    /// Represents a single message or reply within a support ticket.
    /// </summary>
    public class TicketMessage : Entity<Guid>
    {
        public Guid TicketId { get; private set; }
        public Guid SenderId { get; private set; }
        public string Content { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }

        private readonly List<FileAttachment> _attachments = new();
        public IReadOnlyCollection<FileAttachment> Attachments => _attachments.AsReadOnly();

        private TicketMessage() { } // EF Core

        internal static TicketMessage Create(Guid ticketId, Guid senderId, string content, IEnumerable<FileAttachment>? attachments = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be empty.", nameof(content));

            var message = new TicketMessage
            {
                TicketId = ticketId,
                SenderId = senderId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            if (attachments != null)
            {
                message._attachments.AddRange(attachments);
            }

            return message;
        }
    }
}
