import { apiClient } from "./apiClient";

// ── Types ────────────────────────────────────────────────────────────────────

export interface MentorTopicItem {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  majorName: string;
  sourceType: number; // 0=FromPool, 1=DirectRegistration
  sourceTypeName: string;
  status: number; // ProjectStatus enum
  statusName: string;
  submittedAt: string | null;
  createdAt: string;
  semesterName: string;
}

export interface MentorTopicsResponse {
  items: MentorTopicItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SemesterOption {
  id: number;
  name: string;
  code: string;
  startDate: string;
  endDate: string;
  status: string;
}

export interface MentorTopicFilters {
  semesterId?: number;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface UpdatePoolTopicPayload {
  nameVi: string;
  nameEn: string;
  nameAbbr: string;
  description: string;
  objectives: string;
  scope?: string | null;
  technologies?: string | null;
  expectedResults?: string | null;
  maxStudents: number;
}

// ── Service ──────────────────────────────────────────────────────────────────

export const mentorTopicService = {
  getTopics: (filters: MentorTopicFilters = {}): Promise<MentorTopicsResponse> => {
    const params = new URLSearchParams();
    if (filters.semesterId != null) params.set("semesterId", String(filters.semesterId));
    if (filters.search) params.set("search", filters.search);
    params.set("page", String(filters.page ?? 1));
    params.set("pageSize", String(filters.pageSize ?? 10));
    return apiClient.get<MentorTopicsResponse>(`/api/mentor/topics?${params.toString()}`);
  },

  getSemesters: (): Promise<SemesterOption[]> => {
    return apiClient.get<SemesterOption[]>("/api/semesters");
  },

  /** Update a pool-based topic (mentor edits after NeedsModification) */
  updatePoolTopic: (projectId: string, data: UpdatePoolTopicPayload): Promise<void> => {
    return apiClient.put<void>(`/api/mentor/topics/${projectId}/update`, data);
  },

  /** Resubmit a pool-based topic for evaluation */
  resubmitPoolTopic: (projectId: string): Promise<void> => {
    return apiClient.put<void>(`/api/mentor/topics/${projectId}/resubmit`);
  },
};

// ── Helpers ──────────────────────────────────────────────────────────────────

/** Map SourceType enum to display text */
export function sourceTypeLabel(sourceType: number): string {
  return sourceType === 0 ? "Trong kho" : "Đăng ký trực tiếp";
}

/** Map ProjectStatus enum to display badge config */
export function statusConfig(status: number): { label: string; bg: string; text: string; dot: string } {
  switch (status) {
    case 1: // PendingEvaluation
      return { label: "Chờ duyệt", bg: "bg-amber-50", text: "text-amber-700", dot: "bg-amber-500" };
    case 2: // NeedsModification
      return { label: "Yêu cầu sửa", bg: "bg-rose-50", text: "text-rose-700", dot: "bg-rose-500" };
    case 3: // Approved
      return { label: "Đã duyệt", bg: "bg-emerald-50", text: "text-emerald-700", dot: "bg-emerald-500" };
    case 4: // Rejected
      return { label: "Từ chối", bg: "bg-red-50", text: "text-red-700", dot: "bg-red-500" };
    case 5: // InProgress
      return { label: "Đang thực hiện", bg: "bg-blue-50", text: "text-blue-700", dot: "bg-blue-500" };
    case 6: // Completed
      return { label: "Hoàn thành", bg: "bg-teal-50", text: "text-teal-700", dot: "bg-teal-500" };
    case 7: // Cancelled
      return { label: "Đã hủy", bg: "bg-slate-100", text: "text-slate-500", dot: "bg-slate-400" };
    case 8: // PendingMentorReview
      return { label: "Chờ GV duyệt", bg: "bg-violet-50", text: "text-violet-700", dot: "bg-violet-500" };
    default: // Draft=0 or unknown
      return { label: "Nháp", bg: "bg-slate-100", text: "text-slate-600", dot: "bg-slate-400" };
  }
}

/** Map ProjectStatus to evaluation submission status display */
export function evaluationStatusConfig(status: number): { label: string; bg: string; text: string; dot: string } {
  switch (status) {
    case 0: // Draft - not submitted yet
    case 2: // NeedsModification - not yet sent to evaluation
    case 8: // PendingMentorReview - not yet sent to evaluation
      return { label: "Chưa gửi thẩm định", bg: "bg-slate-50", text: "text-slate-600", dot: "bg-slate-400" };
    case 1: // PendingEvaluation - waiting for evaluation council
      return { label: "Chờ thẩm định", bg: "bg-amber-50", text: "text-amber-700", dot: "bg-amber-500" };
    case 3: // Approved
      return { label: "Đã duyệt", bg: "bg-emerald-50", text: "text-emerald-700", dot: "bg-emerald-500" };
    case 4: // Rejected
      return { label: "Từ chối", bg: "bg-red-50", text: "text-red-700", dot: "bg-red-500" };
    case 5: // InProgress
      return { label: "Đang thực hiện", bg: "bg-blue-50", text: "text-blue-700", dot: "bg-blue-500" };
    case 6: // Completed
      return { label: "Hoàn thành", bg: "bg-teal-50", text: "text-teal-700", dot: "bg-teal-500" };
    case 7: // Cancelled
      return { label: "Đã hủy", bg: "bg-slate-100", text: "text-slate-500", dot: "bg-slate-400" };
    default:
      return { label: "Chưa xác định", bg: "bg-slate-100", text: "text-slate-600", dot: "bg-slate-400" };
  }
}
