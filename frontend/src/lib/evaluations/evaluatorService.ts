import {
  DepartmentEvaluator,
  EvaluatorFilterOptionsResponse,
  EvaluatorHistoryFilters,
  EvaluatorHistoryResponse,
  EvaluatorProjectsFilters,
  EvaluatorProjectsResponse,
  ProjectReviewResponse,
  SimilarityMatchDto,
} from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const evaluatorService = {
  // ── Evaluator self-service ──────────────────────────────────────────────────
  getFilterOptions: (): Promise<EvaluatorFilterOptionsResponse> =>
    apiClient.get<EvaluatorFilterOptionsResponse>(routes.evaluator.filterOptions),

  getProjects: (filters: EvaluatorProjectsFilters = {}): Promise<EvaluatorProjectsResponse> => {
    const params = new URLSearchParams();
    params.set("page", String(filters.page ?? 1));
    params.set("pageSize", String(filters.pageSize ?? 10));
    if (filters.search) params.set("search", filters.search);
    if (filters.semesterId != null) params.set("semesterId", String(filters.semesterId));
    if (filters.majorId != null) params.set("majorId", String(filters.majorId));
    if (filters.result) params.set("result", filters.result);
    return apiClient.get<EvaluatorProjectsResponse>(`${routes.evaluator.projects}?${params.toString()}`);
  },

  getHistory: (filters: EvaluatorHistoryFilters = {}): Promise<EvaluatorHistoryResponse> => {
    const params = new URLSearchParams();
    params.set("page", String(filters.page ?? 1));
    params.set("pageSize", String(filters.pageSize ?? 10));
    if (filters.search) params.set("search", filters.search);
    if (filters.result) params.set("result", filters.result);
    if (filters.dateRange) params.set("dateRange", filters.dateRange);
    return apiClient.get<EvaluatorHistoryResponse>(`${routes.evaluator.history}?${params.toString()}`);
  },

  getProjectForReview: (projectId: string): Promise<ProjectReviewResponse> =>
    apiClient.get<ProjectReviewResponse>(routes.evaluator.review(projectId)),

  checkSimilarity: (projectId: string): Promise<SimilarityMatchDto[]> =>
    apiClient.get<SimilarityMatchDto[]>(routes.evaluator.similarity(projectId)),

  submitEvaluation: (projectId: string, data: { result: number; feedback?: string }): Promise<string> =>
    apiClient.post<string>(routes.evaluator.evaluate(projectId), data),

  // ── Department-head evaluation management ────────────────────────────────────
  getDepartmentEvaluators: (): Promise<DepartmentEvaluator[]> =>
    apiClient.get<DepartmentEvaluator[]>(routes.departmentHead.evaluators),

  assignEvaluator: (projectId: string, phaseId: number, evaluatorId: string, order: number): Promise<unknown> =>
    apiClient.post(routes.departmentHead.assignEvaluator, { projectId, phaseId, evaluatorId, evaluatorOrder: order }),

  submitFinalDecision: (projectId: string, result: number, notes?: string): Promise<unknown> =>
    apiClient.post(routes.departmentHead.finalDecision(projectId), { result, notes }),
};
