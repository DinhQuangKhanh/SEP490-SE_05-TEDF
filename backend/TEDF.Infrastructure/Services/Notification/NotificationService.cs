using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Notifications.DTOs;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Notification;
using TEDF.Infrastructure.RealTime.Models;
using TEDF.Infrastructure.RealTime.Services;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Infrastructure.Services.Notification
{
    /// <summary>
    /// Notification service that handles both persistence (MongoDB) and real-time delivery (SignalR).
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IRealtimeNotificationService? _realtimeService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            ILogger<NotificationService> logger,
            IRealtimeNotificationService? realtimeService = null)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
            _realtimeService = realtimeService;
        }

        public async Task SendAsync(
            Guid userId,
            string title,
            string content,
            NotificationType type,
            NotificationCategory category,
            string? targetUrl = null,
            CancellationToken ct = default)
        {
            // Persist notification to MongoDB
            var notification = new NotificationDocument
            {
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                Category = category,
                TargetUrl = targetUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification, ct);

            // Send real-time notification
            if (_realtimeService is not null)
            {
                var alert = new NotificationAlert(
                    Id: notification.Id,
                    Title: notification.Title,
                    Content: notification.Content,
                    Type: notification.Type.ToString(),
                    Category: notification.Category.ToString(),
                    TargetUrl: notification.TargetUrl,
                    CreatedAt: notification.CreatedAt
                );

                await _realtimeService.SendNotificationAlertAsync(userId, alert, ct);
            }

            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
        }

        public async Task SendToMultipleAsync(
            IEnumerable<Guid> userIds,
            string title,
            string content,
            NotificationType type,
            NotificationCategory category,
            string? targetUrl = null,
            CancellationToken ct = default)
        {
            var userIdList = userIds.ToList();
            var now = DateTime.UtcNow;

            // Persist notifications to MongoDB
            var notifications = userIdList.Select(userId => new NotificationDocument
            {
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                Category = category,
                TargetUrl = targetUrl,
                CreatedAt = now
            }).ToList();

            await _notificationRepository.AddManyAsync(notifications, ct);

            // Send real-time notifications
            if (_realtimeService is not null)
            {
                foreach (var notification in notifications)
                {
                    var alert = new NotificationAlert(
                        Id: notification.Id,
                        Title: notification.Title,
                        Content: notification.Content,
                        Type: notification.Type.ToString(),
                        Category: notification.Category.ToString(),
                        TargetUrl: notification.TargetUrl,
                        CreatedAt: notification.CreatedAt
                    );

                    await _realtimeService.SendNotificationAlertAsync(notification.UserId, alert, ct);
                }
            }

            _logger.LogInformation("Notification sent to {Count} users: {Title}", userIdList.Count, title);
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(
            Guid userId,
            int limit = 50,
            CancellationToken ct = default)
        {
            var docs = await _notificationRepository.GetByUserIdAsync(userId, limit, ct);
            return docs.Select(MapToDto);
        }

        public async Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(
            Guid userId,
            CancellationToken ct = default)
        {
            var docs = await _notificationRepository.GetUnreadByUserIdAsync(userId, ct);
            return docs.Select(MapToDto);
        }

        public async Task<long> GetUnreadCountAsync(Guid userId, NotificationCategory? category = null, CancellationToken ct = default)
            => await _notificationRepository.GetUnreadCountAsync(userId, category, ct);

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
            => await _notificationRepository.MarkAsReadAsync(notificationId, userId, ct);

        public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
            => await _notificationRepository.MarkAllAsReadAsync(userId, ct);

        private static NotificationDto MapToDto(NotificationDocument n) => new()
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Content = n.Content,
            Type = n.Type.ToString(),
            Category = n.Category.ToString(),
            TargetUrl = n.TargetUrl,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            CreatedAt = n.CreatedAt
        };

        public async Task NotifyProjectSubmittedAsync(
            Guid projectId,
            string projectName,
            Guid submittedBy,
            CancellationToken ct = default)
            => await SendAsync(
                submittedBy,
                "Đề tài đã được nộp",
                $"Đề tài '{projectName}' đã được nộp thành công.",
                NotificationType.Info,
                NotificationCategory.Project,
                "/student/my-topic",
                ct);

        public async Task NotifyProjectApprovedAsync(
            Guid projectId,
            string projectName,
            Guid groupLeaderId,
            CancellationToken ct = default)
            => await SendAsync(
                groupLeaderId,
                "Đề tài được duyệt",
                $"Đề tài '{projectName}' đã được duyệt.",
                NotificationType.Success,
                NotificationCategory.Project,
                "/student/my-topic",
                ct);

        public async Task NotifyEvaluationAssignedAsync(
            Guid evaluatorId,
            Guid projectId,
            string projectName,
            CancellationToken ct = default)
            => await SendAsync(
                evaluatorId,
                "Phân công thẩm định",
                $"Bạn được phân công thẩm định đề tài '{projectName}'.",
                NotificationType.Info,
                NotificationCategory.Evaluation,
                $"/lecturer/moderate/{projectId}",
                ct);

        public async Task SendTopicExpirationWarningAsync(Project topic, CancellationToken ct = default)
        {
            if (topic.SubmittedBy is null)
            {
                _logger.LogWarning("Cannot send expiration warning for topic {TopicId}: SubmittedBy is null.", topic.Id);
                return;
            }

            await SendAsync(
                topic.SubmittedBy.Value,
                "Đề tài sắp hết hạn",
                $"Đề tài '{topic.NameVi}' sẽ hết hạn.",
                NotificationType.Warning,
                NotificationCategory.Deadline,
                "/lecturer",
                ct);
        }

        public async Task NotifyMeetingApprovedAsync(
            Guid requesterId,
            string meetingTitle,
            DateTime scheduledTime,
            CancellationToken ct = default)
            => await SendAsync(
                requesterId,
                "Lịch hẹn được duyệt",
                $"Lịch hẹn '{meetingTitle}' được duyệt vào {scheduledTime:dd/MM/yyyy HH:mm}.",
                NotificationType.Success,
                NotificationCategory.Meeting,
                null,
                ct);

        public async Task NotifyDefenseScheduledAsync(
            IEnumerable<Guid> memberIds,
            string projectName,
            DateTime defenseTime,
            string location,
            CancellationToken ct = default)
            => await SendToMultipleAsync(
                memberIds,
                "Lịch bảo vệ đồ án",
                $"Đề tài '{projectName}' bảo vệ vào {defenseTime:dd/MM/yyyy HH:mm} tại {location}.",
                NotificationType.Info,
                NotificationCategory.Defense,
                null,
                ct);
    }
}
