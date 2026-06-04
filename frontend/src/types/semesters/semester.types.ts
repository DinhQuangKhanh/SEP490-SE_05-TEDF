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
  phases: SemesterPhaseDto[];
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
