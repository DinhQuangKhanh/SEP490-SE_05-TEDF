using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Constants;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Infrastructure.EventHandlers.Support;

/// <summary>
/// Handles the TicketMessageAddedEvent domain event.
/// Logs the message addition and notifies the relevant party (reporter or admin).
/// </summary>
public class TicketMessageAddedEventHandler : INotificationHandler<TicketMessageAddedEvent>
{
    private readonly ILogger<TicketMessageAddedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;

    public TicketMessageAddedEventHandler(
        ILogger<TicketMessageAddedEventHandler> logger,
        INotificationService notificationService,
        ISupportTicketRepository ticketRepository,
        IUserRepository userRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(TicketMessageAddedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Message added to ticket: TicketId={TicketId}, MessageId={MessageId}, SenderId={SenderId}",
            notification.TicketId,
            notification.MessageId,
            notification.SenderId);

        var ticket = await _ticketRepository.GetByIdAsync(notification.TicketId, cancellationToken);
        if (ticket is null) return;

        var sender = await _userRepository.GetByIdAsync(notification.SenderId, cancellationToken);
        var senderName = sender?.FullName ?? "Người dùng";

        // If sender is the reporter, notify the assignee (Admin). Otherwise, notify the reporter.
        var recipientId = notification.SenderId == ticket.ReporterId
            ? ticket.AssigneeId ?? Guid.Empty
            : ticket.ReporterId;

        if (recipientId == Guid.Empty)
        {
            // Ticket has no specific assignee, notify all Admins
            var admins = await _userRepository.GetByRoleAsync("Admin", cancellationToken);
            var adminIds = admins.Select(a => a.Id).ToList();

            if (adminIds.Any())
            {
                await _notificationService.SendToMultipleAsync(
                    adminIds,
                    "Phản hồi mới trên ticket",
                    $"{senderName} đã gửi phản hồi trên ticket {ticket.Code.Value}.",
                    NotificationType.Info,
                    NotificationCategory.Support,
                    "/admin/support",
                    cancellationToken);
            }
        }
        else
        {
            var reporter = await _userRepository.GetByIdAsync(recipientId, cancellationToken);
            var reporterTargetUrl = reporter is not null && reporter.HasRole(DomainRoleIds.Student)
                ? "/student/support"
                : reporter is not null && reporter.HasRole(DomainRoleIds.Admin)
                    ? "/admin/support"
                    : "/lecturer/support";

            await _notificationService.SendAsync(
                recipientId,
                "Phản hồi mới trên ticket",
                $"{senderName} đã gửi phản hồi trên ticket {ticket.Code.Value}.",
                NotificationType.Info,
                NotificationCategory.Support,
                reporterTargetUrl,
                cancellationToken);
        }
    }
}
