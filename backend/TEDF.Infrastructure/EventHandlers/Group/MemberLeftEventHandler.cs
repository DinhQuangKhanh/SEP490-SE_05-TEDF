using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.GroupAggregate.Events;
using TEDF.Domain.Enums.Notification;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Group
{
    public class MemberLeftEventHandler : INotificationHandler<MemberLeftEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<MemberLeftEventHandler> _logger;

        public MemberLeftEventHandler(
            INotificationService notificationService,
            ILogger<MemberLeftEventHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(MemberLeftEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Member left group: GroupId={GroupId}, StudentId={StudentId}",
                notification.GroupId, notification.StudentId);

            // The student chose to leave, so only the leader needs telling — a group that drops below
            // the minimum size cannot register a topic.
            if (notification.LeaderId is null) return;

            try
            {
                await _notificationService.SendAsync(
                    notification.LeaderId.Value,
                    "Một thành viên đã rời nhóm",
                    $"Một thành viên đã rời khỏi nhóm {notification.GroupCode}. Hãy mời thành viên mới nếu nhóm chưa đủ số lượng.",
                    NotificationType.Warning,
                    NotificationCategory.Group,
                    "/student/groups",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling MemberLeftEvent for group {GroupId}", notification.GroupId);
            }
        }
    }
}
