export interface SemesterPhaseDto {
  id: number;
  name: string;
  type: string;
  startDate: string;
  endDate: string;
  order: number;
  status: string;
  durationDays: number;
}

export interface SemesterDto {
  id: number;
  name: string;
  code: string;
  startDate: string;
  endDate: string;
  status: string;
  academicYear: string;
  description: string | null;
  createdAt: string;
  updatedAt: string | null;
  rosterPublishedAt: string | null;
  phases: SemesterPhaseDto[];
}

// ── Eligibility roster (students + mentors) ────────────────────────────────────
export interface EligibleStudentDto {
  studentId: string;
  studentCode: string;
  fullName: string | null;
  email: string | null;
  phoneNumber: string | null;
  majorId: number | null;
  programCode: string | null;
  programName: string | null;
  isEligible: boolean;
  importedAt: string;
}

export interface EligibleMentorDto {
  mentorId: string;
  employeeCode: string;
  fullName: string | null;
  email: string | null;
  phoneNumber: string | null;
  /** Bộ môn giảng viên đang dạy (SE/CF/AI/IA…), snapshot từ file. */
  division: string | null;
  majorId: number | null;
  programCode: string | null;
  programName: string | null;
  isAssigned: boolean;
  importedAt: string;
}

/** Một dòng không được nhập, kèm lý do (để cảnh báo người dùng). */
export interface ImportRowIssue {
  code: string;
  reason: string;
}

/** Result returned by the eligible-students / eligible-mentors import endpoints. */
export interface ImportRosterResponse {
  totalProcessed: number;
  successfullyImported: number;
  issues: ImportRowIssue[];
}

/** Lightweight semester option for dropdowns (mentor topics filter, etc.). */
export interface SemesterOption {
  id: number;
  name: string;
  code: string;
  startDate: string;
  endDate: string;
  status: string;
}

// ── Phase/progress info reused by dashboards (admin, mentor, department head) ──
export interface SemesterPhaseInfo {
  name: string;
  type: number; // 0=Registration, 1=Evaluation, 2=Implementation, 3=Defense
  status: number; // 0=NotStarted, 1=InProgress, 2=Completed
  startDate: string;
  endDate: string;
  order: number;
}

export interface SemesterProgressInfo {
  semesterName: string;
  phases: SemesterPhaseInfo[];
}

// ── Request bodies ────────────────────────────────────────────────────────────
export interface CreateSemesterPhaseRequest {
  name: string;
  type: string;
  startDate: string; // ISO
  endDate: string; // ISO
}

export interface CreateSemesterRequest {
  name: string;
  code: string;
  startDate: string; // ISO
  endDate: string; // ISO
  academicYearStart: number;
  description: string | null;
  phases: CreateSemesterPhaseRequest[];
}

/** A phase as sent when updating a semester (existing phase id + new dates). */
export interface UpdateSemesterPhaseRequest {
  id: number;
  startDate: string; // ISO
  endDate: string; // ISO
}

export interface UpdateSemesterRequest {
  id: number;
  name: string;
  startDate: string; // ISO
  endDate: string; // ISO
  description: string | null;
  phases: UpdateSemesterPhaseRequest[];
}
