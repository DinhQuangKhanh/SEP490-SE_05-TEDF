// Topic catalog (browse/detail/documents) + mentor's owned topics.

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

// ── Mentor's owned topics (GET /api/topics/mentor) ──────────────────────────────
export interface MentorTopicItem {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  majorName: string;
  sourceType: number; // 0=FromPool, 1=DirectRegistration
  sourceTypeName: string;
  status: number; // ProjectStatus enum
  statusName: string;
  submittedAt: string | null;
  createdAt: string;
  semesterName: string;
}

export interface MentorTopicsResponse {
  items: MentorTopicItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface MentorTopicFilters {
  semesterId?: number;
  search?: string;
  page?: number;
  pageSize?: number;
}

// ── Topic-pool registration made by a student group ─────────────────────────────
// GET /api/topic-pools/groups/{groupId}/registrations
export interface GroupRegistrationDto {
  id: string;
  projectId: string;
  projectName: string | null;
  projectCode: string | null;
  mentorName: string | null;
  /** Pending | Confirmed | Rejected | Cancelled */
  status: string;
  registeredAt: string;
  note: string | null;
  rejectReason: string | null;
}
