/** POST /api/student/{groupId}/propose-topic */
export interface CreateProposedTopicRequest {
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

/** PUT /api/mentor/direct-registration/{projectId}/review */
export interface MentorReviewRequest {
  action: "approve" | "requestModification";
  feedback?: string;
}
