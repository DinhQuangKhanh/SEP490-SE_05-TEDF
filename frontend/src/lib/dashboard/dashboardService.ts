import {
  AdminDashboardData,
  DepartmentHeadDashboardData,
  EvaluatorDashboardResponse,
  MentorDashboardData,
} from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

/** Per-role dashboard fetchers (mirrors backend Dashboard feature). */
export const dashboardService = {
  getAdminDashboard: (): Promise<AdminDashboardData> => apiClient.get<AdminDashboardData>(routes.dashboard.admin),

  getMentorDashboard: (): Promise<MentorDashboardData> => apiClient.get<MentorDashboardData>(routes.dashboard.mentor),

  getDepartmentHeadDashboard: (): Promise<DepartmentHeadDashboardData> =>
    apiClient.get<DepartmentHeadDashboardData>(routes.dashboard.departmentHead),

  getEvaluatorDashboard: (): Promise<EvaluatorDashboardResponse> =>
    apiClient.get<EvaluatorDashboardResponse>(routes.dashboard.evaluator),
};
