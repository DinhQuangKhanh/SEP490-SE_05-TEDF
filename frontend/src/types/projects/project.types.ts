import { TopicMentorDto } from "../topicPools/topicPool.types";

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
