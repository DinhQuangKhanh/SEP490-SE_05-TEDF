using TEDF.Application.Features.Notifications.DTOs;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Application-layer abstraction for notification operations.
/// Implemented by Infrastructure's NotificationService.
/// </summary>
public interface INotificationService
{
    Task SendAsync(Guid userId, string title, string content, NotificationType type,
        NotificationCategory category, string? targetUrl = null, CancellationToken ct = default);

    Task SendToMultipleAsync(IEnumerable<Guid> userIds, string title, string content,
        NotificationType type, NotificationCategory category, string? targetUrl = null,
        CancellationToken ct = default);

    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, int limit = 50,
        CancellationToken ct = default);

    Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(Guid userId,
        CancellationToken ct = default);

    Task<long> GetUnreadCountAsync(Guid userId, NotificationCategory? category = null, CancellationToken ct = default);

    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);

    Task NotifyProjectSubmittedAsync(Guid projectId, string projectName, Guid submittedBy,
        CancellationToken ct = default);

    Task NotifyProjectApprovedAsync(Guid projectId, string projectName, Guid groupLeaderId,
        CancellationToken ct = default);

    Task NotifyEvaluationAssignedAsync(Guid evaluatorId, Guid projectId, string projectName,
        CancellationToken ct = default);

    Task SendTopicExpirationWarningAsync(Project topic, CancellationToken ct = default);

    Task NotifyMeetingApprovedAsync(Guid requesterId, string meetingTitle, DateTime scheduledTime,
        CancellationToken ct = default);

    Task NotifyDefenseScheduledAsync(IEnumerable<Guid> memberIds, string projectName,
        DateTime defenseTime, string location, CancellationToken ct = default);
}
