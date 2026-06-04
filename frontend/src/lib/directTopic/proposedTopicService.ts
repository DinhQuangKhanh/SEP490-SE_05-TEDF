import { AvailableMentor, CreateProposedTopicRequest, MentorReviewRequest } from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const proposedTopicService = {
  createProposedTopic: (data: CreateProposedTopicRequest): Promise<{ id: string }> => {
    const { groupId, ...requestBody } = data;
    return apiClient.post<{ id: string }>(routes.studentTopics.createDirectTopic(groupId), requestBody);
  },

  submitToMentor: (groupId: string, projectId: string): Promise<void> => {
    return apiClient.put<void>(routes.studentTopics.submitDirectTopicToMentor(groupId, projectId));
  },

  updateTopic: (projectId: string, data: Partial<CreateProposedTopicRequest>): Promise<void> => {
    return apiClient.put<void>(routes.studentTopics.updateDirectTopic(projectId), data);
  },

  getAvailableMentors: (majorId?: number): Promise<AvailableMentor[]> => {
    const params = new URLSearchParams();
    if (majorId != null) params.set("majorId", String(majorId));
    return apiClient.get<AvailableMentor[]>(`${routes.studentTopics.availableMentors}?${params.toString()}`);
  },

  mentorReviewProposedTopic: (projectId: string, payload: MentorReviewRequest): Promise<void> => {
    return apiClient.put<void>(routes.mentor.directRegistrationReview(projectId), payload);
  },
};
