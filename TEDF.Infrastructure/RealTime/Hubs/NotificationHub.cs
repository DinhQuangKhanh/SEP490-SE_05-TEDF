using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using AppClaimTypes = TEDF.Application.Common.AppClaimTypes;

namespace TEDF.Infrastructure.RealTime.Hubs
{
    /// <summary>
    /// SignalR hub for real-time notifications.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation("User {UserId} connected to NotificationHub", userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Joins a project group channel for project-specific notifications.
        /// </summary>
        public async Task JoinProjectChannel(Guid projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project_{projectId}");
            _logger.LogDebug("Connection joined project channel: {ProjectId}", projectId);
        }

        /// <summary>
        /// Leaves a project group channel.
        /// </summary>
        public async Task LeaveProjectChannel(Guid projectId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project_{projectId}");
            _logger.LogDebug("Connection left project channel: {ProjectId}", projectId);
        }

        /// <summary>
        /// Joins a group channel for group-specific notifications.
        /// </summary>
        public async Task JoinGroupChannel(Guid groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
            _logger.LogDebug("Connection joined group channel: {GroupId}", groupId);
        }

        /// <summary>
        /// Leaves a group channel.
        /// </summary>
        public async Task LeaveGroupChannel(Guid groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
            _logger.LogDebug("Connection left group channel: {GroupId}", groupId);
        }

        /// <summary>
        /// Marks a notification as read and notifies the caller.
        /// </summary>
        public async Task MarkNotificationAsRead(Guid notificationId)
        {
            await Clients.Caller.SendAsync("NotificationRead", notificationId);
        }

        private Guid? GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(AppClaimTypes.DbUserId)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
