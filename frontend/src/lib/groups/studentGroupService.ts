import {
  AvailableStudentDto,
  GroupRegistrationDto,
  BulkOperationResultDto,
  InvitationDto,
  JoinRequestDto,
  MentorGroupDto,
  OpenGroupDto,
  PendingJoinRequestDto,
  StudentGroupDto,
} from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const studentGroupService = {
  getMentorGroups: (semesterId?: number) =>
    apiClient.get<MentorGroupDto[]>(`${routes.mentor.studentGroups}${semesterId ? `?semesterId=${semesterId}` : ""}`),

  getMyGroup: (semesterId?: number) =>
    apiClient.get<StudentGroupDto | null>(
      `${routes.studentGroups.myGroup}${semesterId ? `?semesterId=${semesterId}` : ""}`,
    ),

  getOpenGroups: (semesterId?: number) =>
    apiClient.get<OpenGroupDto[]>(`${routes.studentGroups.open}${semesterId ? `?semesterId=${semesterId}` : ""}`),

  getMyInvitations: () => apiClient.get<InvitationDto[]>(routes.studentGroups.myInvitations),

  getJoinRequests: (groupId: string) => apiClient.get<JoinRequestDto[]>(routes.studentGroups.joinRequests(groupId)),

  getInvitableStudents: (groupId: string) =>
    apiClient.get<AvailableStudentDto[]>(routes.studentGroups.invitableStudents(groupId)),

  getMyPendingJoinRequest: (semesterId?: number) =>
    apiClient.get<PendingJoinRequestDto | null>(
      `${routes.studentGroups.myPendingJoinRequest}${semesterId ? `?semesterId=${semesterId}` : ""}`,
    ),

  // Groups carry no client-supplied name: the server assigns the id (SE_NN), and the display name
  // becomes "SE_NN - English topic name - Mentor" once the topic passes evaluation.
  createGroup: () => apiClient.post<{ id: string }>(routes.studentGroups.base),

  inviteMember: (groupId: string, studentCode: string, message?: string) =>
    apiClient.post<{ id: number }>(routes.studentGroups.invitations(groupId), {
      studentCode,
      message,
    }),

  acceptInvitation: (groupId: string, invitationId: number) =>
    apiClient.put<void>(`${routes.studentGroups.invitations(groupId)}/${invitationId}/accept`),

  rejectInvitation: (groupId: string, invitationId: number) =>
    apiClient.put<void>(`${routes.studentGroups.invitations(groupId)}/${invitationId}/reject`),

  requestJoin: (groupId: string, message?: string) =>
    apiClient.post<{ id: number }>(routes.studentGroups.joinRequests(groupId), { message }),

  approveJoinRequest: (groupId: string, requestId: number) =>
    apiClient.put<void>(`${routes.studentGroups.joinRequests(groupId)}/${requestId}/approve`),

  rejectJoinRequest: (groupId: string, requestId: number) =>
    apiClient.put<void>(`${routes.studentGroups.joinRequests(groupId)}/${requestId}/reject`),

  bulkApproveJoinRequests: (groupId: string, requestIds: number[]) =>
    apiClient.put<BulkOperationResultDto>(routes.studentGroups.bulkApproveJoinRequests(groupId), { requestIds }),

  bulkRejectJoinRequests: (groupId: string, requestIds: number[]) =>
    apiClient.put<BulkOperationResultDto>(routes.studentGroups.bulkRejectJoinRequests(groupId), { requestIds }),

  /** Group leader registers the group for a topic from the pool. */
  registerTopic: (params: { projectId: string; groupId: string; note?: string }) =>
    apiClient.post(routes.studentTopics.registerTopic(params.groupId), {
      projectId: params.projectId,
      note: params.note,
    }),

  /** Lists the group's topic-pool registrations (newest first) so it can track pending/rejected ones. */
  getMyRegistrations: (groupId: string) =>
    apiClient.get<GroupRegistrationDto[]>(routes.studentTopics.myRegistrations(groupId)),

  /** Group leader cancels their own pending registration. */
  cancelRegistration: (registrationId: string) =>
    apiClient.put<void>(routes.studentTopics.cancelRegistration(registrationId)),
};
