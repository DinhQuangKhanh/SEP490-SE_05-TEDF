import { apiClient } from "./apiClient";

// ── Types ────────────────────────────────────────────────────────────────────

export interface CreateDirectTopicPayload {
  nameVi: string;
  nameEn: string;
  nameAbbr: string;
  description: string;
  objectives: string;
  scope?: string;
  technologies?: string;
  expectedResults?: string;
  mentorId: string;
  groupId: string;
  majorId: number;
  maxStudents: number;
}

export interface AvailableMentor {
  mentorId: string;
  fullName: string;
  email: string;
  academicTitle: string | null;
  currentGroupCount: number;
  maxGroups: number;
}

export interface MentorReviewPayload {
  action: "approve" | "requestModification";
  feedback?: string;
}

// ── Service ──────────────────────────────────────────────────────────────────

export const directTopicService = {
  createDirectTopic: (data: CreateDirectTopicPayload): Promise<{ id: string }> => {
    const { groupId, ...requestBody } = data;
    return apiClient.post<{ id: string }>(`/api/student/${groupId}/direct-topic`, requestBody);
  },

  submitToMentor: (groupId: string, projectId: string): Promise<void> => {
    return apiClient.put<void>(`/api/student/direct-topic/${groupId}/${projectId}/submit-to-mentor`);
  },

  updateTopic: (projectId: string, data: Partial<CreateDirectTopicPayload>): Promise<void> => {
    return apiClient.put<void>(`/api/student/direct-topic/${projectId}/update`, data);
  },

  getAvailableMentors: (majorId?: number): Promise<AvailableMentor[]> => {
    const params = new URLSearchParams();
    if (majorId != null) params.set("majorId", String(majorId));
    return apiClient.get<AvailableMentor[]>(`/api/student/available-mentors?${params.toString()}`);
  },

  mentorReviewTopic: (projectId: string, payload: MentorReviewPayload): Promise<void> => {
    return apiClient.put<void>(`/api/mentor/topics/${projectId}/review`, payload);
  },
};
