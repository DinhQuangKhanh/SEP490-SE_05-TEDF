using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.GroupAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Infrastructure.EventHandlers.Group
{
    public class JoinRequestedEventHandler : INotificationHandler<JoinRequestedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<JoinRequestedEventHandler> _logger;

        public JoinRequestedEventHandler(
            INotificationService notificationService,
            IUserRepository userRepository,
            ILogger<JoinRequestedEventHandler> logger)
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(JoinRequestedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Join request created: GroupId={GroupId}, StudentId={StudentId}",
                notification.GroupId, notification.StudentId);

            if (!notification.LeaderId.HasValue)
                return;

            try
            {
                var student = await _userRepository.GetByIdAsync(notification.StudentId, cancellationToken);
                var studentName = student?.FullName ?? "Một sinh viên";

                await _notificationService.SendAsync(
                    notification.LeaderId.Value,
                    "Có yêu cầu tham gia nhóm mới",
                    $"{studentName} vừa gửi yêu cầu tham gia nhóm {notification.GroupCode}. Vui lòng phản hồi trong vòng 1 giờ.",
                    NotificationType.Info,
                    NotificationCategory.Group,
                    "/student/groups",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling JoinRequestedEvent for group {GroupId}", notification.GroupId);
            }
        }
    }
}
