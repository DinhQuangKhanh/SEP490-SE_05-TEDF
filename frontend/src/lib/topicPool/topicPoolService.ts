import {
  DepartmentWithPoolsDto,
  TopicDetail,
  TopicDetailRaw,
  TopicDocument,
  TopicFilters,
  TopicPoolDto,
  TopicPoolStatisticsDto,
  TopicsInPoolResponse,
} from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const topicPoolService = {
  // ── Pool catalog ──────────────────────────────────────────────────────────
  /** All topic pools (optionally filtered by major). */
  getTopicPools: (majorId?: number): Promise<TopicPoolDto[]> =>
    apiClient.get<TopicPoolDto[]>(`${routes.topicPools.list}${majorId != null ? `?majorId=${majorId}` : ""}`),

  getTopicPoolsByDepartment: (): Promise<DepartmentWithPoolsDto[]> =>
    apiClient.get<DepartmentWithPoolsDto[]>(routes.topicPools.byDepartment),

  getTopicPoolById: (id: string): Promise<TopicPoolDto> => apiClient.get<TopicPoolDto>(routes.topicPools.byId(id)),

  getTopicPoolStatistics: (id: string): Promise<TopicPoolStatisticsDto> =>
    apiClient.get<TopicPoolStatisticsDto>(routes.topicPools.statistics(id)),

  /** Mentor proposes a new topic into a pool (multipart: fields + attachments). */
  proposeTopic: (poolId: string, formData: FormData): Promise<{ id: string }> =>
    apiClient.postForm<{ id: string }>(routes.topicPools.propose(poolId), formData),

  // ── Topics in a pool ──────────────────────────────────────────────────────
  /** Paginated list of topics available in pool for student browsing. */
  getTopics: (filters: TopicFilters = {}): Promise<TopicsInPoolResponse> => {
    const params = buildParams(filters);
    return apiClient.get<TopicsInPoolResponse>(`${routes.topics.list}?${params.toString()}`);
  },

  /** Full detail of a topic by ID. Works for FromPool and DirectRegistration. */
  getTopicDetail: (topicId: string): Promise<TopicDetail> => {
    return apiClient.get<TopicDetailRaw>(routes.topics.detail(topicId)).then((raw) => ({
      ...raw,
      technologies: raw.technologies ?? null,
    }));
  },

  /** Get documents attached to a topic. */
  getTopicDocuments: (topicId: string): Promise<TopicDocument[]> =>
    apiClient.get<TopicDocument[]>(routes.topics.documents(topicId)),

  /** Upload documents to a topic. Returns queued count for malware scanning. */
  uploadTopicDocuments: (topicId: string, files: File[]): Promise<{ queuedCount: number }> => {
    const formData = new FormData();
    files.forEach((file) => formData.append("attachments", file));
    return apiClient.postForm<{ queuedCount: number }>(routes.topics.documents(topicId), formData);
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
