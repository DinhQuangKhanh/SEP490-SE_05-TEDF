using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.Support;

/// <summary>
/// Handles the TicketClosedEvent domain event.
/// Logs the closure and notifies the reporter.
/// </summary>
public class TicketClosedEventHandler : INotificationHandler<TicketClosedEvent>
{
    private readonly ILogger<TicketClosedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly ISupportTicketRepository _ticketRepository;

    public TicketClosedEventHandler(
        ILogger<TicketClosedEventHandler> logger,
        INotificationService notificationService,
        ISupportTicketRepository ticketRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _ticketRepository = ticketRepository;
    }

    public async Task Handle(TicketClosedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ticket closed: TicketId={TicketId}",
            notification.TicketId);

        var ticket = await _ticketRepository.GetByIdAsync(notification.TicketId, cancellationToken);
        if (ticket is null) return;

        // Note: Notifications disabled to ensure reporter only gets reply notifications.
    }
}
