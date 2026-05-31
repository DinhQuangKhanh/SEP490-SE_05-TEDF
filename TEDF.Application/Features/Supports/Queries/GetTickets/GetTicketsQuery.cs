using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Supports.DTOs;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Application.Features.Supports.Queries.GetTickets;

public record GetTicketsQuery(
    Guid ReporterId,
    bool IsAdmin,
    string? SearchTerm = null,
    TicketStatus? Status = null,
    TicketPriority? Priority = null) : IQuery<List<TicketListDto>>;
