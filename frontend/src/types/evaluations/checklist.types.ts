// Types for the topic-evaluation checklist (evaluator results + department-head configuration).
// Mirrors the backend `EvaluationChecklists` feature DTOs.

// ── Evaluator: per-project checklist ────────────────────────────────────────
export interface ProjectChecklistItemDto {
  criterionId: string;
  order: number;
  titleVi: string;
  titleEn: string;
  description: string | null;
  /** The evaluator's per-criterion comment. */
  comment: string | null;
  /** The evaluator's pass/fail decision. */
  isPassed: boolean;
}

export interface ProjectChecklistResponse {
  projectId: string;
  /** False when the project's semester has no Active checklist configured. */
  hasActiveConfig: boolean;
  configId: string | null;
  /** The checklist config version used (null when no active config). */
  version: number | null;
  totalCriteria: number;
  requiredPassCount: number;
  passedCount: number;
  /** True when passedCount >= requiredPassCount (server-computed). */
  canApprove: boolean;
  /** True once the evaluator has saved a result for the current round. */
  isSaved: boolean;
  evaluatorNote: string | null;
  updatedAt: string | null;
  items: ProjectChecklistItemDto[];
}

export interface ChecklistEvaluationItemInput {
  criterionId: string;
  isPassed: boolean;
  comment?: string | null;
}

export interface SaveProjectChecklistRequest {
  items: ChecklistEvaluationItemInput[];
  note?: string | null;
}

// ── Department Head: checklist configuration ────────────────────────────────
export type ChecklistConfigStatus = "Draft" | "Active" | "Inactive";

export interface ChecklistCriterionDto {
  id: string;
  order: number;
  titleVi: string;
  titleEn: string;
  description: string | null;
}

export interface ChecklistConfigDto {
  id: string;
  semesterId: number;
  semesterName: string;
  version: number;
  status: ChecklistConfigStatus;
  requiredPassCount: number;
  criteriaCount: number;
  sourceFileName: string | null;
  isUsed: boolean;
  createdAt: string;
  createdBy: string | null;
  createdByName: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
  updatedByName: string | null;
  criteria: ChecklistCriterionDto[];
}

export interface ChecklistSemesterOptionDto {
  id: number;
  name: string;
  code: string;
  status: string;
}

export interface ChecklistConfigListResponse {
  semesters: ChecklistSemesterOptionDto[];
  configs: ChecklistConfigDto[];
}

export interface ChecklistCriterionSeedDto {
  order: number;
  titleVi: string;
  titleEn: string;
  description: string;
}

/** Editable criterion payload sent to create/update endpoints. */
export interface ChecklistCriterionInput {
  titleVi: string;
  titleEn: string;
  description?: string | null;
}

export interface CreateChecklistConfigRequest {
  semesterId: number;
  criteria: ChecklistCriterionInput[];
  requiredPassCount: number;
}

export interface UpdateChecklistConfigRequest {
  criteria: ChecklistCriterionInput[];
  requiredPassCount: number;
}

export interface CopyChecklistConfigRequest {
  targetSemesterId: number;
}

// ── Department Head: Excel import preview ───────────────────────────────────
export interface ChecklistImportPreviewRowDto {
  rowNumber: number;
  order: number;
  titleVi: string;
  titleEn: string;
  description: string | null;
  errors: string[];
}

export interface ChecklistImportPreviewResponse {
  /** True when the file has criteria and no row/file-level errors. */
  isValid: boolean;
  criteriaCount: number;
  rows: ChecklistImportPreviewRowDto[];
  errors: string[];
}
