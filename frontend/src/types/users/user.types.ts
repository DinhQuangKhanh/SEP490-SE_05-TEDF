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

export interface MyProfile {
  id: string;
  fullName: string;
  email: string;
  avatarUrl: string | null;
  studentCode: string | null;
  employeeCode: string | null;
  phoneNumber: string | null;
  birthDate: string | null;
  privacySettings: string | null;
  academicTitle: string | null;
  departmentId: number | null;
  departmentName: string | null;
  /** Chuyên ngành (Major) — set for students. */
  majorId: number | null;
  majorName: string | null;
  /** Chuyên ngành hẹp (MajorProgram) — set for students. ProgramCode = CurriculumCode_ComboCode. */
  majorProgramId: number | null;
  majorProgramCode: string | null;
  majorProgramDescription: string | null;
  /** Bộ môn đang giảng dạy (CF/SE/AI/IA/IC) — set for lecturers. */
  division: string | null;
  status: string;
  roles: string[];
  createdAt: string;
  lastLoginAt: string | null;
}

export interface UpdateMyProfileRequest {
  phoneNumber?: string | null;
  birthDate?: string | null;
  privacySettings?: string | null;
}
