import { apiClient } from './apiClient'

// ── Types ──────────────────────────────────────────

export interface AdminStats {
    totalStudents: number
    totalMentors: number
    totalRegisteredTopics: number
    highPriorityPending: number
}

export interface SemesterPhaseInfo {
    name: string
    type: number   // 0=Registration, 1=Evaluation, 2=Implementation, 3=Defense
    status: number // 0=NotStarted, 1=InProgress, 2=Completed
    startDate: string
    endDate: string
    order: number
}

export interface SemesterProgress {
    semesterName: string
    phases: SemesterPhaseInfo[]
}

export interface ApprovalRate {
    approved: number
    rejected: number
    inProgress: number
    pending: number
    total: number
}

export interface RecentTicket {
    code: string
    title: string
    reporterName: string
    category: number  // 0=Technical, 1=Academic, 2=Account, 3=Other
    priority: number  // 0=Low, 1=Medium, 2=High, 3=Urgent
    status: number    // 0=Open, 1=InProgress, 2=Resolved, 3=Closed
    createdAt: string
}

export interface AdminDashboardData {
    stats: AdminStats
    semesterProgress: SemesterProgress | null
    approvalRate: ApprovalRate
    recentTickets: RecentTicket[]
}

// ── Mentor Dashboard Types ──────────────────────────────

export interface MentorStats {
    totalGroups: number
    totalStudents: number
    pendingEvaluation: number
    approvedProjects: number
    inProgressProjects: number
    totalProjects: number
}

export interface RecentProject {
    id: string
    code: string
    nameVi: string
    nameEn: string
    status: number
    sourceType: number
    groupName: string | null
    leaderName: string | null
    memberCount: number
    createdAt: string
    submittedAt: string | null
}

export interface MentorDashboardData {
    mentorName: string
    stats: MentorStats
    semesterProgress: SemesterProgress | null
    recentProjects: RecentProject[]
}

// ── Service ──────────────────────────────────────────

export const dashboardService = {
    getAdminDashboard: (): Promise<AdminDashboardData> =>
        apiClient.get<AdminDashboardData>('/api/admin/dashboard'),

    getMentorDashboard: (): Promise<MentorDashboardData> =>
        apiClient.get<MentorDashboardData>('/api/mentor/dashboard'),
}
