import {
  DepartmentWithPoolsDto,
  GroupRegistrationDto,
  MentorRegistrationRequestDto,
  NoteAttachmentUploadResponse,
  RegisterFormPreview,
  TopicPoolDto,
  TopicPoolStatisticsDto,
  UpdatePoolTopicRequest,
} from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const topicPoolService = {
  /** All topic pools (optionally filtered by major). */
  getTopicPools: (majorId?: number): Promise<TopicPoolDto[]> =>
    apiClient.get<TopicPoolDto[]>(`${routes.topicPools.list}${majorId != null ? `?majorId=${majorId}` : ""}`),

  getTopicPoolsByDepartment: (): Promise<DepartmentWithPoolsDto[]> =>
    apiClient.get<DepartmentWithPoolsDto[]>(routes.topicPools.byDepartment),

  getTopicPoolById: (id: string): Promise<TopicPoolDto> => apiClient.get<TopicPoolDto>(routes.topicPools.byId(id)),

  getTopicPoolStatistics: (id: string): Promise<TopicPoolStatisticsDto> =>
    apiClient.get<TopicPoolStatisticsDto>(routes.topicPools.statistics(id)),

  /**
   * Step A: scan + parse + validate an uploaded register form WITHOUT creating the topic. Resolves
   * with the parsed 3.1–3.4 preview when the form is clean & complete; rejects with the specific
   * error (Kinds-of-person / mentor mismatch / missing section) otherwise.
   */
  validateRegisterForm: (poolId: string, file: File): Promise<RegisterFormPreview> => {
    const formData = new FormData();
    formData.append("registerForm", file);
    return apiClient.postForm<RegisterFormPreview>(routes.topicPools.proposeValidate(poolId), formData);
  },

  /** Step B: mentor proposes the topic (multipart: the register form + the optional note). */
  proposeTopic: (poolId: string, formData: FormData): Promise<{ id: string }> =>
    apiClient.postForm<{ id: string }>(routes.topicPools.propose(poolId), formData),

  /** Mentor edits a pool topic after NeedsModification. */
  updatePoolTopic: (projectId: string, data: UpdatePoolTopicRequest): Promise<void> =>
    apiClient.put<void>(routes.mentor.topicUpdate(projectId), data),

  /** Mentor resubmits a pool topic for evaluation. */
  resubmitPoolTopic: (projectId: string): Promise<void> =>
    apiClient.put<void>(routes.mentor.topicResubmit(projectId)),

  /** Pending registration requests for the current mentor's pool topics. */
  getMentorRegistrations: (): Promise<MentorRegistrationRequestDto[]> =>
    apiClient.get<MentorRegistrationRequestDto[]>(routes.topicPools.mentorRegistrations),

  /**
   * Confirmed registration (reason + attachments) for a project the current mentor supervises.
   * Returns null when the project has no confirmed registration (e.g. a direct-registration topic).
   */
  getProjectRegistration: (projectId: string): Promise<GroupRegistrationDto | null> =>
    apiClient.get<GroupRegistrationDto | null>(routes.topicPools.projectRegistration(projectId)),

  /** Mentor confirms a registration (assigns the group to the topic). */
  confirmRegistration: (registrationId: string): Promise<void> =>
    apiClient.put<void>(routes.topicPools.confirmRegistration(registrationId)),

  /** Mentor rejects a registration with a reason. */
  rejectRegistration: (registrationId: string, reason: string): Promise<void> =>
    apiClient.put<void>(routes.topicPools.rejectRegistration(registrationId), { reason }),

  /** Uploads a single image/file for the registration-note editor; returns its public URL. */
  uploadNoteAttachment: (file: File): Promise<NoteAttachmentUploadResponse> => {
    const formData = new FormData();
    formData.append("file", file);
    return apiClient.postForm<NoteAttachmentUploadResponse>(routes.topicPools.noteAttachment, formData);
  },
};
