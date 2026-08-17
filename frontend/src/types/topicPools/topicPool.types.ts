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

/** POST /api/topic-pools/note-attachment — synchronous upload for the registration-note editor. */
export interface NoteAttachmentUploadResponse {
  url: string;
  originalFileName: string;
  fileSize: number;
  contentType: string;
}

/** An attachment already uploaded to storage, kept in the registration-note editor's list. */
export interface NoteAttachment {
  url: string;
  name: string;
  size: number;
}

/**
 * POST /api/topic-pools/{poolId}/propose/validate — the 3.1–3.4 fields parsed off an uploaded register
 * form when it passes validation, shown as a preview in the propose modal before the mentor submits.
 */
export interface RegisterFormPreview {
  nameEn: string;
  nameVi: string;
  nameAbbr: string;
  description: string;
  objectives: string;
  technologies: string | null;
  expectedResults: string | null;
  scope: string | null;
  mentorCount: number;
}
