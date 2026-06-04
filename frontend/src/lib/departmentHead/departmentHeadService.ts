import { DepartmentEvaluator, DepartmentHeadDashboardData, DepartmentProjectsResponse } from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

// ── API ──────────────────────────────────────────────────────────────────────
export const departmentHeadService = {
  getDashboard: () => apiClient.get<DepartmentHeadDashboardData>(routes.departmentHead.dashboard),

  getProjects: () => apiClient.get<DepartmentProjectsResponse>(routes.departmentHead.projects),

  getEvaluators: () => apiClient.get<DepartmentEvaluator[]>(routes.departmentHead.evaluators),

  assignEvaluator: (projectId: string, evaluatorId: string, order: number) =>
    apiClient.post(routes.departmentHead.assignEvaluator, {
      projectId,
      evaluatorId,
      evaluatorOrder: order,
    }),

  submitFinalDecision: (projectId: string, result: number, notes?: string) =>
    apiClient.post(routes.departmentHead.finalDecision(projectId), {
      result,
      notes,
    }),
};
