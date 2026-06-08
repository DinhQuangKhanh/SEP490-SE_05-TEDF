// ── Types (flat / legacy) ───────────────────────────────────────────────────
export interface ActivityLogItem {
  id: string;
  userId: string;
  userName: string;
  userEmail: string | null;
  activeRole: string;
  action: string;
  category: string | null;
  entityType: string | null;
  entityId: string | null;
  severity: string;
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
  category?: string;
  severity?: string;
  search?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

// ── Types (grouped) ─────────────────────────────────────────────────────────
export interface SeverityCounts {
  info: number;
  warning: number;
  error: number;
  critical: number;
}

export interface SeveritySummary extends SeverityCounts {
  total: number;
}

export interface GroupedActivityLogItem {
  userId: string;
  userName: string;
  userEmail: string | null;
  activeRole: string;
  action: string;
  category: string | null;
  totalCount: number;
  latestTimestamp: string;
  severityCounts: SeverityCounts;
}

export interface GroupedActivityLogResponse {
  items: GroupedActivityLogItem[];
  totalGroups: number;
  page: number;
  pageSize: number;
  totalPages: number;
  roleCounts: Record<string, number>;
}

export interface ErrorDetailItem {
  message: string;
  errorType: string | null;
  count: number;
  latestAt: string;
}

export interface ErrorDetailsResponse {
  errors: ErrorDetailItem[];
}

// ── Types (error log detail) ────────────────────────────────────────────────
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
