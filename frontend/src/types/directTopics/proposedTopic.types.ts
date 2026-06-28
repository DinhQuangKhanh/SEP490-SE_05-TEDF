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
  /** The student's own program (read-only on the form); validated server-side. */
  majorId: number;
}

export interface AvailableMentor {
  mentorId: string;
  fullName: string;
  email: string;
  academicTitle: string | null;
  currentGroupCount: number;
  maxGroups: number;
}

/** GET /api/direct-topics/available-mentors — the student's program + mentors rostered for it. */
export interface AvailableMentorsResponse {
  majorId: number;
  majorName: string;
  mentors: AvailableMentor[];
}

/** PUT /api/mentor/direct-registration/{projectId}/review */
export interface MentorReviewRequest {
  action: "approve" | "requestModification";
  feedback?: string;
}
