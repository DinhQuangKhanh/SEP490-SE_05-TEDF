import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";
import type {
  ActivityLogFilters,
  ActivityLogResponse,
  ActivityLogSummary,
  ErrorLogDetail,
  ErrorLogFilters,
  ErrorLogResponse,
} from "@/types/activityLogs/activityLog.types";

export const activityLogService = {
  getLogs: (filters: ActivityLogFilters = {}): Promise<ActivityLogResponse> => {
    const params = buildActivityParams(filters);
    return apiClient.get<ActivityLogResponse>(`${routes.admin.activityLogs}?${params.toString()}`);
  },

  getSummary: (role?: string, from?: string, to?: string): Promise<ActivityLogSummary> => {
    const params = new URLSearchParams();
    if (role) params.set("role", role);
    if (from) params.set("from", from);
    if (to) params.set("to", to);
    return apiClient.get<ActivityLogSummary>(`${routes.admin.activityLogsSummary}?${params.toString()}`);
  },

  getErrorLogs: (filters: ErrorLogFilters = {}): Promise<ErrorLogResponse> => {
    const params = buildErrorParams(filters);
    return apiClient.get<ErrorLogResponse>(`${routes.admin.errorLogs}?${params.toString()}`);
  },

  getErrorLogDetail: (id: string): Promise<ErrorLogDetail> => {
    return apiClient.get<ErrorLogDetail>(routes.admin.errorLogDetail(id));
  },

  clearActivityLogs: (olderThanDays?: number): Promise<{ deletedCount: number }> => {
    const params = new URLSearchParams();
    if (olderThanDays && olderThanDays > 0) params.set("olderThanDays", String(olderThanDays));
    const qs = params.toString();
    return apiClient.delete<{ deletedCount: number }>(
      qs ? `${routes.admin.activityLogs}?${qs}` : routes.admin.activityLogs,
    );
  },

  clearErrorLogs: (olderThanDays?: number): Promise<{ deletedCount: number }> => {
    const params = new URLSearchParams();
    if (olderThanDays && olderThanDays > 0) params.set("olderThanDays", String(olderThanDays));
    const qs = params.toString();
    return apiClient.delete<{ deletedCount: number }>(
      qs ? `${routes.admin.errorLogs}?${qs}` : routes.admin.errorLogs,
    );
  },
};

function buildActivityParams(filters: ActivityLogFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.role) params.set("role", filters.role);
  if (filters.featureCategory) params.set("featureCategory", filters.featureCategory);
  if (filters.status) params.set("status", filters.status);
  if (filters.search) params.set("search", filters.search);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  params.set("page", String(filters.page ?? 1));
  params.set("pageSize", String(filters.pageSize ?? 20));
  return params;
}

function buildErrorParams(filters: ErrorLogFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.severity) params.set("severity", filters.severity);
  if (filters.source) params.set("source", filters.source);
  if (filters.search) params.set("search", filters.search);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  params.set("page", String(filters.page ?? 1));
  params.set("pageSize", String(filters.pageSize ?? 20));
  return params;
}
