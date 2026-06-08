import { NotificationListResponse, UnreadCountResponse } from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const notificationService = {
  /** Latest notifications for the current user (bell dropdown / upload-status polling). */
  getNotifications: (limit?: number): Promise<NotificationListResponse> =>
    apiClient.get<NotificationListResponse>(`${routes.notifications.list}${limit ? `?limit=${limit}` : ""}`),

  getUnreadCount: (): Promise<UnreadCountResponse> =>
    apiClient.get<UnreadCountResponse>(routes.notifications.unreadCount),

  markRead: (id: string): Promise<void> => apiClient.put<void>(routes.notifications.markRead(id), {}),

  markAllRead: (): Promise<void> => apiClient.put<void>(routes.notifications.markAllRead, {}),
};
