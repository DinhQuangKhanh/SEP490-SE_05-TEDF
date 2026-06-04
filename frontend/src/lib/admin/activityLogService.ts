import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";
import type {
  ActivityLogFilters,
  ActivityLogResponse,
  ErrorDetailsResponse,
  ErrorLogDetail,
  GroupedActivityLogResponse,
  SeveritySummary,
} from "@/types/activityLogs/activityLog.types";

export const activityLogService = {
  /**
   * Fetches a paginated, filtered list of user activity logs (flat list).
   */
  getLogs: (filters: ActivityLogFilters = {}): Promise<ActivityLogResponse> => {
    const params = buildParams(filters);
    return apiClient.get<ActivityLogResponse>(`${routes.admin.activityLogs}?${params.toString()}`);
  },

  /**
   * Fetches grouped activity logs (one row per user+action) with severity counts and role totals.
   */
  getGroupedLogs: (filters: ActivityLogFilters = {}): Promise<GroupedActivityLogResponse> => {
    const params = buildParams(filters);
    return apiClient.get<GroupedActivityLogResponse>(`${routes.admin.groupedActivityLogs}?${params.toString()}`);
  },

  /**
   * Fetches aggregate severity counts (info, warning, error, critical + total).
   */
  getSeveritySummary: (role?: string, from?: string, to?: string): Promise<SeveritySummary> => {
    const params = new URLSearchParams();
    if (role) params.set("role", role);
    if (from) params.set("from", from);
    if (to) params.set("to", to);

    return apiClient.get<SeveritySummary>(`${routes.admin.severitySummary}?${params.toString()}`);
  },

  /**
   * Fetches error details for a specific (userId, action) pair.
   */
  getErrorDetails: (userId: string, action: string, from?: string, to?: string): Promise<ErrorDetailsResponse> => {
    const params = new URLSearchParams();
    params.set("userId", userId);
    params.set("action", action);
    if (from) params.set("from", from);
    if (to) params.set("to", to);

    return apiClient.get<ErrorDetailsResponse>(`${routes.admin.errorDetails}?${params.toString()}`);
  },

  /**
   * Fetches full error log detail by ID (stack trace, inner exceptions, request params).
   */
  getErrorLogDetail: (id: string): Promise<ErrorLogDetail> => {
    return apiClient.get<ErrorLogDetail>(routes.admin.errorLogDetail(id));
  },
};

// ── Helpers ──────────────────────────────────────────────────────────────────
function buildParams(filters: ActivityLogFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.role) params.set("role", filters.role);
  if (filters.category) params.set("category", filters.category);
  if (filters.severity) params.set("severity", filters.severity);
  if (filters.search) params.set("search", filters.search);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  params.set("page", String(filters.page ?? 1));
  params.set("pageSize", String(filters.pageSize ?? 20));
  return params;
}
