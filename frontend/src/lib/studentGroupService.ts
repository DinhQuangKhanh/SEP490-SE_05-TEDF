import { apiClient } from "./apiClient";

// ── Types ───────────────────────────────────────────────────────

export interface GroupMemberDto {
  studentId: string;
  fullName: string;
  studentCode?: string;
  email?: string;
  role: string;
  status: string;
  joinedAt: string;
}

export interface MentorGroupDto {
  groupId: string;
  groupCode: string;
  groupName?: string;
  groupStatus: string;
  maxMembers: number;
  projectId?: string;
  projectName?: string;
  projectCode?: string;
  projectStatus?: string;
  createdAt: string;
  members: GroupMemberDto[];
}

export interface StudentGroupDto {
  groupId: string;
  groupCode: string;
  groupName?: string;
  groupStatus: string;
  maxMembers: number;
  isOpenForRequests: boolean;
  projectId?: string;
  projectName?: string;
  projectCode?: string;
  projectStatus?: string;
  mentorName?: string;
  createdAt: string;
  members: GroupMemberDto[];
}

export interface OpenGroupDto {
  groupId: string;
  groupCode: string;
  groupName?: string;
  memberCount: number;
  maxMembers: number;
  createdAt: string;
  members: GroupMemberDto[];
}

export interface InvitationDto {
  id: number;
  groupId: string;
  groupCode: string;
  groupName?: string;
  inviterId: string;
  inviterName: string;
  message?: string;
  status: string;
  createdAt: string;
  expiresAt: string;
}

export interface JoinRequestDto {
  id: number;
  studentId: string;
  studentName: string;
  studentCode?: string;
  message?: string;
  status: string;
  createdAt: string;
}

export interface PendingJoinRequestDto {
  requestId: number;
  groupId: string;
  groupCode: string;
  groupName?: string;
  message?: string;
  createdAt: string;
  expiresAt: string;
}

// ── Service ─────────────────────────────────────────────────────

export const studentGroupService = {
  // Queries
  getMentorGroups: (semesterId?: number) =>
    apiClient.get<MentorGroupDto[]>(`/api/student-groups/mentor${semesterId ? `?semesterId=${semesterId}` : ""}`),

  getMyGroup: (semesterId?: number) =>
    apiClient.get<StudentGroupDto | null>(
      `/api/student-groups/my-group${semesterId ? `?semesterId=${semesterId}` : ""}`,
    ),

  getOpenGroups: (semesterId?: number) =>
    apiClient.get<OpenGroupDto[]>(`/api/student-groups/open${semesterId ? `?semesterId=${semesterId}` : ""}`),

  getMyInvitations: () => apiClient.get<InvitationDto[]>("/api/student-groups/my-invitations"),

  getJoinRequests: (groupId: string) => apiClient.get<JoinRequestDto[]>(`/api/student-groups/${groupId}/join-requests`),

  getMyPendingJoinRequest: (semesterId?: number) =>
    apiClient.get<PendingJoinRequestDto | null>(
      `/api/student-groups/my-pending-join-request${semesterId ? `?semesterId=${semesterId}` : ""}`,
    ),

  // Commands
  createGroup: (name?: string) => apiClient.post<{ id: string }>("/api/student-groups", { name }),

  inviteMember: (groupId: string, studentCode: string, message?: string) =>
    apiClient.post<{ id: number }>(`/api/student-groups/${groupId}/invitations`, { studentCode, message }),

  acceptInvitation: (groupId: string, invitationId: number) =>
    apiClient.put<void>(`/api/student-groups/${groupId}/invitations/${invitationId}/accept`),

  rejectInvitation: (groupId: string, invitationId: number) =>
    apiClient.put<void>(`/api/student-groups/${groupId}/invitations/${invitationId}/reject`),

  requestJoin: (groupId: string, message?: string) =>
    apiClient.post<{ id: number }>(`/api/student-groups/${groupId}/join-requests`, { message }),

  approveJoinRequest: (groupId: string, requestId: number) =>
    apiClient.put<void>(`/api/student-groups/${groupId}/join-requests/${requestId}/approve`),

  rejectJoinRequest: (groupId: string, requestId: number) =>
    apiClient.put<void>(`/api/student-groups/${groupId}/join-requests/${requestId}/reject`),
};
