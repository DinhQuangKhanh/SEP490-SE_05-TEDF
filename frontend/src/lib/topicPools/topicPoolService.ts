import {
  DepartmentWithPoolsDto,
  MentorRegistrationRequestDto,
  TopicPoolDto,
  TopicPoolStatisticsDto,
  UpdatePoolTopicRequest,
} from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const topicPoolService = {
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

  /** Mentor edits a pool topic after NeedsModification. */
  updatePoolTopic: (projectId: string, data: UpdatePoolTopicRequest): Promise<void> =>
    apiClient.put<void>(routes.mentor.topicUpdate(projectId), data),

  /** Mentor resubmits a pool topic for evaluation. */
  resubmitPoolTopic: (projectId: string): Promise<void> =>
    apiClient.put<void>(routes.mentor.topicResubmit(projectId)),

  /** Pending registration requests for the current mentor's pool topics. */
  getMentorRegistrations: (): Promise<MentorRegistrationRequestDto[]> =>
    apiClient.get<MentorRegistrationRequestDto[]>(routes.topicPools.mentorRegistrations),

  /** Mentor confirms a registration (assigns the group to the topic). */
  confirmRegistration: (registrationId: string): Promise<void> =>
    apiClient.put<void>(routes.topicPools.confirmRegistration(registrationId)),

  /** Mentor rejects a registration with a reason. */
  rejectRegistration: (registrationId: string, reason: string): Promise<void> =>
    apiClient.put<void>(routes.topicPools.rejectRegistration(registrationId), { reason }),
};
