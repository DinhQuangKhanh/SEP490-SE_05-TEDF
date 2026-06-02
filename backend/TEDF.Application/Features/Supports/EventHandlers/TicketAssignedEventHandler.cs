using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.Events;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Application.Features.Supports.EventHandlers;

/// <summary>
/// Handles the TicketAssignedEvent domain event.
/// Logs the assignment and notifies both the assignee and the reporter.
/// </summary>
public class TicketAssignedEventHandler : INotificationHandler<TicketAssignedEvent>
{
    private readonly ILogger<TicketAssignedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly ISupportTicketRepository _ticketRepository;

    public TicketAssignedEventHandler(
        ILogger<TicketAssignedEventHandler> logger,
        INotificationService notificationService,
        ISupportTicketRepository ticketRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _ticketRepository = ticketRepository;
    }

    public async Task Handle(TicketAssignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ticket assigned: TicketId={TicketId}, AssigneeId={AssigneeId}",
            notification.TicketId,
            notification.AssigneeId);

        var ticket = await _ticketRepository.GetByIdAsync(notification.TicketId, cancellationToken);
        if (ticket is null) return;

        // Notify the assignee (Admin) that a ticket has been assigned to them
        await _notificationService.SendAsync(
            notification.AssigneeId,
            "Ticket được giao cho bạn",
            $"Bạn đã được giao xử lý ticket {ticket.Code.Value}: \"{ticket.Title}\".",
            NotificationType.Info,
            NotificationCategory.Support,
            $"/admin/supports/{notification.TicketId}",
            cancellationToken);

        // Notify the reporter about assignment is disabled as per user request.
    }
}
