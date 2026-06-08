using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.Support;

/// <summary>
/// Handles the TicketResolvedEvent domain event.
/// Logs the resolution and notifies the reporter.
/// </summary>
public class TicketResolvedEventHandler : INotificationHandler<TicketResolvedEvent>
{
    private readonly ILogger<TicketResolvedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly ISupportTicketRepository _ticketRepository;

    public TicketResolvedEventHandler(
        ILogger<TicketResolvedEventHandler> logger,
        INotificationService notificationService,
        ISupportTicketRepository ticketRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _ticketRepository = ticketRepository;
    }

    public async Task Handle(TicketResolvedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ticket resolved: TicketId={TicketId}",
            notification.TicketId);

        var ticket = await _ticketRepository.GetByIdAsync(notification.TicketId, cancellationToken);
        if (ticket is null) return;

        // Note: Notifications disabled to ensure reporter only gets reply notifications.
    }
}
