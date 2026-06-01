import { useState, useEffect, useCallback } from "react";
import { apiClient } from "@/lib/apiClient";

export function useUnreadSupportCount() {
  const [count, setCount] = useState<number>(0);

  const fetchCount = useCallback(async () => {
    try {
      const res = await apiClient.get<{ unreadCount: number }>("/api/notifications/unread-count?category=Support");
      setCount(Number(res.unreadCount ?? 0));
    } catch {
      // ignore
    }
  }, []);

  useEffect(() => {
    fetchCount();

    const handleUpdate = () => {
      fetchCount();
    };

    window.addEventListener("notificationRead", handleUpdate);
    const interval = setInterval(fetchCount, 60_000);

    return () => {
      window.removeEventListener("notificationRead", handleUpdate);
      clearInterval(interval);
    };
  }, [fetchCount]);

  return count;
}
