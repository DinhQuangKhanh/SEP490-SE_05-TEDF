// SignalR client method names sent by NotificationHub/ChatHub. Keep in sync with
// backend/TEDF.Infrastructure/RealTime/RealtimeEvents.cs — same strings, same casing.
export const SignalREvents = {
  ReceiveNotification: "ReceiveNotification",
  UnreadCountUpdated: "UnreadCountUpdated",
  ProjectStatusUpdated: "ProjectStatusUpdated",
  ReceiveRegistrationUpdate: "ReceiveRegistrationUpdate",
  MeetingReminder: "MeetingReminder",
  DefenseScheduled: "DefenseScheduled",
  NotificationRead: "NotificationRead",
  NewMessage: "NewMessage",
  UserTyping: "UserTyping",
} as const;
