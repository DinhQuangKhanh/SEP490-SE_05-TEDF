using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.Events;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Application.Features.Supports.EventHandlers;

/// <summary>
/// Handles the TicketStatusChangedEvent domain event.
/// Logs the status change and notifies the reporter.
/// </summary>
public class TicketStatusChangedEventHandler : INotificationHandler<TicketStatusChangedEvent>
{
    private readonly ILogger<TicketStatusChangedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly ISupportTicketRepository _ticketRepository;

    public TicketStatusChangedEventHandler(
        ILogger<TicketStatusChangedEventHandler> logger,
        INotificationService notificationService,
        ISupportTicketRepository ticketRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _ticketRepository = ticketRepository;
    }

    public async Task Handle(TicketStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ticket status changed: TicketId={TicketId}, OldStatus={OldStatus}, NewStatus={NewStatus}",
            notification.TicketId,
            notification.OldStatus,
            notification.NewStatus);

        var ticket = await _ticketRepository.GetByIdAsync(notification.TicketId, cancellationToken);
        if (ticket is null) return;

        // Note: Notifications for status changes are disabled as per user request to
        // only notify reporters when there is an actual reply message on the ticket.
    }
}
