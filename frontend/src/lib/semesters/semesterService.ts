import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";
import type {
  CreateSemesterRequest,
  EligibleMentorDto,
  EligibleStudentDto,
  ImportRosterResponse,
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
  importEligibleStudents: (id: number, file: File): Promise<ImportRosterResponse> => {
    const formData = new FormData();
    formData.append("file", file);
    return apiClient.postForm<ImportRosterResponse>(routes.admin.eligibleStudentsImport(id), formData);
  },

  /** Upload the supervising-mentors list (.csv/.xlsx) for a semester. */
  importEligibleMentors: (id: number, file: File): Promise<ImportRosterResponse> => {
    const formData = new FormData();
    formData.append("file", file);
    return apiClient.postForm<ImportRosterResponse>(routes.admin.eligibleMentorsImport(id), formData);
  },

  /** Roster preview tables (admin). */
  getEligibleStudents: (id: number): Promise<EligibleStudentDto[]> =>
    apiClient.get<EligibleStudentDto[]>(routes.admin.eligibleStudents(id)),

  getEligibleMentors: (id: number): Promise<EligibleMentorDto[]> =>
    apiClient.get<EligibleMentorDto[]>(routes.admin.eligibleMentors(id)),

  /** Correct a rostered mentor's assigned program (the inline "Program" edit). */
  updateEligibleMentorMajor: (id: number, mentorId: string, majorId: number): Promise<void> =>
    apiClient.put<void>(routes.admin.eligibleMentorMajor(id, mentorId), { majorId }),

  /** Finalize the roster: notify mentors + email eligible students (sent once). */
  publishRoster: (id: number): Promise<void> => apiClient.post<void>(routes.admin.semesterRosterPublish(id), {}),

  /** Permanently remove selected eligible students from the roster. */
  removeEligibleStudents: (id: number, studentIds: string[]): Promise<void> =>
    apiClient.post<void>(routes.admin.eligibleStudentsBulkDelete(id), { ids: studentIds }),

  /** Permanently remove selected eligible mentors from the roster. */
  removeEligibleMentors: (id: number, mentorIds: string[]): Promise<void> =>
    apiClient.post<void>(routes.admin.eligibleMentorsBulkDelete(id), { ids: mentorIds }),
};
