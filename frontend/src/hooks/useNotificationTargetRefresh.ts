import { useEffect } from "react";
import { NOTIFICATION_TARGET_REFRESH_EVENT } from "@/components/layout";

/**
 * Runs `onRefresh` when a clicked notification's targetUrl matches the route the
 * user is already on (see NotificationDropdown — react-router won't remount/refetch
 * on a no-op navigate, so it dispatches this event instead).
 */
export function useNotificationTargetRefresh(onRefresh: () => void) {
  useEffect(() => {
    window.addEventListener(NOTIFICATION_TARGET_REFRESH_EVENT, onRefresh);
    return () => window.removeEventListener(NOTIFICATION_TARGET_REFRESH_EVENT, onRefresh);
  }, [onRefresh]);
}
