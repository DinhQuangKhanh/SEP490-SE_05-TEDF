import type {
  ChecklistConfigDto,
  ChecklistConfigListResponse,
  ChecklistCriterionSeedDto,
  CopyChecklistConfigRequest,
  CreateChecklistConfigRequest,
  ProjectChecklistResponse,
  SaveProjectChecklistRequest,
  UpdateChecklistConfigRequest,
} from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const checklistService = {
  // ── Evaluator: per-project checklist ──────────────────────────────────────
  getProjectChecklist: (projectId: string): Promise<ProjectChecklistResponse> =>
    apiClient.get<ProjectChecklistResponse>(routes.evaluator.checklist(projectId)),

  saveProjectChecklist: (projectId: string, data: SaveProjectChecklistRequest): Promise<void> =>
    apiClient.put<void>(routes.evaluator.checklist(projectId), data),

  // ── Department Head: checklist configuration management ────────────────────
  getConfigs: (semesterId?: number): Promise<ChecklistConfigListResponse> => {
    const query = semesterId != null ? `?semesterId=${semesterId}` : "";
    return apiClient.get<ChecklistConfigListResponse>(`${routes.checklistConfigs.base}${query}`);
  },

  getConfig: (id: string): Promise<ChecklistConfigDto> =>
    apiClient.get<ChecklistConfigDto>(routes.checklistConfigs.byId(id)),

  getDefaultCriteria: (): Promise<ChecklistCriterionSeedDto[]> =>
    apiClient.get<ChecklistCriterionSeedDto[]>(routes.checklistConfigs.defaultCriteria),

  createConfig: (data: CreateChecklistConfigRequest): Promise<string> =>
    apiClient.post<string>(routes.checklistConfigs.base, data),

  copyConfig: (sourceId: string, data: CopyChecklistConfigRequest): Promise<string> =>
    apiClient.post<string>(routes.checklistConfigs.copy(sourceId), data),

  updateConfig: (id: string, data: UpdateChecklistConfigRequest): Promise<void> =>
    apiClient.put<void>(routes.checklistConfigs.byId(id), data),

  activateConfig: (id: string): Promise<void> =>
    apiClient.post<void>(routes.checklistConfigs.activate(id), {}),

  deactivateConfig: (id: string): Promise<void> =>
    apiClient.post<void>(routes.checklistConfigs.deactivate(id), {}),
};
