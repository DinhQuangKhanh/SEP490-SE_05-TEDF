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
  /**
   * Tên hiển thị: chỉ mã nhóm (SE_NN) cho tới khi đề tài thẩm định thành công,
   * sau đó là "SE_NN - Tên tiếng Anh của đề tài - Giảng viên hướng dẫn".
   */
  displayName: string;
  groupStatus: string;
  maxMembers: number;
  projectId?: string;
  projectName?: string;
  projectNameEn?: string;
  projectCode?: string;
  projectStatus?: string;
  semesterId: number;
  semesterName: string;
  semesterStartDate: string;
  createdAt: string;
  members: GroupMemberDto[];
}

export interface StudentGroupDto {
  groupId: string;
  groupCode: string;
  /** Luôn có dạng SE_NN — phần đuôi của groupCode. */
  groupName?: string;
  /**
   * Tên hiển thị: chỉ mã nhóm (SE_NN) cho tới khi đề tài thẩm định thành công,
   * sau đó là "SE_NN - Tên tiếng Anh của đề tài - Giảng viên hướng dẫn".
   */
  displayName: string;
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

export interface AvailableStudentDto {
  studentId: string;
  studentCode: string;
  fullName: string;
}

export interface BulkOperationResultDto {
  totalRequested: number;
  successCount: number;
  failures: BulkItemFailureDto[];
  message: string;
}

export interface BulkItemFailureDto {
  id: number;
  error: string;
}
