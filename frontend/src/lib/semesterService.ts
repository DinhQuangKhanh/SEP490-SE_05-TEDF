import { apiClient } from "./apiClient";
import type { SemesterDto } from "@/types/admin.types";

// ── Types ──────────────────────────────────────────────────

export type { SemesterDto, SemesterPhaseDto } from "@/types/admin.types";

/** A phase as sent when creating a semester. */
export interface CreateSemesterPhasePayload {
  name: string;
  type: string;
  startDate: string; // ISO
  endDate: string; // ISO
}

export interface CreateSemesterPayload {
  name: string;
  code: string;
  startDate: string; // ISO
  endDate: string; // ISO
  academicYearStart: number;
  description: string | null;
  phases: CreateSemesterPhasePayload[];
}

/** A phase as sent when updating a semester (existing phase id + new dates). */
export interface UpdateSemesterPhasePayload {
  id: number;
  startDate: string; // ISO
  endDate: string; // ISO
}

export interface UpdateSemesterPayload {
  id: number;
  name: string;
  startDate: string; // ISO
  endDate: string; // ISO
  description: string | null;
  phases: UpdateSemesterPhasePayload[];
}

export interface MajorOption {
  id: number;
  name: string;
}

// ── Service ────────────────────────────────────────────────

export const semesterService = {
  /** List semesters, optionally filtered by status (Upcoming | Ongoing | Ended). */
  getSemesters: (status?: string): Promise<SemesterDto[]> => {
    const query = status ? `?status=${encodeURIComponent(status)}` : "";
    return apiClient.get<SemesterDto[]>(`/api/admin/semesters${query}`);
  },

  getSemesterById: (id: number): Promise<SemesterDto> =>
    apiClient.get<SemesterDto>(`/api/admin/semesters/${id}`),

  getActiveSemester: (): Promise<SemesterDto> =>
    apiClient.get<SemesterDto>(`/api/admin/semesters/active`),

  createSemester: (payload: CreateSemesterPayload): Promise<{ id: number }> =>
    apiClient.post<{ id: number }>(`/api/admin/semesters`, payload),

  updateSemester: (id: number, payload: UpdateSemesterPayload): Promise<void> =>
    apiClient.put<void>(`/api/admin/semesters/${id}`, payload),

  deleteSemester: (id: number): Promise<void> =>
    apiClient.delete<void>(`/api/admin/semesters/${id}`),

  /** Upload the eligible-students list (.csv/.xlsx) for a semester. */
  importEligibleStudents: (id: number, file: File): Promise<unknown> => {
    const formData = new FormData();
    formData.append("file", file);
    return apiClient.postForm<unknown>(`/api/admin/semesters/${id}/eligible-students/import`, formData);
  },

  /** Majors used by the eligible-students filter in the create/edit semester modals. */
  getMajors: (): Promise<MajorOption[]> => apiClient.get<MajorOption[]>(`/api/majors`),
};
