namespace TEDF.Infrastructure.RealTime
{
    /// <summary>
    /// SignalR client method names used by NotificationHub/ChatHub and RealtimeNotificationService.
    /// Centralized here so backend and frontend stay in sync on casing/spelling.
    /// </summary>
    public static class RealtimeEvents
    {
        public const string ReceiveNotification = "ReceiveNotification";
        public const string UnreadCountUpdated = "UnreadCountUpdated";
        public const string ProjectStatusUpdated = "ProjectStatusUpdated";
        public const string MeetingReminder = "MeetingReminder";
        public const string DefenseScheduled = "DefenseScheduled";
        public const string NotificationRead = "NotificationRead";
        public const string NewMessage = "NewMessage";
        public const string UserTyping = "UserTyping";
    }
}
