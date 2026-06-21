using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TEDF.Infrastructure.RealTime.Hubs;
using TEDF.Infrastructure.RealTime.Models;
using TEDF.Infrastructure.RealTime;

namespace TEDF.Infrastructure.RealTime.Services
{
    /// <summary>
    /// SignalR-based real-time notification service implementation.
    /// </summary>
    public class RealtimeNotificationService : IRealtimeNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<RealtimeNotificationService> _logger;

        public RealtimeNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<RealtimeNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        #region User-targeted messaging

        /// <inheritdoc/>
        public async Task SendToUserAsync(Guid userId, string method, object message, CancellationToken ct = default)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"user_{userId}")
                    .SendAsync(method, message, ct);

                _logger.LogDebug("Realtime notification sent to user {UserId}: {Method}", userId, method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending realtime notification to user {UserId}", userId);
            }
        }

        /// <inheritdoc/>
        public async Task SendToUsersAsync(IEnumerable<Guid> userIds, string method, object message, CancellationToken ct = default)
        {
            try
            {
                var groups = userIds.Select(id => $"user_{id}").ToList();
                await _hubContext.Clients
                    .Groups(groups)
                    .SendAsync(method, message, ct);

                _logger.LogDebug("Realtime notification sent to {Count} users: {Method}", groups.Count, method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending realtime notification to multiple users");
            }
        }

        #endregion

        #region Group-targeted messaging

        /// <inheritdoc/>
        public async Task SendToGroupAsync(string groupName, string method, object message, CancellationToken ct = default)
        {
            try
            {
                await _hubContext.Clients
                    .Group(groupName)
                    .SendAsync(method, message, ct);

                _logger.LogDebug("Realtime notification sent to group {Group}: {Method}", groupName, method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending realtime notification to group {Group}", groupName);
            }
        }

        /// <inheritdoc/>
        public async Task SendToProjectGroupAsync(Guid projectId, string method, object message, CancellationToken ct = default)
        {
            await SendToGroupAsync($"project_{projectId}", method, message, ct);
        }

        #endregion

        #region Broadcast messaging

        /// <inheritdoc/>
        public async Task SendToAllAsync(string method, object message, CancellationToken ct = default)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync(method, message, ct);

                _logger.LogDebug("Realtime notification broadcast: {Method}", method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting realtime notification");
            }
        }

        /// <inheritdoc/>
        public async Task SendToAllExceptAsync(IEnumerable<string> excludedConnectionIds, string method, object message, CancellationToken ct = default)
        {
            try
            {
                var excluded = excludedConnectionIds.ToList();

                await _hubContext.Clients
                    .AllExcept(excluded)
                    .SendAsync(method, message, ct);

                _logger.LogDebug("Realtime notification sent to all except {Count} connections: {Method}",
                    excluded.Count, method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending realtime notification to all except");
            }
        }

        #endregion

        #region Typed notification helpers

        /// <inheritdoc/>
        public async Task SendNotificationAlertAsync(Guid userId, NotificationAlert alert, CancellationToken ct = default)
        {
            await SendToUserAsync(userId, RealtimeEvents.ReceiveNotification, alert, ct);
        }

        /// <inheritdoc/>
        public async Task SendProjectStatusUpdateAsync(Guid projectId, ProjectStatusUpdate update, CancellationToken ct = default)
        {
            await SendToProjectGroupAsync(projectId, RealtimeEvents.ProjectStatusUpdated, update, ct);
        }

        /// <inheritdoc/>
        public async Task SendMeetingReminderAsync(IEnumerable<Guid> participantIds, MeetingReminder reminder, CancellationToken ct = default)
        {
            await SendToUsersAsync(participantIds, RealtimeEvents.MeetingReminder, reminder, ct);
        }

        /// <inheritdoc/>
        public async Task SendDefenseNotificationAsync(IEnumerable<Guid> recipientIds, DefenseNotification notification, CancellationToken ct = default)
        {
            await SendToUsersAsync(recipientIds, RealtimeEvents.DefenseScheduled, notification, ct);
        }

        /// <inheritdoc/>
        public async Task SendUnreadCountUpdateAsync(Guid userId, long unreadCount, CancellationToken ct = default)
        {
            await SendToUserAsync(userId, RealtimeEvents.UnreadCountUpdated, new { count = unreadCount }, ct);
        }

        #endregion
    }
}
