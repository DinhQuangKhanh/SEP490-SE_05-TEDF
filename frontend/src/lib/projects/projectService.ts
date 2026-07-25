import { DepartmentProjectsResponse, ProjectAuditLogResponse, ProjectDetail, ProjectDetailRaw, ProjectFilters, ProjectListResponse, SupervisedProjectFilters, SupervisedProjectsResponse } from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const projectService = {
  getProjects: (filters: ProjectFilters = {}): Promise<ProjectListResponse> => {
    const params = buildParams(filters);
    return apiClient.get<ProjectListResponse>(`${routes.admin.projects}?${params.toString()}`);
  },

  /** Department head: projects within the caller's department (with evaluator assignments). */
  getDepartmentProjects: (): Promise<DepartmentProjectsResponse> =>
    apiClient.get<DepartmentProjectsResponse>(routes.departmentHead.projects),

  /** Mentor: projects the current user supervises (for the profile supervision history). */
  getMySupervised: (filters: SupervisedProjectFilters = {}): Promise<SupervisedProjectsResponse> => {
    const params = new URLSearchParams();
    if (filters.search) params.set("search", filters.search);
    if (filters.sort) params.set("sort", filters.sort);
    params.set("page", String(filters.page ?? 1));
    params.set("pageSize", String(filters.pageSize ?? 10));
    return apiClient.get<SupervisedProjectsResponse>(`${routes.mentor.supervisedProjects}?${params.toString()}`);
  },

  /** Get full detail of a project by ID. Reuses the topic detail endpoint. */
  getProjectDetail: async (projectId: string): Promise<ProjectDetail> => {
    const raw = await apiClient.get<ProjectDetailRaw>(routes.topics.detail(projectId));
    return {
      ...raw,
      technologies: raw.technologies ?? raw.techologies ?? null,
    };
  },

  /** Get audit logs for a project */
  getAuditLogs: (projectId: string): Promise<ProjectAuditLogResponse> => {
    return apiClient.get<ProjectAuditLogResponse>(`${routes.admin.projects}/${projectId}/audit-logs`);
  },
};

// ── Helpers ──────────────────────────────────────────────────────────────────
function buildParams(filters: ProjectFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.search) params.set("search", filters.search);
  if (filters.semesterId != null) params.set("semesterId", String(filters.semesterId));
  if (filters.status) params.set("status", filters.status);
  if (filters.majorId != null) params.set("majorId", String(filters.majorId));
  params.set("page", String(filters.page ?? 1));
  params.set("pageSize", String(filters.pageSize ?? 20));
  return params;
}
