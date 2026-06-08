using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Supports.DTOs;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// Read-side service for the Supports feature. See <see cref="ISupportsQueryService"/>.
/// </summary>
public class SupportsQueryService : ISupportsQueryService
{
    private readonly ISupportTicketRepository _supportTicketRepository;
    private readonly IUserRepository _userRepository;

    public SupportsQueryService(ISupportTicketRepository supportTicketRepository, IUserRepository userRepository)
    {
        _supportTicketRepository = supportTicketRepository;
        _userRepository = userRepository;
    }

    public async Task<TicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await _supportTicketRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(SupportTicket), id);

        var userIds = ticket.Messages.Select(m => m.SenderId).ToList();
        userIds.Add(ticket.ReporterId);
        if (ticket.AssigneeId.HasValue) userIds.Add(ticket.AssigneeId.Value);
        userIds = userIds.Distinct().ToList();

        var usersList = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var users = usersList.ToDictionary(u => u.Id, u => new UserBriefDto(u.Id, u.FullName, u.Email, string.Join(", ", u.GetActiveRoles())));

        return new TicketDto
        {
            Id = ticket.Id,
            Code = ticket.Code.Value,
            Title = ticket.Title,
            Description = ticket.Description,
            Category = ticket.Category.ToString(),
            Priority = ticket.Priority.ToString(),
            Status = ticket.Status.ToString(),
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            Reporter = users.GetValueOrDefault(ticket.ReporterId)!,
            Assignee = ticket.AssigneeId.HasValue ? users.GetValueOrDefault(ticket.AssigneeId.Value) : null,
            Messages = ticket.Messages.OrderBy(m => m.CreatedAt).Select(m => new TicketMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                Sender = users.GetValueOrDefault(m.SenderId)
            }).ToList()
        };
    }

    public async Task<TicketStatsDto> GetStatsAsync(Guid reporterId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        int unread, inProgress, resolved, closed;

        if (isAdmin)
        {
            var counts = await _supportTicketRepository.GetStatusCountAsync(cancellationToken);
            unread = counts.GetValueOrDefault(TicketStatus.Open, 0);
            inProgress = counts.GetValueOrDefault(TicketStatus.InProgress, 0);
            resolved = counts.GetValueOrDefault(TicketStatus.Resolved, 0);
            closed = counts.GetValueOrDefault(TicketStatus.Closed, 0);
        }
        else
        {
            var myTickets = await _supportTicketRepository.GetByReporterIdAsync(reporterId, cancellationToken);
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

    public async Task<List<TicketListDto>> GetTicketsAsync(
        Guid reporterId, bool isAdmin, string? searchTerm,
        TicketStatus? status, TicketPriority? priority, CancellationToken cancellationToken = default)
    {
        IEnumerable<SupportTicket> tempTickets;

        if (!isAdmin)
        {
            tempTickets = await _supportTicketRepository.GetByReporterIdAsync(reporterId, cancellationToken);
            if (status.HasValue)
                tempTickets = tempTickets.Where(t => t.Status == status.Value);
        }
        else if (status.HasValue)
        {
            tempTickets = await _supportTicketRepository.GetByStatusAsync(status.Value, cancellationToken);
        }
        else
        {
            tempTickets = await _supportTicketRepository.GetAllAsync(cancellationToken);
        }

        if (priority.HasValue)
            tempTickets = tempTickets.Where(t => t.Priority == priority.Value);

        var tickets = tempTickets.OrderByDescending(t => t.CreatedAt).ToList();

        var reporterIds = tickets.Select(t => t.ReporterId).Distinct().ToList();
        var usersList = await _userRepository.GetByIdsAsync(reporterIds, cancellationToken);
        var users = usersList.ToDictionary(u => u.Id, u => new UserBriefDto(u.Id, u.FullName, u.Email, string.Join(", ", u.GetActiveRoles())));

        var result = tickets.Select(t => new TicketListDto
        {
            Id = t.Id,
            Code = t.Code.Value,
            Title = t.Title,
            Category = t.Category.ToString(),
            Priority = t.Priority.ToString(),
            Status = t.Status.ToString(),
            CreatedAt = t.CreatedAt,
            Reporter = users.GetValueOrDefault(t.ReporterId)!
        });

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLowerInvariant();
            result = result.Where(t =>
                t.Code.ToLowerInvariant().Contains(search) ||
                t.Title.ToLowerInvariant().Contains(search) ||
                (t.Reporter?.FullName?.ToLowerInvariant().Contains(search) ?? false));
        }

        return result.ToList();
    }
}
