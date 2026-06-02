import { apiClient } from "./apiClient";

// ── Types ──────────────────────────────────────────────────

export interface UserListItem {
  id: string;
  fullName: string;
  email: string;
  avatarUrl: string | null;
  studentCode: string | null;
  employeeCode: string | null;
  academicTitle: string | null;
  departmentId: number | null;
  departmentName: string | null;
  status: string;
  roles: string[];
  createdAt: string;
}

export interface UserListResponse {
  items: UserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserFilters {
  role?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

// ── Service ────────────────────────────────────────────────

function buildParams(filters: UserFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.role) params.set("role", filters.role);
  if (filters.search) params.set("search", filters.search);
  params.set("page", String(filters.page ?? 1));
  params.set("pageSize", String(filters.pageSize ?? 20));
  return params;
}

export const userService = {
  getUsers: (filters: UserFilters = {}): Promise<UserListResponse> => {
    const params = buildParams(filters);
    return apiClient.get<UserListResponse>(`/api/admin/users?${params.toString()}`);
  },

  lockUser: (userId: string): Promise<void> => {
    return apiClient.put<void>(`/api/admin/users/${userId}/lock`, {});
  },

  unlockUser: (userId: string): Promise<void> => {
    return apiClient.put<void>(`/api/admin/users/${userId}/unlock`, {});
  },
};
