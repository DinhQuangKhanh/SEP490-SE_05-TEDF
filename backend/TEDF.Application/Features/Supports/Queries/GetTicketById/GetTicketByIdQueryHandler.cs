using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Supports.DTOs;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;

namespace TEDF.Application.Features.Supports.Queries.GetTicketById;

public class GetTicketByIdQueryHandler : IQueryHandler<GetTicketByIdQuery, TicketDto>
{
    private readonly ISupportTicketRepository _supportTicketRepository;
    private readonly IUserRepository _userRepository;

    public GetTicketByIdQueryHandler(ISupportTicketRepository supportTicketRepository, IUserRepository userRepository)
    {
        _supportTicketRepository = supportTicketRepository;
        _userRepository = userRepository;
    }

    public async Task<TicketDto> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _supportTicketRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(SupportTicket), request.Id);

        // Fetch users (Reporter, Assignee, Message Senders)
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
}
