import { TopicMentorDto } from "../topics/topic.types";

/** Mentor projection used by department-head project lists (mentorName). */
export interface MentorSummary {
  mentorId: string;
  mentorName: string;
}

export interface ProjectListItem {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string | null;
  status: string;
  majorName: string;
  majorCode: string;
  semesterName: string;
  sourceType: string;
  mentorNames: string[];
  studentNames: string[];
  groupCode: string | null;
  createdAt: string;
}

export interface ProjectListResponse {
  items: ProjectListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ProjectFilters {
  search?: string;
  semesterId?: number;
  status?: string;
  majorId?: number;
  page?: number;
  pageSize?: number;
}

export interface ProjectDetail {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  nameAbbr: string;
  description: string;
  objectives: string;
  scope: string | null;
  technologies: string | null;
  expectedResults: string | null;
  majorId: number;
  majorName: string;
  majorCode: string;
  poolStatus: number;
  poolStatusName: string;
  maxStudents: number;
  mentors: TopicMentorDto[];
  createdAt: string;
  updatedAt: string | null;
}

export type ProjectDetailRaw = ProjectDetail & {
  techologies?: string | null;
};

/** A project the current lecturer supervises (profile "Lịch Sử Hướng Dẫn Đồ Án"). */
export interface SupervisedProject {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string | null;
  status: string;
  statusValue: number;
  semesterName: string;
  groupCode: string | null;
  description: string | null;
  objectives: string | null;
  startDate: string | null;
  deadline: string | null;
  createdAt: string;
}

export interface SupervisedProjectsResponse {
  items: SupervisedProject[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SupervisedProjectFilters {
  search?: string;
  /** "name" | "oldest" | "status" | (default) newest */
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface ProjectAuditLogDto {
  id: string;
  action: string;
  performedBy: string | null;
  performedByName: string | null;
  /** Trạng thái trước khi hành động này thay đổi trạng thái đề tài. */
  oldStatus: string | null;
  /** Trạng thái sau khi hành động này thay đổi trạng thái đề tài. */
  newStatus: string | null;
  submissionNumber: number | null;
  timestamp: string;
  metadata: Record<string, unknown> | null;
}

export interface DepartmentAuditLogFilters {
  search?: string;
  /** Comma-separated action names; empty means all. */
  actions?: string;
  page?: number;
  pageSize?: number;
}

export interface DepartmentAuditLogItemDto {
  id: string;
  projectId: string;
  projectCode: string;
  projectName: string;
  action: string;
  performedBy: string | null;
  performedByName: string | null;
  oldStatus: string | null;
  newStatus: string | null;
  submissionNumber: number | null;
  timestamp: string;
  metadata: Record<string, unknown> | null;
}

export interface DepartmentAuditLogStatsDto {
  total: number;
  submitted: number;
  approved: number;
  revision: number;
  rejected: number;
}

export interface DepartmentAuditLogsResponse {
  items: DepartmentAuditLogItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  stats: DepartmentAuditLogStatsDto;
}

export interface ProjectAuditLogResponse {
  logs: ProjectAuditLogDto[];
  /** Số lần đề tài bị trả về để chỉnh sửa. */
  revisionCount: number;
  /** Số lần nhóm đã nộp (lần nộp cao nhất). */
  submissionCount: number;
}
