// Notifications — bell dropdown + upload-status polling.

export interface NotificationDto {
  id: string;
  userId: string;
  title: string;
  content: string;
  type: "Info" | "Warning" | "Success" | "Error";
  category: string;
  targetUrl: string | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

/** GET /api/notifications */
export interface NotificationListResponse {
  items: NotificationDto[];
  totalCount: number;
  unreadCount: number;
}

/** GET /api/notifications/unread-count */
export interface UnreadCountResponse {
  unreadCount: number;
}
