import { apiClient } from "./apiClient";

// ── Types ────────────────────────────────────────────────────────────────────

export interface ProjectReviewDetail {
  projectId: string;
  projectCode: string;
  nameVi: string;
  nameEn: string;
  nameAbbr: string | null;
  description: string;
  objectives: string;
  scope: string | null;
  technologies: string | null;
  expectedResults: string | null;
  maxStudents: number;
  submittedAt: string | null;
  evaluationCount: number;
  majorName: string;
  majorCode: string;
  semesterName: string;
  mentorName: string;
  studentName: string;
  studentAvatar: string | null;
  assignmentId: string;
  assignedAt: string;
  daysElapsed: number;
  existingFeedback: string | null;
  existingResult: string | null;
}

export interface SimilarTitle {
  projectId: string;
  projectCode: string;
  nameEn: string;
  nameVi: string;
  semesterName: string;
  similarity: number;
  commonKeywords: string[];
  // Comparison panel fields
  description: string;
  objectives: string;
  scope: string | null;
  technologies: string | null;
  expectedResults: string | null;
  mentorName: string;
  studentName: string;
}

// ── API ──────────────────────────────────────────────────────────────────────

export const evaluatorService = {
  getProjectForReview: (projectId: string) =>
    apiClient.get<ProjectReviewDetail>(`/api/evaluator/projects/${projectId}/review`),

  checkSimilarity: (projectId: string) =>
    apiClient.get<SimilarTitle[]>(`/api/evaluator/projects/${projectId}/similarity`),

  submitEvaluation: (projectId: string, data: { result: number; feedback?: string }) =>
    apiClient.post<string>(`/api/evaluator/projects/${projectId}/evaluate`, data),
};
