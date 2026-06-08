using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.Events;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Infrastructure.EventHandlers.Support;

/// <summary>
/// Handles the TicketReopenedEvent domain event.
/// Logs the reopening and notifies the assignee (Admin).
/// </summary>
public class TicketReopenedEventHandler : INotificationHandler<TicketReopenedEvent>
{
    private readonly ILogger<TicketReopenedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly ISupportTicketRepository _ticketRepository;

    public TicketReopenedEventHandler(
        ILogger<TicketReopenedEventHandler> logger,
        INotificationService notificationService,
        ISupportTicketRepository ticketRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _ticketRepository = ticketRepository;
    }

    public async Task Handle(TicketReopenedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ticket reopened: TicketId={TicketId}",
            notification.TicketId);

        var ticket = await _ticketRepository.GetByIdAsync(notification.TicketId, cancellationToken);
        if (ticket is null) return;

        // Notify the assignee (Admin) that the ticket has been reopened
        if (ticket.AssigneeId.HasValue)
        {
            await _notificationService.SendAsync(
                ticket.AssigneeId.Value,
                "Ticket được mở lại",
                $"Ticket {ticket.Code.Value}: \"{ticket.Title}\" đã được mở lại và cần xử lý thêm.",
                NotificationType.Warning,
                NotificationCategory.Support,
                $"/admin/supports/{notification.TicketId}",
                cancellationToken);
        }
    }
}
