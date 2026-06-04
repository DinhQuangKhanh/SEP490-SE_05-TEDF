// Topic pools + topics browsed/registered from a pool.

export interface TopicInPoolItem {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  description: string | null;
  technologies: string | null;
  majorId: number;
  majorName: string;
  majorCode: string;
  poolStatus: number;
  poolStatusName: string;
  maxStudents: number;
  mentorName: string;
  mentorId: string;
  createdAt: string;
}

/** Mentor projection embedded in a topic's detail. */
export interface TopicMentorDto {
  mentorId: string;
  fullName: string;
}

/** Full detail of a thesis topic — works for all source types (pool or direct registration). */
export interface TopicDetail {
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

export interface TopicDetailRaw extends Omit<TopicDetail, "technologies"> {
  technologies?: string | null;
}

/** GET /api/topics — paginated topics in pool. */
export interface TopicsInPoolResponse {
  items: TopicInPoolItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface TopicFilters {
  majorId?: number;
  search?: string;
  poolStatus?: number;
  sortBy?: string;
  page?: number;
  pageSize?: number;
}

export interface TopicDocument {
  id: string;
  fileName: string;
  originalFileName: string;
  fileType: string;
  fileSize: number;
  documentType: string;
  description: string | null;
  uploadedAt: string;
  uploadedByName: string;
}

// ── Topic pool catalog (mentor screens) ───────────────────────────────────────

/** GET /api/topic-pools/{id} */
export interface TopicPoolDto {
  id: string;
  code: string;
  name: string;
  description: string | null;
  majorId: number;
  statusName: string;
  maxActiveTopicsPerMentor: number;
  expirationSemesters: number;
}

/** GET /api/topic-pools/{id}/statistics */
export interface TopicPoolStatisticsDto {
  poolId: string;
  poolCode: string;
  poolName: string;
  totalMentors: number;
  totalTopicsCount: number;
  activeTopicsCount: number;
  registeredTopicsCount: number;
  expiredTopicsCount: number;
}

/** Pool summary embedded in the by-department listing. */
export interface TopicPoolSummaryDto {
  id: string;
  code: string;
  name: string;
  statusName: string;
  totalTopics: number;
}

export interface MajorWithPoolDto {
  majorId: number;
  majorCode: string;
  majorName: string;
  pool: TopicPoolSummaryDto | null;
}

/** GET /api/topic-pools/by-department */
export interface DepartmentWithPoolsDto {
  departmentId: number;
  departmentCode: string;
  departmentName: string;
  majors: MajorWithPoolDto[];
}
