import { useState, useRef, useEffect, useCallback } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { apiClient } from "@/lib/apiClient";
import { useSignalR } from "@/hooks/useSignalR";

// ─── Types ──────────────────────────────────────────────────────────────────

// Notification types for different roles
export type UserRole = "admin" | "mentor" | "student" | "evaluator" | "department-head";

interface NotificationItem {
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

interface NotificationListResponse {
  items: NotificationItem[];
  totalCount: number;
  unreadCount: number;
}

interface UnreadCountResponse {
  unreadCount: number;
}

interface NotificationDropdownProps {
  role?: UserRole;
  isNavy?: boolean;
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

function relativeTime(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diffMs / 60000);
  if (mins < 1) return "Vừa xong";
  if (mins < 60) return `${mins} phút trước`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours} giờ trước`;
  const days = Math.floor(hours / 24);
  if (days === 1) return "Hôm qua";
  return `${days} ngày trước`;
}

type NotiType = NotificationItem["type"];

const typeColors: Record<NotiType, { bg: string; icon: string }> = {
  Info: { bg: "bg-blue-50", icon: "text-blue-600" },
  Warning: { bg: "bg-amber-50", icon: "text-amber-600" },
  Success: { bg: "bg-green-50", icon: "text-green-600" },
  Error: { bg: "bg-red-50", icon: "text-red-600" },
};

function categoryIcon(category: string): string {
  const map: Record<string, string> = {
    Project: "description",
    Evaluation: "rate_review",
    Meeting: "event",
    Message: "chat",
    TopicPool: "library_books",
    Group: "group",
    Defense: "school",
    Deadline: "alarm",
    System: "campaign",
  };
  return map[category] ?? "notifications";
}

// ─── Component ───────────────────────────────────────────────────────────────

export function NotificationDropdown({ isNavy = false }: NotificationDropdownProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const fetchedRef = useRef(false); // prevent duplicate fetch on first open

  const [pulse, setPulse] = useState(false);

  // ── Fetch unread count (badge) on mount ──────────────────────────────────
  const fetchUnreadCount = useCallback(async () => {
    try {
      const res = await apiClient.get<UnreadCountResponse>("/api/notifications/unread-count");
      setUnreadCount(Number(res.unreadCount ?? 0));
    } catch {
      // silently ignore badge errors
    }
  }, []);

  useEffect(() => {
    fetchUnreadCount();
  }, [fetchUnreadCount]);

  // ── SignalR real-time notifications ─────────────────────────────────────
  const handleReceiveNotification = useCallback((raw: unknown) => {
    const n = raw as NotificationItem;
    if (!n?.id) return;
    setNotifications((prev) => [n, ...prev.filter((x) => x.id !== n.id)]);
    setUnreadCount((prev) => prev + 1);
    setPulse(true);
    setTimeout(() => setPulse(false), 2000);
  }, []);

  useSignalR({ onReceiveNotification: handleReceiveNotification });

  // ── Fetch notifications list when dropdown opens ──────────────────────────
  const fetchNotifications = useCallback(async () => {
    if (fetchedRef.current) return;
    fetchedRef.current = true;
    setLoading(true);
    setError(null);
    try {
      const res = await apiClient.get<NotificationListResponse>("/api/notifications?limit=20");
      setNotifications(res.items ?? []);
      setUnreadCount(Number(res.unreadCount ?? 0));
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Không thể tải thông báo");
    } finally {
      setLoading(false);
    }
  }, []);

  const handleOpen = useCallback(() => {
    setIsOpen((prev) => {
      if (!prev) {
        fetchedRef.current = false; // allow re-fetch every open
        fetchNotifications();
      }
      return !prev;
    });
  }, [fetchNotifications]);

  // ── Close on outside click ────────────────────────────────────────────────
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const markAsRead = useCallback(async (id: string, targetUrl: string | null) => {
    setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
    setUnreadCount((prev) => Math.max(0, prev - 1));
    window.dispatchEvent(new Event("notificationRead"));

    try {
      await apiClient.put(`/api/notifications/${id}/read`, {});
    } catch {
      // optimistic update — revert if it fails
      setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: false } : n)));
      setUnreadCount((prev) => prev + 1);
    }

    if (targetUrl) {
      window.location.href = targetUrl;
    }
  }, []);

  const markAllAsRead = useCallback(async () => {
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    setUnreadCount(0);
    window.dispatchEvent(new Event("notificationRead"));
    try {
      await apiClient.put("/api/notifications/read-all", {});
    } catch {
      // silently ignore — badge will self-correct on next poll
    }
  }, []);

  // ─────────────────────────────────────────────────────────────────────────
  return (
    <div className="relative z-[9999]" ref={dropdownRef}>
      {/* Bell button */}
      <button
        id="notification-bell"
        onClick={handleOpen}
        className={`relative p-2 rounded-lg transition-all duration-200 ${
          isNavy
            ? "text-blue-200 hover:text-white hover:bg-white/10"
            : "text-slate-500 hover:text-primary hover:bg-slate-100"
        }`}
        aria-label="Mở thông báo"
      >
        <span className={`material-symbols-outlined text-[24px] ${pulse ? "animate-bounce" : ""}`}>notifications</span>
        {unreadCount > 0 && (
          <span
            className={`absolute -top-1 -right-1 min-w-[18px] h-[18px] flex items-center justify-center text-[10px] font-bold text-white rounded-full ${isNavy ? "bg-red-500" : "bg-primary"}`}
          >
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown panel */}
      <AnimatePresence>
        {isOpen && (
          <motion.div
            initial={{ opacity: 0, y: 10, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 10, scale: 0.95 }}
            transition={{ duration: 0.15 }}
            className="absolute right-0 top-10 w-96 bg-white rounded-xl shadow-xl border border-slate-200 overflow-hidden z-[9999]"
          >
            {/* Header */}
            <div className="flex items-center justify-between px-4 py-3 border-b bg-slate-50 border-slate-200">
              <div className="flex items-center gap-2">
                <span className="material-symbols-outlined text-primary text-[20px]">notifications</span>
                <h3 className="font-bold text-slate-800">Thông báo</h3>
                {unreadCount > 0 && (
                  <span className="bg-primary/10 text-primary text-xs font-bold px-2 py-0.5 rounded-full">
                    {unreadCount} mới
                  </span>
                )}
              </div>
              {unreadCount > 0 && (
                <button onClick={markAllAsRead} className="text-xs font-medium text-primary hover:text-primary-dark">
                  Đánh dấu tất cả đã đọc
                </button>
              )}
            </div>

            {/* Body */}
            <div className="max-h-[400px] overflow-y-auto">
              {loading ? (
                <div className="flex flex-col gap-3 p-4">
                  {[1, 2, 3].map((i) => (
                    <div key={i} className="flex gap-3 animate-pulse">
                      <div className="rounded-lg size-10 bg-slate-200 shrink-0" />
                      <div className="flex-1 space-y-2">
                        <div className="w-3/4 h-3 rounded bg-slate-200" />
                        <div className="w-full h-3 rounded bg-slate-200" />
                        <div className="w-1/3 h-2 rounded bg-slate-200" />
                      </div>
                    </div>
                  ))}
                </div>
              ) : error ? (
                <div className="flex flex-col items-center gap-2 px-4 py-10 text-center">
                  <span className="text-3xl text-red-400 material-symbols-outlined">error</span>
                  <p className="text-sm text-slate-500">{error}</p>
                  <button
                    onClick={() => {
                      fetchedRef.current = false;
                      fetchNotifications();
                    }}
                    className="text-xs font-medium text-primary hover:underline"
                  >
                    Thử lại
                  </button>
                </div>
              ) : notifications.length === 0 ? (
                <div className="flex flex-col items-center gap-2 px-4 py-10 text-center">
                  <span className="text-4xl material-symbols-outlined text-slate-300">notifications_off</span>
                  <p className="text-sm font-medium text-slate-400">Không có thông báo nào</p>
                </div>
              ) : (
                notifications.map((n) => {
                  const colors = typeColors[n.type] ?? typeColors.Info;
                  return (
                    <motion.div
                      key={n.id}
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      onClick={() => markAsRead(n.id, n.targetUrl)}
                      className={`px-4 py-3 border-b border-slate-100 hover:bg-slate-50 cursor-pointer transition-colors ${!n.isRead ? "bg-blue-50/50" : ""}`}
                    >
                      <div className="flex gap-3">
                        <div
                          className={`w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0 ${colors.bg}`}
                        >
                          <span className={`material-symbols-outlined text-[20px] ${colors.icon}`}>
                            {categoryIcon(n.category)}
                          </span>
                        </div>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-start justify-between gap-2">
                            <p
                              className={`text-sm leading-snug ${!n.isRead ? "font-bold text-slate-800" : "font-medium text-slate-700"}`}
                            >
                              {n.title}
                            </p>
                            {!n.isRead && <span className="w-2 h-2 bg-primary rounded-full flex-shrink-0 mt-1.5" />}
                          </div>
                          <p className="text-xs text-slate-500 mt-0.5 line-clamp-2">{n.content}</p>
                          <p className="text-[10px] text-slate-400 mt-1">{relativeTime(n.createdAt)}</p>
                        </div>
                      </div>
                    </motion.div>
                  );
                })
              )}
            </div>

            {/* Footer */}
            <div className="px-4 py-3 border-t bg-slate-50 border-slate-200">
              <button className="w-full text-sm font-medium text-center text-primary hover:text-primary-dark">
                Xem tất cả thông báo
              </button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
