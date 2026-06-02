import { apiClient } from "./apiClient";

// ── Types matching backend DTOs ──────────────────────────────────────────────

export interface MentorSummary {
  mentorId: string;
  mentorName: string;
}

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
  evaluators: EvaluatorAssignment[];
  mentors: MentorSummary[];
  hasConflict: boolean;
  needsFinalDecision: boolean;
  assignedEvaluatorCount: number;
}

export interface DepartmentProjectsResponse {
  items: DepartmentProject[];
  totalCount: number;
  pendingAssignmentCount: number;
  inEvaluationCount: number;
  needsFinalDecisionCount: number;
  completedCount: number;
}

export interface DepartmentEvaluator {
  userId: string;
  fullName: string;
  email: string;
  academicTitle: string | null;
  activeAssignmentCount: number;
}

// ── Grouped data for UI ──────────────────────────────────────────────────────

export interface GroupedProjects {
  pendingAssignment: DepartmentProject[];
  inEvaluation: DepartmentProject[];
  needsDecision: DepartmentProject[];
  completed: DepartmentProject[];
}

/** StatusValue 1 = PendingEvaluation in the backend enum */
const STATUS_PENDING_EVALUATION = 1;

export function groupProjects(resp: DepartmentProjectsResponse | null | undefined): GroupedProjects {
  const empty: GroupedProjects = { pendingAssignment: [], inEvaluation: [], needsDecision: [], completed: [] };
  if (!resp?.items?.length) return empty;

  const pending: DepartmentProject[] = [];
  const inEval: DepartmentProject[] = [];
  const needs: DepartmentProject[] = [];
  const done: DepartmentProject[] = [];

  for (const p of resp.items) {
    if (p.needsFinalDecision) {
      needs.push(p);
    } else if (p.statusValue !== STATUS_PENDING_EVALUATION) {
      done.push(p);
    } else if (p.assignedEvaluatorCount < 2) {
      pending.push(p);
    } else {
      inEval.push(p);
    }
  }

  return { pendingAssignment: pending, inEvaluation: inEval, needsDecision: needs, completed: done };
}

// ── Dashboard types ─────────────────────────────────────────────────────────

export interface DepartmentHeadDashboardData {
  departmentName: string;
  headName: string;
  stats: DepartmentHeadStats;
  semesterProgress: SemesterProgressInfo | null;
  evaluationProgress: EvaluationProgress;
  recentActivities: RecentActivity[];
}

export interface DepartmentHeadStats {
  totalProjects: number;
  pendingAssignment: number;
  inEvaluation: number;
  needsFinalDecision: number;
  completed: number;
  totalEvaluators: number;
  totalMentors: number;
}

export interface SemesterProgressInfo {
  semesterName: string;
  phases: SemesterPhaseInfo[];
}

export interface SemesterPhaseInfo {
  name: string;
  type: number;
  status: number; // 0=NotStarted, 1=InProgress, 2=Completed
  startDate: string;
  endDate: string;
  order: number;
}

export interface EvaluationProgress {
  approved: number;
  rejected: number;
  needsModification: number;
  pending: number;
}

export interface RecentActivity {
  projectId: string;
  projectCode: string;
  projectName: string;
  activityType: string; // "submitted" | "assigned" | "decided"
  actorName: string;
  occurredAt: string;
}

// ── API ──────────────────────────────────────────────────────────────────────

export const departmentHeadService = {
  getDashboard: () =>
    apiClient.get<DepartmentHeadDashboardData>("/api/department-head/dashboard"),

  getProjects: () =>
    apiClient.get<DepartmentProjectsResponse>("/api/department-head/projects"),

  getEvaluators: () =>
    apiClient.get<DepartmentEvaluator[]>("/api/department-head/evaluators"),

  assignEvaluator: (projectId: string, evaluatorId: string, order: number) =>
    apiClient.post("/api/department-head/assign-evaluator", {
      projectId,
      evaluatorId,
      evaluatorOrder: order,
    }),

  submitFinalDecision: (projectId: string, result: number, notes?: string) =>
    apiClient.post(`/api/department-head/projects/${projectId}/final-decision`, {
      result,
      notes,
    }),
};
