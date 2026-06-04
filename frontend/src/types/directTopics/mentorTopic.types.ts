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

/** GET /api/mentor/topics */
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

/** PUT /api/mentor/topics/{id}/update */
export interface UpdatePoolTopicRequest {
  nameVi: string;
  nameEn: string;
  nameAbbr: string;
  description: string;
  objectives: string;
  scope?: string | null;
  technologies?: string | null;
  expectedResults?: string | null;
  maxStudents: number;
}
