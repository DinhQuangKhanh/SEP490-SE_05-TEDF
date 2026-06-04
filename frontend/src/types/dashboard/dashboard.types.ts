// Dashboard payloads for the Admin and Mentor home screens.
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
