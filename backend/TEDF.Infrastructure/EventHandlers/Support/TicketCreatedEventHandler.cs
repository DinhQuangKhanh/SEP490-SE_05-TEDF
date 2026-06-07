using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SupportAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Infrastructure.EventHandlers.Support;

/// <summary>
/// Handles the TicketCreatedEvent domain event.
/// Logs ticket creation and sends a notification to all admins.
/// </summary>
public class TicketCreatedEventHandler : INotificationHandler<TicketCreatedEvent>
{
    private readonly ILogger<TicketCreatedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;

    public TicketCreatedEventHandler(
        ILogger<TicketCreatedEventHandler> logger,
        INotificationService notificationService,
        IUserRepository userRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _userRepository = userRepository;
    }

    public async Task Handle(TicketCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Support ticket created: TicketId={TicketId}, Code={TicketCode}, Category={Category}, Priority={Priority}",
            notification.TicketId,
            notification.TicketCode,
            notification.Category,
            notification.Priority);

        var admins = await _userRepository.GetByRoleAsync("Admin", cancellationToken);
        var adminIds = admins.Select(a => a.Id).ToList();

        if (adminIds.Any())
        {
            // Notify admins about the new ticket
            await _notificationService.SendToMultipleAsync(
                adminIds,
                "Yêu cầu hỗ trợ mới",
                $"Ticket {notification.TicketCode} (Ưu tiên: {notification.Priority}) vừa được tạo.",
                NotificationType.Info,
                NotificationCategory.Support,
                $"/admin/supports/{notification.TicketId}",
                cancellationToken);
        }
    }
}
