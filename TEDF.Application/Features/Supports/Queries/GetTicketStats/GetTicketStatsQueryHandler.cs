using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Supports.DTOs;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Application.Features.Supports.Queries.GetTicketStats;

public class GetTicketStatsQueryHandler : IQueryHandler<GetTicketStatsQuery, TicketStatsDto>
{
    private readonly ISupportTicketRepository _repository;

    public GetTicketStatsQueryHandler(ISupportTicketRepository repository)
    {
        _repository = repository;
    }

    public async Task<TicketStatsDto> Handle(GetTicketStatsQuery request, CancellationToken cancellationToken)
    {
        int unread, inProgress, resolved, closed;

        if (request.IsAdmin)
        {
            // Admin sees stats for all tickets
            var counts = await _repository.GetStatusCountAsync(cancellationToken);
            unread = counts.GetValueOrDefault(TicketStatus.Open, 0);
            inProgress = counts.GetValueOrDefault(TicketStatus.InProgress, 0);
            resolved = counts.GetValueOrDefault(TicketStatus.Resolved, 0);
            closed = counts.GetValueOrDefault(TicketStatus.Closed, 0);
        }
        else
        {
            // Non-admin: only count their own tickets
            var myTickets = await _repository.GetByReporterIdAsync(request.ReporterId, cancellationToken);
            var ticketList = myTickets.ToList();
            unread = ticketList.Count(t => t.Status == TicketStatus.Open);
            inProgress = ticketList.Count(t => t.Status == TicketStatus.InProgress);
            resolved = ticketList.Count(t => t.Status == TicketStatus.Resolved);
            closed = ticketList.Count(t => t.Status == TicketStatus.Closed);
        }

        return new TicketStatsDto
        {
            TotalTickets = unread + inProgress + resolved + closed,
            Unread = unread,
            InProgress = inProgress,
            Resolved = resolved + closed
        };
    }
}
