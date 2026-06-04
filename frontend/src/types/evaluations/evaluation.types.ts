// ── Review a single project ───────────────────────────────────────────────────

/** GET /api/evaluator/projects/{id}/review */
export interface ProjectReviewResponse {
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

/** Element of GET /api/evaluator/projects/{id}/similarity */
export interface SimilarTitleDto {
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

/** PUT /api/evaluator/projects/{id}/evaluate */
export interface SubmitEvaluationRequest {
  result: number;
  feedback?: string;
}

// ── Evaluator dashboard ───────────────────────────────────────────────────────
export interface EvaluatorStatsDto {
  totalAssigned: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
  needsModificationCount: number;
  reviewedCount: number;
  avgReviewDays: number | null;
}

export interface PendingEvaluationDto {
  assignmentId: string;
  projectId: string;
  projectCode: string;
  projectNameVi: string;
  majorName: string;
  studentName: string;
  studentAvatar: string | null;
  assignedAt: string;
  daysElapsed: number;
  isUrgent: boolean;
}

export interface RecentReviewedDto {
  projectId: string;
  projectNameVi: string;
  result: string;
  evaluatedAt: string;
}

/** GET /api/evaluator/dashboard */
export interface EvaluatorDashboardResponse {
  stats: EvaluatorStatsDto;
  pendingEvaluations: PendingEvaluationDto[];
  recentReviewed: RecentReviewedDto[];
}

// ── Filter options ────────────────────────────────────────────────────────────
export interface FilterOptionDto {
  value: number;
  label: string;
}

/** GET /api/evaluator/filter-options */
export interface EvaluatorFilterOptionsResponse {
  semesters: FilterOptionDto[];
  majors: FilterOptionDto[];
}

// ── Assigned projects list ────────────────────────────────────────────────────
export interface EvaluatorProjectItemDto {
  assignmentId: string;
  projectId: string;
  projectCode: string;
  projectNameVi: string;
  majorName: string;
  studentName: string;
  studentAvatar: string | null;
  mentorName: string;
  submittedAt: string | null;
  assignedAt: string;
  individualResult: string;
  isUrgent: boolean;
}

/** GET /api/evaluator/projects */
export interface EvaluatorProjectsResponse {
  items: EvaluatorProjectItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface EvaluatorProjectsFilters {
  page?: number;
  pageSize?: number;
  search?: string;
  semesterId?: number;
  majorId?: number;
  result?: string;
}

// ── Evaluation history ────────────────────────────────────────────────────────
export interface EvaluatorHistoryStatsDto {
  totalReviewed: number;
  approvedCount: number;
  needsModificationCount: number;
  rejectedCount: number;
}

export interface EvaluatorHistoryItemDto {
  projectId: string;
  projectCode: string;
  projectNameVi: string;
  studentName: string;
  studentAvatar: string | null;
  evaluatedAt: string;
  result: string;
  feedback: string | null;
}

/** GET /api/evaluator/history */
export interface EvaluatorHistoryResponse {
  stats: EvaluatorHistoryStatsDto;
  items: EvaluatorHistoryItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface EvaluatorHistoryFilters {
  page?: number;
  pageSize?: number;
  search?: string;
  result?: string;
  dateRange?: string;
}
