// Topic pool catalog + mentor pool-topic editing.

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

/** PUT /api/topic-pools/topics/{id}/update — mentor edits a pool topic. */
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

/** GET /api/topic-pools/registrations/mentor — a pending registration request for the mentor. */
export interface MentorRegistrationRequestDto {
  registrationId: string;
  projectId: string;
  projectName: string | null;
  projectCode: string | null;
  groupId: string;
  groupName: string | null;
  groupCode: string | null;
  registeredByName: string | null;
  memberCount: number;
  note: string | null;
  registeredAt: string;
}

/** SignalR `ReceiveRegistrationUpdate` payload (real-time mentor registration tab). */
export interface RegistrationUpdate {
  action: "added" | "removed";
  registrationId: string;
  projectId: string;
}
