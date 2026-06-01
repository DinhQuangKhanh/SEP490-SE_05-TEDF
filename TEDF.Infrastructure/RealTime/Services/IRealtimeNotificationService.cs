using TEDF.Infrastructure.RealTime.Models;

namespace TEDF.Infrastructure.RealTime.Services
{
    /// <summary>
    /// Service interface for real-time notifications via SignalR.
    /// </summary>
    public interface IRealtimeNotificationService
    {
        #region User-targeted messaging

        /// <summary>
        /// Sends a message to a specific user.
        /// </summary>
        Task SendToUserAsync(Guid userId, string method, object message, CancellationToken ct = default);

        /// <summary>
        /// Sends a message to multiple users.
        /// </summary>
        Task SendToUsersAsync(IEnumerable<Guid> userIds, string method, object message, CancellationToken ct = default);

        #endregion

        #region Group-targeted messaging

        /// <summary>
        /// Sends a message to a named group.
        /// </summary>
        Task SendToGroupAsync(string groupName, string method, object message, CancellationToken ct = default);

        /// <summary>
        /// Sends a message to a project group.
        /// </summary>
        Task SendToProjectGroupAsync(Guid projectId, string method, object message, CancellationToken ct = default);

        #endregion

        #region Broadcast messaging

        /// <summary>
        /// Sends a message to all connected clients.
        /// </summary>
        Task SendToAllAsync(string method, object message, CancellationToken ct = default);

        /// <summary>
        /// Sends a message to all clients except specified connections.
        /// </summary>
        Task SendToAllExceptAsync(IEnumerable<string> excludedConnectionIds, string method, object message, CancellationToken ct = default);

        #endregion

        #region Typed notification helpers

        /// <summary>
        /// Sends a notification alert to a specific user.
        /// </summary>
        Task SendNotificationAlertAsync(Guid userId, NotificationAlert alert, CancellationToken ct = default);

        /// <summary>
        /// Sends a project status update to project group members.
        /// </summary>
        Task SendProjectStatusUpdateAsync(Guid projectId, ProjectStatusUpdate update, CancellationToken ct = default);

        /// <summary>
        /// Sends a meeting reminder to participants.
        /// </summary>
        Task SendMeetingReminderAsync(IEnumerable<Guid> participantIds, MeetingReminder reminder, CancellationToken ct = default);

        /// <summary>
        /// Sends a defense notification to recipients.
        /// </summary>
        Task SendDefenseNotificationAsync(IEnumerable<Guid> recipientIds, DefenseNotification notification, CancellationToken ct = default);

        /// <summary>
        /// Sends an unread count update to a user.
        /// </summary>
        Task SendUnreadCountUpdateAsync(Guid userId, long unreadCount, CancellationToken ct = default);

        #endregion
    }
}
