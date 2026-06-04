// Dashboard payloads for every role's home screen (mirrors backend Dashboard feature).
import { SemesterProgressInfo } from "../semesters/semester.types";

// ── Admin dashboard ───────────────────────────────────────────────────────────
export interface AdminStats {
  totalStudents: number;
  totalMentors: number;
  totalRegisteredTopics: number;
  highPriorityPending: number;
}

export interface ApprovalRate {
  approved: number;
  rejected: number;
  inProgress: number;
  pending: number;
  total: number;
}

export interface RecentTicket {
  code: string;
  title: string;
  reporterName: string;
  category: number; // 0=Technical, 1=Academic, 2=Account, 3=Other
  priority: number; // 0=Low, 1=Medium, 2=High, 3=Urgent
  status: number; // 0=Open, 1=InProgress, 2=Resolved, 3=Closed
  createdAt: string;
}

export interface AdminDashboardData {
  stats: AdminStats;
  semesterProgress: SemesterProgressInfo | null;
  approvalRate: ApprovalRate;
  recentTickets: RecentTicket[];
}

// ── Mentor dashboard ──────────────────────────────────────────────────────────
export interface MentorStats {
  totalGroups: number;
  totalStudents: number;
  pendingEvaluation: number;
  approvedProjects: number;
  inProgressProjects: number;
  totalProjects: number;
}

export interface RecentProject {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  status: number;
  sourceType: number;
  groupName: string | null;
  leaderName: string | null;
  memberCount: number;
  createdAt: string;
  submittedAt: string | null;
}

export interface MentorDashboardData {
  mentorName: string;
  stats: MentorStats;
  semesterProgress: SemesterProgressInfo | null;
  recentProjects: RecentProject[];
}

// ── Department-head dashboard ───────────────────────────────────────────────────
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

export interface DepartmentHeadDashboardData {
  departmentName: string;
  headName: string;
  stats: DepartmentHeadStats;
  semesterProgress: SemesterProgressInfo | null;
  evaluationProgress: EvaluationProgress;
  recentActivities: RecentActivity[];
}

// ── Evaluator dashboard ─────────────────────────────────────────────────────────
export interface EvaluatorStatsDto {
  totalAssigned: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
  needsModificationCount: number;
  reviewedCount: number;
  avgReviewDays: number | null;
}

export interface PendingEvaluationDto {
  assignmentId: string;
  projectId: string;
  projectCode: string;
  projectNameVi: string;
  majorName: string;
  studentName: string;
  studentAvatar: string | null;
  assignedAt: string;
  daysElapsed: number;
  isUrgent: boolean;
}

export interface RecentReviewedDto {
  projectId: string;
  projectNameVi: string;
  result: string;
  evaluatedAt: string;
}

/** GET /api/dashboard/evaluator */
export interface EvaluatorDashboardResponse {
  stats: EvaluatorStatsDto;
  pendingEvaluations: PendingEvaluationDto[];
  recentReviewed: RecentReviewedDto[];
}
