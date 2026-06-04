import { MajorOption } from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const majorService = {
  /** Majors used by topic/semester/project filters and select dropdowns. */
  getMajors: (): Promise<MajorOption[]> => apiClient.get<MajorOption[]>(routes.common.majors),
};
