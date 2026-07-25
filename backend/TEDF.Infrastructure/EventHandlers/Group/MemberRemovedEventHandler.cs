using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.GroupAggregate.Events;
using TEDF.Domain.Enums.Notification;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Group
{
    public class MemberRemovedEventHandler : INotificationHandler<MemberRemovedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<MemberRemovedEventHandler> _logger;

        public MemberRemovedEventHandler(
            INotificationService notificationService,
            ILogger<MemberRemovedEventHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(MemberRemovedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Member removed from group: GroupId={GroupId}, StudentId={StudentId}, RemovedBy={RemovedBy}",
                notification.GroupId, notification.StudentId, notification.RemovedBy);

            try
            {
                await _notificationService.SendAsync(
                    notification.StudentId,
                    "Bạn đã bị xóa khỏi nhóm",
                    "Bạn đã bị xóa khỏi nhóm đồ án. Vui lòng liên hệ nhóm trưởng để biết thêm chi tiết.",
                    NotificationType.Warning,
                    NotificationCategory.Group,
                    "/student/groups",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling MemberRemovedEvent for group {GroupId}", notification.GroupId);
            }
        }
    }
}
