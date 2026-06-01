import { apiClient } from "./apiClient";

// ── Types ────────────────────────────────────────────────────────────────────

/** A single thesis topic available in the pool for student browsing. */
export interface TopicInPoolItem {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  description: string;
  technologies: string | null;
  majorId: number;
  majorName: string;
  majorCode: string;
  poolStatus: number;
  poolStatusName: string;
  maxStudents: number;
  mentorName: string;
  mentorId: string;
  createdAt: string;
}

/** Full detail of a thesis topic — works for all source types (pool or direct registration). */
export interface TopicDetail {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  nameAbbr: string;
  description: string;
  objectives: string;
  scope: string | null;
  technologies: string | null;
  expectedResults: string | null;
  majorId: number;
  majorName: string;
  majorCode: string;
  poolStatus: number;
  poolStatusName: string;
  maxStudents: number;
  mentors: MentorSummary[];
  createdAt: string;
  updatedAt: string | null;
}

type TopicDetailRaw = TopicDetail & {
  techologies?: string | null;
};

export interface MentorSummary {
  mentorId: string;
  fullName: string;
}

export interface TopicsInPoolResponse {
  items: TopicInPoolItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface TopicFilters {
  majorId?: number;
  search?: string;
  poolStatus?: number;
  sortBy?: string;
  page?: number;
  pageSize?: number;
}

export interface MajorOption {
  id: number;
  name: string;
  code: string;
}

export interface TopicDocument {
  id: string;
  fileName: string;
  originalFileName: string;
  fileType: string;
  fileSize: number;
  documentType: string;
  description: string | null;
  uploadedAt: string;
  uploadedByName: string;
}

// ── Service ──────────────────────────────────────────────────────────────────

export const topicPoolService = {
  /** Get paginated list of topics available in pool for student browsing. */
  getTopics: (filters: TopicFilters = {}): Promise<TopicsInPoolResponse> => {
    const params = buildParams(filters);
    return apiClient.get<TopicsInPoolResponse>(`/api/topics?${params.toString()}`);
  },

  /** Get full detail of a topic by ID. Works for FromPool and DirectRegistration. */
  getTopicDetail: (topicId: string): Promise<TopicDetail> => {
    return apiClient.get<TopicDetailRaw>(`/api/topics/${topicId}`).then((raw) => ({
      ...raw,
      technologies: raw.technologies ?? raw.techologies ?? null,
    }));
  },

  getMajors: (): Promise<MajorOption[]> => {
    return apiClient.get<MajorOption[]>("/api/majors");
  },

  registerTopic: (params: { projectId: string; groupId: string; note?: string }) => {
    return apiClient.post(`/api/student-groups/${params.groupId}/topic-registrations`, {
      projectId: params.projectId,
      note: params.note,
    });
  },

  /** Get documents attached to a topic. */
  getTopicDocuments: (topicId: string): Promise<TopicDocument[]> => {
    return apiClient.get<TopicDocument[]>(`/api/topics/${topicId}/documents`);
  },

  /** Upload documents to a topic. Returns queued count for malware scanning. */
  uploadTopicDocuments: (topicId: string, files: File[]): Promise<{ queuedCount: number }> => {
    const formData = new FormData();
    files.forEach((f) => formData.append("attachments", f));
    return apiClient.postForm<{ queuedCount: number }>(`/api/topics/${topicId}/documents`, formData);
  },
};

// ── Helpers ──────────────────────────────────────────────────────────────────

function buildParams(filters: TopicFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.majorId != null) params.set("majorId", String(filters.majorId));
  if (filters.search) params.set("search", filters.search);
  if (filters.poolStatus != null) params.set("poolStatus", String(filters.poolStatus));
  if (filters.sortBy) params.set("sortBy", filters.sortBy);
  params.set("page", String(filters.page ?? 1));
  params.set("pageSize", String(filters.pageSize ?? 12));
  return params;
}
