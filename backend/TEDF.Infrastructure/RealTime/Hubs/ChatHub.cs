using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TEDF.Infrastructure.RealTime.Models;
using AppClaimTypes = TEDF.Application.Common.AppClaimTypes;

namespace TEDF.Infrastructure.RealTime.Hubs
{
    /// <summary>
    /// SignalR hub for real-time chat messaging.
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_user_{userId}");
                _logger.LogInformation("User {UserId} connected to ChatHub", userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_user_{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Joins a conversation for real-time messaging.
        /// </summary>
        public async Task JoinConversation(Guid conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            _logger.LogDebug("User joined conversation: {ConversationId}", conversationId);
        }

        /// <summary>
        /// Leaves a conversation.
        /// </summary>
        public async Task LeaveConversation(Guid conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            _logger.LogDebug("User left conversation: {ConversationId}", conversationId);
        }

        /// <summary>
        /// Sends a message to a conversation.
        /// </summary>
        public async Task SendMessage(Guid conversationId, string content)
        {
            var userId = GetUserId();
            if (userId is null) return;

            var message = new ChatMessage(
                ConversationId: conversationId,
                SenderId: userId.Value,
                Content: content,
                SentAt: DateTime.UtcNow
            );

            await Clients.Group($"conversation_{conversationId}").SendAsync("NewMessage", message);
        }

        /// <summary>
        /// Sends a typing indicator to other participants in a conversation.
        /// </summary>
        public async Task SendTypingIndicator(Guid conversationId, bool isTyping)
        {
            var userId = GetUserId();
            if (userId is null) return;

            var indicator = new TypingIndicator(
                ConversationId: conversationId,
                UserId: userId.Value,
                IsTyping: isTyping
            );

            await Clients.OthersInGroup($"conversation_{conversationId}").SendAsync("UserTyping", indicator);
        }

        private Guid? GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(AppClaimTypes.DbUserId)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
