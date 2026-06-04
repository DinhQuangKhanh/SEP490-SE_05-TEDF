import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";
import type {
  CreateSemesterRequest,
  SemesterDto,
  SemesterOption,
  UpdateSemesterRequest,
} from "@/types";

export const semesterService = {
  /** List semesters, optionally filtered by status (Upcoming | Ongoing | Ended). */
  getSemesters: (status?: string): Promise<SemesterDto[]> => {
    const query = status ? `?status=${encodeURIComponent(status)}` : "";
    return apiClient.get<SemesterDto[]>(`${routes.admin.semesters}${query}`);
  },

  /** Lightweight semester options (id/name/dates) for filter dropdowns. */
  getSemesterOptions: (): Promise<SemesterOption[]> => apiClient.get<SemesterOption[]>(routes.admin.semesters),

  getSemesterById: (id: number): Promise<SemesterDto> => apiClient.get<SemesterDto>(routes.admin.semesterById(id)),

  getActiveSemester: (): Promise<SemesterDto> => apiClient.get<SemesterDto>(routes.admin.activeSemester),

  createSemester: (payload: CreateSemesterRequest): Promise<{ id: number }> =>
    apiClient.post<{ id: number }>(routes.admin.semesters, payload),

  updateSemester: (id: number, payload: UpdateSemesterRequest): Promise<void> =>
    apiClient.put<void>(routes.admin.semesterById(id), payload),

  deleteSemester: (id: number): Promise<void> => apiClient.delete<void>(routes.admin.semesterById(id)),

  /** Upload the eligible-students list (.csv/.xlsx) for a semester. */
  importEligibleStudents: (id: number, file: File): Promise<unknown> => {
    const formData = new FormData();
    formData.append("file", file);
    return apiClient.postForm<unknown>(routes.admin.eligibleStudentsImport(id), formData);
  },
};
