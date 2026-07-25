// ── Activity log (flat list) ─────────────────────────────────────────────────
export interface ActivityLogItem {
  id: string;
  userId: string;
  userName: string;
  userEmail: string | null;
  role: string;
  actionCode: string;
  actionName: string;
  featureCategory: string;
  requestPath: string;
  requestMethod: string;
  entityType: string | null;
  entityId: string | null;
  status: "Success" | "Failure";
  durationMs: number;
  correlationId: string | null;
  ipAddress: string | null;
  timestamp: string;
}

export interface ActivityLogResponse {
  items: ActivityLogItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ActivityLogFilters {
  role?: string;
  featureCategory?: string;
  status?: string;
  search?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

// ── Summary ──────────────────────────────────────────────────────────────────
export interface ActivityLogSummary {
  roleCounts: Record<string, number>;
  success: number;
  failure: number;
  total: number;
}

// ── Error logs (list) ────────────────────────────────────────────────────────
export interface ErrorLogItem {
  id: string;
  userId: string | null;
  userName: string | null;
  activeRole: string | null;
  severity: string;
  source: string;
  actionCode: string | null;
  requestPath: string;
  requestMethod: string;
  errorMessage: string;
  errorType: string;
  correlationId: string | null;
  timestamp: string;
}

export interface ErrorLogResponse {
  items: ErrorLogItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ErrorLogFilters {
  severity?: string;
  source?: string;
  search?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

// ── Error log detail ─────────────────────────────────────────────────────────
export interface InnerException {
  message: string;
  type: string;
  stackTrace: string | null;
}

export interface ErrorLogDetail {
  id: string;
  userId: string | null;
  userName: string | null;
  userEmail: string | null;
  activeRole: string | null;
  severity: string;
  source: string;
  actionCode: string | null;
  action: string | null;
  routePath: string | null;
  requestPath: string;
  requestMethod: string;
  errorMessage: string;
  errorType: string;
  stackTrace: string | null;
  innerExceptions: InnerException[];
  correlationId: string | null;
  timestamp: string;
}
