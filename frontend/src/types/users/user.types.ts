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
  /** Bộ môn (Department) — CF/SE/AI/IA/IC. Set for lecturers. */
  departmentId: number | null;
  departmentName: string | null;
  /** Chuyên ngành (Major) — snapshotted onto the semester roster by the system, read-only. */
  majorId: number | null;
  majorCode: string | null;
  majorName: string | null;
  /** Chương trình đào tạo (Programs) — e.g. BIT_SE_K18C. Set for students. */
  programId: number | null;
  programCode: string | null;
  programName: string | null;
  /** Chuyên ngành hẹp (Combo) — e.g. .NET. Set for students. */
  comboId: number | null;
  comboName: string | null;
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

/** Body for POST /api/users — create a single user (admin). */
export interface CreateUserRequest {
  role: string; // Student | Mentor | Evaluator | DepartmentHead
  email: string;
  fullName: string;
  code: string; // MSSV for Student, employee code for staff
  phone?: string;
  academicTitle?: string; // staff only
  majorId?: number; // student's major (also resolves the department)
}

/** One rejected/skipped row in a user import. */
export interface UserImportIssue {
  code: string;
  reason: string;
}

/** Result of POST /api/users/import. */
export interface UserImportResponse {
  totalProcessed: number;
  successfullyImported: number;
  issues: UserImportIssue[];
}
