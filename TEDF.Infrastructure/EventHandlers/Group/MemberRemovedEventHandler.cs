using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TEDF.Domain.Aggregates.GroupAggregate.Events;
using TEDF.Domain.Enums.Notification;
using TEDF.Application.Common.Interfaces;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Group
{
    public class MemberRemovedEventHandler : INotificationHandler<MemberRemovedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IUserActivityLogRepository _activityLogRepository;
        private readonly ILogger<MemberRemovedEventHandler> _logger;

        public MemberRemovedEventHandler(
            INotificationService notificationService,
            IUserActivityLogRepository activityLogRepository,
            ILogger<MemberRemovedEventHandler> logger)
        {
            _notificationService = notificationService;
            _activityLogRepository = activityLogRepository;
            _logger = logger;
        }

        public async Task Handle(MemberRemovedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Member removed from group: GroupId={GroupId}, StudentId={StudentId}, RemovedBy={RemovedBy}",
                notification.GroupId, notification.StudentId, notification.RemovedBy);

            try
            {
                // Log the activity
                var log = new UserActivityLogDocument
                {
                    UserId = notification.RemovedBy,
                    ActiveRole = "mentor",
                    Action = "Xóa thành viên khỏi nhóm",
                    ActionCode = "MemberRemoved",
                    Category = "Group",
                    EntityType = "Group",
                    EntityId = notification.GroupId,
                    Severity = "warning",
                    Timestamp = DateTime.UtcNow,
                    Details = new BsonDocument
                    {
                        ["GroupId"] = new BsonBinaryData(notification.GroupId.ToByteArray()),
                        ["RemovedStudentId"] = new BsonBinaryData(notification.StudentId.ToByteArray()),
                    }
                };

                await _activityLogRepository.AddAsync(log, cancellationToken);

                // Notify the removed student
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
