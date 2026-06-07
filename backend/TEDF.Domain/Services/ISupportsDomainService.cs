using TEDF.Domain.Enums.Ticket;

namespace TEDF.Domain.Services;

/// <summary>
/// Write-side service for the Supports feature. Command handlers depend on this only.
/// </summary>
public interface ISupportsDomainService
{
    Task<Guid> CreateTicketAsync(
        string title, string description, TicketCategory category, TicketPriority priority,
        Guid reporterId, CancellationToken cancellationToken = default);

    Task ReplyAsync(Guid ticketId, Guid senderId, string content, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(Guid ticketId, TicketStatus status, CancellationToken cancellationToken = default);
}
