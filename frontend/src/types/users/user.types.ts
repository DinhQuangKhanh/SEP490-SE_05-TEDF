export interface UserListItem {
  id: string;
  fullName: string;
  email: string;
  avatarUrl: string | null;
  studentCode: string | null;
  employeeCode: string | null;
  academicTitle: string | null;
  departmentId: number | null;
  departmentName: string | null;
  status: string;
  roles: string[];
  createdAt: string;
}

export interface UserListResponse {
  items: UserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserFilters {
  role?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}
