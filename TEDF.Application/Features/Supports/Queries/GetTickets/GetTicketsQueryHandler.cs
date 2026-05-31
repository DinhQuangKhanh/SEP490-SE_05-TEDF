using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Supports.DTOs;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.UserAggregate;

namespace TEDF.Application.Features.Supports.Queries.GetTickets;

public class GetTicketsQueryHandler : IQueryHandler<GetTicketsQuery, List<TicketListDto>>
{
    private readonly ISupportTicketRepository _supportTicketRepository;
    private readonly IUserRepository _userRepository;

    public GetTicketsQueryHandler(ISupportTicketRepository supportTicketRepository, IUserRepository userRepository)
    {
        _supportTicketRepository = supportTicketRepository;
        _userRepository = userRepository;
    }

    public async Task<List<TicketListDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<SupportTicket> tempTickets;

        // Non-admin users only see their own tickets
        if (!request.IsAdmin)
        {
            tempTickets = await _supportTicketRepository.GetByReporterIdAsync(request.ReporterId, cancellationToken);

            if (request.Status.HasValue)
            {
                tempTickets = tempTickets.Where(t => t.Status == request.Status.Value);
            }
        }
        else if (request.Status.HasValue)
        {
            tempTickets = await _supportTicketRepository.GetByStatusAsync(request.Status.Value, cancellationToken);
        }
        else
        {
            // Admin sees all tickets
            tempTickets = await _supportTicketRepository.GetAllAsync(cancellationToken);
        }

        if (request.Priority.HasValue)
        {
            tempTickets = tempTickets.Where(t => t.Priority == request.Priority.Value);
        }

        var tickets = tempTickets.OrderByDescending(t => t.CreatedAt).ToList();

        // Fetch users (Reporters)
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

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLowerInvariant();
            result = result.Where(t => 
                t.Code.ToLowerInvariant().Contains(search) || 
                t.Title.ToLowerInvariant().Contains(search) ||
                (t.Reporter?.FullName?.ToLowerInvariant().Contains(search) ?? false));
        }

        return result.ToList();
    }
}
