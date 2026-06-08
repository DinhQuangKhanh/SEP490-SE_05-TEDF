import {
  MentorTopicFilters,
  MentorTopicsResponse,
  TopicDetail,
  TopicDetailRaw,
  TopicDocument,
  TopicFilters,
  TopicsInPoolResponse,
} from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const topicService = {
  /** Paginated list of topics available in pool for student browsing. */
  getTopics: (filters: TopicFilters = {}): Promise<TopicsInPoolResponse> => {
    const params = buildParams(filters);
    return apiClient.get<TopicsInPoolResponse>(`${routes.topics.list}?${params.toString()}`);
  },

  /** Full detail of a topic by ID. Works for FromPool and DirectRegistration. */
  getTopicDetail: (topicId: string): Promise<TopicDetail> =>
    apiClient.get<TopicDetailRaw>(routes.topics.detail(topicId)).then((raw) => ({
      ...raw,
      technologies: raw.technologies ?? null,
    })),

  /** Documents attached to a topic. */
  getTopicDocuments: (topicId: string): Promise<TopicDocument[]> =>
    apiClient.get<TopicDocument[]>(routes.topics.documents(topicId)),

  /** Upload documents to a topic. Returns queued count for malware scanning. */
  uploadTopicDocuments: (topicId: string, files: File[]): Promise<{ queuedCount: number }> => {
    const formData = new FormData();
    files.forEach((file) => formData.append("attachments", file));
    return apiClient.postForm<{ queuedCount: number }>(routes.topics.documents(topicId), formData);
  },

  /** Topics owned by the current mentor. */
  getMentorTopics: (filters: MentorTopicFilters = {}): Promise<MentorTopicsResponse> => {
    const params = new URLSearchParams();
    if (filters.semesterId != null) params.set("semesterId", String(filters.semesterId));
    if (filters.search) params.set("search", filters.search);
    params.set("page", String(filters.page ?? 1));
    params.set("pageSize", String(filters.pageSize ?? 10));
    return apiClient.get<MentorTopicsResponse>(`${routes.mentor.topics}?${params.toString()}`);
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
