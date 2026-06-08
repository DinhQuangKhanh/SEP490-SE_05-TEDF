using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Notifications.DTOs;

namespace TEDF.Application.Features.Notifications.Queries.GetUserNotifications;

/// <summary>
/// Query to get the current user's notifications.
/// </summary>
public record GetUserNotificationsQuery(int Limit = 50) : IQuery<NotificationListResponseDto>;
