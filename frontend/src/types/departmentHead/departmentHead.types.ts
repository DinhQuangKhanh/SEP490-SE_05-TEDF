import { MentorSummary } from "../projects/project.types";
import { SemesterProgressInfo } from "../semesters/semester.types";

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

  return {
    pendingAssignment: pending,
    inEvaluation: inEval,
    needsDecision: needs,
    completed: done,
  };
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
