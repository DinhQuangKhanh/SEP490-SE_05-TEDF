/** Whether the signed-in account may use the system. */
export interface SessionAccess {
  allowed: boolean;
  /** null when allowed, otherwise: "locked" | "inactive" | "student_not_eligible". */
  kind: string | null;
  reason: string | null;
}

/** Session bootstrap info returned by GET /api/auth/session. */
export interface SessionInfo {
  userId: string;
  fullName: string;
  email: string;
  roles: string[];
  status: string;
  access: SessionAccess;
}
