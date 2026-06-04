import {
  EvaluatorDashboardResponse,
  EvaluatorFilterOptionsResponse,
  EvaluatorHistoryFilters,
  EvaluatorHistoryResponse,
  EvaluatorProjectsFilters,
  EvaluatorProjectsResponse,
  ProjectReviewResponse,
  SimilarTitleDto,
} from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const evaluatorService = {
  // Queries
  getDashboard: (): Promise<EvaluatorDashboardResponse> =>
    apiClient.get<EvaluatorDashboardResponse>(routes.evaluator.dashboard),

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

  checkSimilarity: (projectId: string): Promise<SimilarTitleDto[]> =>
    apiClient.get<SimilarTitleDto[]>(routes.evaluator.similarity(projectId)),

  // Commands
  submitEvaluation: (projectId: string, data: { result: number; feedback?: string }): Promise<string> =>
    apiClient.post<string>(routes.evaluator.evaluate(projectId), data),
};
