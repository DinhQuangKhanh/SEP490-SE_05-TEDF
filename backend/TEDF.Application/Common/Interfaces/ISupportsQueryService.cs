using TEDF.Application.Features.Supports.DTOs;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-side service for the Supports feature. Query handlers depend on this only.
/// </summary>
public interface ISupportsQueryService
{
    Task<TicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TicketStatsDto> GetStatsAsync(Guid reporterId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<List<TicketListDto>> GetTicketsAsync(
        Guid reporterId, bool isAdmin, string? searchTerm,
        TicketStatus? status, TicketPriority? priority, CancellationToken cancellationToken = default);
}
