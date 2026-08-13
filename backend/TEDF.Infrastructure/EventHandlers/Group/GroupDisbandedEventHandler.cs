using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.GroupAggregate.Events;
using TEDF.Domain.Enums.Notification;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Group
{
    public class GroupDisbandedEventHandler : INotificationHandler<GroupDisbandedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<GroupDisbandedEventHandler> _logger;

        public GroupDisbandedEventHandler(
            INotificationService notificationService,
            ILogger<GroupDisbandedEventHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(GroupDisbandedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Group disbanded: GroupId={GroupId}, MemberCount={MemberCount}",
                notification.GroupId, notification.MemberIds.Count);

            if (notification.MemberIds.Count == 0) return;

            try
            {
                await _notificationService.SendToMultipleAsync(
                    notification.MemberIds,
                    "Nhóm đã bị giải tán",
                    $"Nhóm {notification.GroupCode} đã được nhóm trưởng giải tán. Bạn có thể tạo nhóm mới hoặc tham gia một nhóm khác.",
                    NotificationType.Warning,
                    NotificationCategory.Group,
                    "/student/groups",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GroupDisbandedEvent for group {GroupId}", notification.GroupId);
            }
        }
    }
}
