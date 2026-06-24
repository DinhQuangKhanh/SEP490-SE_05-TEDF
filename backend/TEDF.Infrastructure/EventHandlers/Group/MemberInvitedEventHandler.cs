using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.GroupAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Infrastructure.EventHandlers.Group
{
    public class MemberInvitedEventHandler : INotificationHandler<MemberInvitedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<MemberInvitedEventHandler> _logger;

        public MemberInvitedEventHandler(
            INotificationService notificationService,
            IUserRepository userRepository,
            ILogger<MemberInvitedEventHandler> logger)
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(MemberInvitedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Member invited to group: GroupId={GroupId}, InviterId={InviterId}, InviteeId={InviteeId}",
                notification.GroupId, notification.InviterId, notification.InviteeId);

            try
            {
                var inviter = await _userRepository.GetByIdAsync(notification.InviterId, cancellationToken);
                var inviterName = inviter?.FullName ?? "Một sinh viên";

                await _notificationService.SendAsync(
                    notification.InviteeId,
                    "Bạn nhận được lời mời tham gia nhóm",
                    $"{inviterName} đã mời bạn tham gia nhóm {notification.GroupCode}. Vui lòng phản hồi trước khi lời mời hết hạn.",
                    NotificationType.Info,
                    NotificationCategory.Group,
                    "/student/groups/invitations",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling MemberInvitedEvent for group {GroupId}", notification.GroupId);
            }
        }
    }
}
