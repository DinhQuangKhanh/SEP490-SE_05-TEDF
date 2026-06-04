import { UserFilters, UserListResponse } from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const userService = {
  getUsers: (filters: UserFilters = {}): Promise<UserListResponse> => {
    const params = buildParams(filters);
    return apiClient.get<UserListResponse>(`${routes.admin.users}?${params.toString()}`);
  },

  lockUser: (userId: string): Promise<void> => apiClient.put<void>(`${routes.admin.users}/${userId}/lock`, {}),

  unlockUser: (userId: string): Promise<void> => apiClient.put<void>(`${routes.admin.users}/${userId}/unlock`, {}),

  /** Assign a user as head of a department. */
  assignDepartmentHead: (departmentId: number, userId: string): Promise<void> =>
    apiClient.post<void>(routes.admin.assignDepartmentHead(departmentId), { userId }),
};

// ── Helpers ──────────────────────────────────────────────────────────────────
function buildParams(filters: UserFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.role) params.set("role", filters.role);
  if (filters.search) params.set("search", filters.search);
  params.set("page", String(filters.page ?? 1));
  params.set("pageSize", String(filters.pageSize ?? 20));
  return params;
}
