import { AvailableMentorsResponse, CreateProposedTopicRequest, MentorReviewRequest } from "@/types";
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

  /** Returns the student's own program (read-only) plus the mentors rostered to supervise it. */
  getAvailableMentors: (): Promise<AvailableMentorsResponse> =>
    apiClient.get<AvailableMentorsResponse>(routes.studentTopics.availableMentors),

  mentorReviewProposedTopic: (projectId: string, payload: MentorReviewRequest): Promise<void> => {
    return apiClient.put<void>(routes.mentor.directRegistrationReview(projectId), payload);
  },
};
