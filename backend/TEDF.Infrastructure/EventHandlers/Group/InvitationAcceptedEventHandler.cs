using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.GroupAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Infrastructure.EventHandlers.Group
{
    public class InvitationAcceptedEventHandler : INotificationHandler<InvitationAcceptedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<InvitationAcceptedEventHandler> _logger;

        public InvitationAcceptedEventHandler(
            INotificationService notificationService,
            IUserRepository userRepository,
            ILogger<InvitationAcceptedEventHandler> logger)
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(InvitationAcceptedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Invitation accepted: GroupId={GroupId}, InviteeId={InviteeId}",
                notification.GroupId, notification.InviteeId);

            try
            {
                var invitee = await _userRepository.GetByIdAsync(notification.InviteeId, cancellationToken);
                var studentName = invitee?.FullName ?? "Một sinh viên";

                await _notificationService.SendAsync(
                    notification.InviterId,
                    "Lời mời được chấp nhận",
                    $"{studentName} đã chấp nhận lời mời tham gia nhóm {notification.GroupCode}.",
                    NotificationType.Success,
                    NotificationCategory.Group,
                    "/student/groups",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling InvitationAcceptedEvent for group {GroupId}", notification.GroupId);
            }
        }
    }
}
