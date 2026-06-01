using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Notifications.Commands.MarkAllAsRead;

/// <summary>
/// Command to mark all notifications as read for the current user.
/// </summary>
[ActionLog("Mark All Notifications Read", "Notification")]
public record MarkAllNotificationsAsReadCommand() : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["notifications:{userId}:"];
}
