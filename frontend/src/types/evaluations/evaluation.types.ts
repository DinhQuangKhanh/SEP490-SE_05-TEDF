import { MentorSummary } from "../projects/project.types";

// ── Review a single project (evaluator) ─────────────────────────────────────────

/** GET /api/evaluations/projects/{id}/review */
export interface ProjectReviewResponse {
  projectId: string;
  projectCode: string;
  nameVi: string;
  nameEn: string;
  nameAbbr: string | null;
  description: string;
  objectives: string;
  scope: string | null;
  technologies: string | null;
  expectedResults: string | null;
  maxStudents: number;
  submittedAt: string | null;
  evaluationCount: number;
  majorName: string;
  majorCode: string;
  semesterName: string;
  mentorName: string;
  studentName: string;
  studentAvatar: string | null;
  assignmentId: string;
  assignedAt: string;
  daysElapsed: number;
  existingFeedback: string | null;
  existingResult: string | null;
}

/** Element of GET /api/evaluations/projects/{id}/similarity */
export interface SimilarTitleDto {
  projectId: string;
  projectCode: string;
  nameEn: string;
  nameVi: string;
  semesterName: string;
  similarity: number;
  commonKeywords: string[];
  description: string;
  objectives: string;
  scope: string | null;
  technologies: string | null;
  expectedResults: string | null;
  mentorName: string;
  studentName: string;
}

/** POST /api/evaluations/projects/{id}/evaluate */
export interface SubmitEvaluationRequest {
  result: number;
  feedback?: string;
}

// ── Filter options ────────────────────────────────────────────────────────────
export interface FilterOptionDto {
  value: number;
  label: string;
}

/** GET /api/evaluations/filter-options */
export interface EvaluatorFilterOptionsResponse {
  semesters: FilterOptionDto[];
  majors: FilterOptionDto[];
}

// ── Assigned projects list (evaluator) ──────────────────────────────────────────
export interface EvaluatorProjectItemDto {
  assignmentId: string;
  projectId: string;
  projectCode: string;
  projectNameVi: string;
  majorName: string;
  studentName: string;
  studentAvatar: string | null;
  mentorName: string;
  submittedAt: string | null;
  assignedAt: string;
  individualResult: string;
  isUrgent: boolean;
}

/** GET /api/evaluations/projects */
export interface EvaluatorProjectsResponse {
  items: EvaluatorProjectItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface EvaluatorProjectsFilters {
  page?: number;
  pageSize?: number;
  search?: string;
  semesterId?: number;
  majorId?: number;
  result?: string;
}

// ── Evaluation history (evaluator) ───────────────────────────────────────────────
export interface EvaluatorHistoryStatsDto {
  totalReviewed: number;
  approvedCount: number;
  needsModificationCount: number;
  rejectedCount: number;
}

export interface EvaluatorHistoryItemDto {
  projectId: string;
  projectCode: string;
  projectNameVi: string;
  studentName: string;
  studentAvatar: string | null;
  evaluatedAt: string;
  result: string;
  feedback: string | null;
}

/** GET /api/evaluations/history */
export interface EvaluatorHistoryResponse {
  stats: EvaluatorHistoryStatsDto;
  items: EvaluatorHistoryItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface EvaluatorHistoryFilters {
  page?: number;
  pageSize?: number;
  search?: string;
  result?: string;
  dateRange?: string;
}

// ── Department-head evaluation management ───────────────────────────────────────
// (assign evaluators, view department projects/evaluators, submit final decision)
export interface EvaluatorAssignment {
  assignmentId: string;
  evaluatorId: string;
  evaluatorName: string;
  evaluatorOrder: number;
  individualResult: string | null; // "Approved" | "Rejected" | "NeedsModification" | null
  individualResultValue: number | null;
  feedback: string | null;
  evaluatedAt: string | null;
  hasSubmitted: boolean;
}

export interface DepartmentProject {
  projectId: string;
  projectCode: string;
  nameVi: string;
  nameEn: string;
  majorName: string;
  semesterName: string;
  status: string;
  statusValue: number;
  submittedAt: string | null;
  /** Fallback date for topics never submitted — `submittedAt` is null for those. */
  createdAt: string;
  evaluators: EvaluatorAssignment[];
  mentors: MentorSummary[];
  hasConflict: boolean;
  needsFinalDecision: boolean;
  assignedEvaluatorCount: number;
}

/** GET /api/projects/department */
export interface DepartmentProjectsResponse {
  items: DepartmentProject[];
  totalCount: number;
  pendingAssignmentCount: number;
  inEvaluationCount: number;
  needsFinalDecisionCount: number;
  completedCount: number;
}

/** GET /api/evaluations/evaluators */
export interface DepartmentEvaluator {
  userId: string;
  fullName: string;
  email: string;
  academicTitle: string | null;
  activeAssignmentCount: number;
}

// ── Grouped data for the assign-evaluators UI ───────────────────────────────────
export interface GroupedProjects {
  pendingAssignment: DepartmentProject[];
  inEvaluation: DepartmentProject[];
  needsDecision: DepartmentProject[];
  completed: DepartmentProject[];
}

/** StatusValue 1 = PendingEvaluation in the backend enum */
const STATUS_PENDING_EVALUATION = 1;

export function groupProjects(resp: DepartmentProjectsResponse | null | undefined): GroupedProjects {
  const empty: GroupedProjects = {
    pendingAssignment: [],
    inEvaluation: [],
    needsDecision: [],
    completed: [],
  };

  if (!resp?.items?.length) return empty;

  const pending: DepartmentProject[] = [];
  const inEval: DepartmentProject[] = [];
  const needs: DepartmentProject[] = [];
  const done: DepartmentProject[] = [];

  for (const project of resp.items) {
    if (project.needsFinalDecision) {
      needs.push(project);
    } else if (project.statusValue !== STATUS_PENDING_EVALUATION) {
      done.push(project);
    } else if (project.assignedEvaluatorCount < 2) {
      pending.push(project);
    } else {
      inEval.push(project);
    }
  }

  // Finished topics are read as a history, so the newest one comes first.
  done.sort(byNewestReviewFirst);

  return { pendingAssignment: pending, inEvaluation: inEval, needsDecision: needs, completed: done };
}

/**
 * When the topic was last acted on: the latest evaluator verdict, falling back to the
 * submission date for topics whose evaluators have not recorded a date.
 */
export function reviewedAt(project: DepartmentProject): string | null {
  const dates = project.evaluators
    .map((e) => e.evaluatedAt)
    .filter((d): d is string => !!d)
    .sort((a, b) => a.localeCompare(b));
  return dates.length > 0 ? dates[dates.length - 1] : (project.submittedAt ?? project.createdAt ?? null);
}

/** Date shown as "Ngày gửi": the submission date, or the creation date for a topic never submitted. */
export function submittedOrCreatedAt(project: DepartmentProject): string | null {
  return project.submittedAt ?? project.createdAt ?? null;
}

/** Sort comparator: most recently reviewed first, topics without any date last. */
export function byNewestReviewFirst(a: DepartmentProject, b: DepartmentProject): number {
  const left = Date.parse(reviewedAt(a) ?? "");
  const right = Date.parse(reviewedAt(b) ?? "");
  if (Number.isNaN(left) && Number.isNaN(right)) return 0;
  if (Number.isNaN(left)) return 1;
  if (Number.isNaN(right)) return -1;
  return right - left;
}
