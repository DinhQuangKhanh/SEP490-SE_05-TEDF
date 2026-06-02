using TEDF.Domain.Aggregates.SupportAggregate.Events;
using TEDF.Domain.Aggregates.SupportAggregate.ValueObjects;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Domain.Aggregates.SupportAggregate
{
    public class SupportTicket : AggregateRoot<Guid>
    {
        public TicketCode Code { get; private set; } = null!;
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public Guid ReporterId { get; private set; }
        public Guid? AssigneeId { get; private set; }
        public TicketCategory Category { get; private set; }
        public TicketPriority Priority { get; private set; }
        public TicketStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? ResolvedAt { get; private set; }
        public DateTime? ClosedAt { get; private set; }

        private readonly List<TicketMessage> _messages = new();
        public IReadOnlyCollection<TicketMessage> Messages => _messages.AsReadOnly();

        private readonly List<FileAttachment> _attachments = new();
        public IReadOnlyCollection<FileAttachment> Attachments => _attachments.AsReadOnly();

        private SupportTicket() { }

        public static SupportTicket Create(
            TicketCode code,
            string title,
            string description,
            Guid reporterId,
            TicketCategory category,
            TicketPriority priority = TicketPriority.Medium,
            IEnumerable<FileAttachment>? attachments = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty.", nameof(title));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be empty.", nameof(description));

            var ticket = new SupportTicket
            {
                Id = Guid.NewGuid(),
                Code = code,
                Title = title.Trim(),
                Description = description.Trim(),
                ReporterId = reporterId,
                Category = category,
                Priority = priority,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            if (attachments != null)
            {
                ticket._attachments.AddRange(attachments);
            }

            ticket.RaiseDomainEvent(new TicketCreatedEvent(ticket.Id, code.Value, category, priority));
            return ticket;
        }

        public void Assign(Guid assigneeId)
        {
            if (Status == TicketStatus.Closed)
                throw new BusinessRuleValidationException("Cannot assign a closed ticket.");

            AssigneeId = assigneeId;

            if (Status == TicketStatus.Open)
            {
                var oldStatus = Status;
                Status = TicketStatus.InProgress;
                RaiseDomainEvent(new TicketStatusChangedEvent(Id, oldStatus, Status));
            }

            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new TicketAssignedEvent(Id, assigneeId));
        }

        public void StartProgress()
        {
            if (Status != TicketStatus.Open)
                throw new BusinessRuleValidationException("Only open tickets can be started.");

            var oldStatus = Status;
            Status = TicketStatus.InProgress;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new TicketStatusChangedEvent(Id, oldStatus, Status));
        }

        public void Resolve()
        {
            if (Status == TicketStatus.Closed)
                throw new BusinessRuleValidationException("Closed tickets cannot be resolved.");
            if (Status == TicketStatus.Resolved)
                throw new BusinessRuleValidationException("Ticket is already resolved.");

            var oldStatus = Status;
            Status = TicketStatus.Resolved;
            ResolvedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new TicketStatusChangedEvent(Id, oldStatus, Status));
            RaiseDomainEvent(new TicketResolvedEvent(Id));
        }

        public void Close()
        {
            if (Status == TicketStatus.Closed)
                throw new BusinessRuleValidationException("Ticket is already closed.");

            var oldStatus = Status;
            Status = TicketStatus.Closed;
            ClosedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new TicketStatusChangedEvent(Id, oldStatus, Status));
            RaiseDomainEvent(new TicketClosedEvent(Id));
        }

        public void Reopen()
        {
            if (Status != TicketStatus.Closed && Status != TicketStatus.Resolved)
                throw new BusinessRuleValidationException("Only closed or resolved tickets can be reopened.");

            var oldStatus = Status;
            Status = TicketStatus.Open;
            ResolvedAt = null;
            ClosedAt = null;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new TicketStatusChangedEvent(Id, oldStatus, Status));
            RaiseDomainEvent(new TicketReopenedEvent(Id));
        }

        public void UpdatePriority(TicketPriority priority)
        {
            if (Status == TicketStatus.Closed)
                throw new BusinessRuleValidationException("Cannot update priority of a closed ticket.");

            Priority = priority;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateCategory(TicketCategory category)
        {
            if (Status == TicketStatus.Closed)
                throw new BusinessRuleValidationException("Cannot update category of a closed ticket.");

            Category = category;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDetails(string? title = null, string? description = null)
        {
            if (Status == TicketStatus.Closed)
                throw new BusinessRuleValidationException("Cannot update a closed ticket.");

            if (!string.IsNullOrWhiteSpace(title))
                Title = title.Trim();

            if (!string.IsNullOrWhiteSpace(description))
                Description = description.Trim();

            UpdatedAt = DateTime.UtcNow;
        }

        public void AddMessage(Guid senderId, string content, IEnumerable<FileAttachment>? attachments = null)
        {
            if (Status == TicketStatus.Closed)
                throw new BusinessRuleValidationException("Cannot add a message to a closed ticket.");

            var message = TicketMessage.Create(Id, senderId, content, attachments);
            _messages.Add(message);
            
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new TicketMessageAddedEvent(Id, message.Id, senderId));
        }
    }
}
