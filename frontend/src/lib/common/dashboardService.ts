import { AdminDashboardData, MentorDashboardData } from "@/types";
import { apiClient } from "./apiClient";
import { routes } from "./routes";

export const dashboardService = {
  getAdminDashboard: (): Promise<AdminDashboardData> => apiClient.get<AdminDashboardData>(routes.admin.dashboard),

  getMentorDashboard: (): Promise<MentorDashboardData> => apiClient.get<MentorDashboardData>(routes.mentor.dashboard),
};
