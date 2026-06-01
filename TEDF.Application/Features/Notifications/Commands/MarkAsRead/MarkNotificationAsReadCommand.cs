using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Notifications.Commands.MarkAsRead;

/// <summary>
/// Command to mark a specific notification as read.
/// </summary>
[ActionLog("Mark Notification Read", "Notification")]
public record MarkNotificationAsReadCommand(Guid NotificationId) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["notifications:{userId}:"];
}
