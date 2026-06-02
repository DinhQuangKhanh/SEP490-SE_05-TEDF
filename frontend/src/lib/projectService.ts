import { apiClient } from "./apiClient";

// ── Types ──────────────────────────────────────────────────

export interface ProjectListItem {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string | null;
  status: string;
  majorName: string;
  majorCode: string;
  semesterName: string;
  sourceType: string;
  mentorNames: string[];
  studentNames: string[];
  groupCode: string | null;
  createdAt: string;
}

export interface ProjectListResponse {
  items: ProjectListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ProjectFilters {
  search?: string;
  semesterId?: number;
  status?: string;
  majorId?: number;
  page?: number;
  pageSize?: number;
}

export interface MentorSummary {
  mentorId: string;
  fullName: string;
}

export interface ProjectDetail {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  nameAbbr: string;
  description: string;
  objectives: string;
  scope: string | null;
  technologies: string | null;
  expectedResults: string | null;
  majorId: number;
  majorName: string;
  majorCode: string;
  poolStatus: number;
  poolStatusName: string;
  maxStudents: number;
  mentors: MentorSummary[];
  createdAt: string;
  updatedAt: string | null;
}

// ── Service ────────────────────────────────────────────────

function buildParams(filters: ProjectFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.search) params.set("search", filters.search);
  if (filters.semesterId) params.set("semesterId", String(filters.semesterId));
  if (filters.status) params.set("status", filters.status);
  if (filters.majorId) params.set("majorId", String(filters.majorId));
  params.set("page", String(filters.page ?? 1));
  params.set("pageSize", String(filters.pageSize ?? 20));
  return params;
}

type ProjectDetailRaw = ProjectDetail & {
  techologies?: string | null;
};

export const projectService = {
  getProjects: (filters: ProjectFilters = {}): Promise<ProjectListResponse> => {
    const params = buildParams(filters);
    return apiClient.get<ProjectListResponse>(
      `/api/admin/projects?${params.toString()}`
    );
  },

  /** Get full detail of a project by ID. Reuses the topics detail endpoint. */
  getProjectDetail: (projectId: string): Promise<ProjectDetail> => {
    return apiClient
      .get<ProjectDetailRaw>(`/api/topics/${projectId}`)
      .then((raw) => ({
        ...raw,
        technologies: raw.technologies ?? raw.techologies ?? null,
      }));
  },
};
