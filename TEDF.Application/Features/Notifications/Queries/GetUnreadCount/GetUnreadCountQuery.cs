using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.Notifications.Queries.GetUnreadCount;

/// <summary>
/// Query to get the count of unread notifications for the current user.
/// </summary>
public record GetUnreadCountQuery(TEDF.Domain.Enums.Notification.NotificationCategory? Category = null) : IQuery<long>;
