using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.GroupAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Infrastructure.EventHandlers.Group
{
    public class InvitationRejectedEventHandler : INotificationHandler<InvitationRejectedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<InvitationRejectedEventHandler> _logger;

        public InvitationRejectedEventHandler(
            INotificationService notificationService,
            IUserRepository userRepository,
            ILogger<InvitationRejectedEventHandler> logger)
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(InvitationRejectedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Invitation rejected: GroupId={GroupId}, InviteeId={InviteeId}",
                notification.GroupId, notification.InviteeId);

            try
            {
                var invitee = await _userRepository.GetByIdAsync(notification.InviteeId, cancellationToken);
                var studentName = invitee?.FullName ?? "Một sinh viên";

                await _notificationService.SendAsync(
                    notification.InviterId,
                    "Lời mời bị từ chối",
                    $"{studentName} đã từ chối lời mời tham gia nhóm {notification.GroupCode}.",
                    NotificationType.Info,
                    NotificationCategory.Group,
                    "/student/groups",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling InvitationRejectedEvent for group {GroupId}", notification.GroupId);
            }
        }
    }
}
