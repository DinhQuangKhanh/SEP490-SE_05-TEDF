export interface UserBriefDto {
  id: string;
  fullName: string;
  email: string;
  role: string;
}

export interface TicketMessageDto {
  id: string;
  senderId: string;
  sender?: UserBriefDto;
  content: string;
  createdAt: string;
}

/** GET /api/supports/{id} */
export interface TicketResponse {
  id: string;
  code: string;
  title: string;
  description: string;
  reporter: UserBriefDto;
  assignee?: UserBriefDto;
  category: string;
  priority: string;
  status: string;
  createdAt: string;
  updatedAt?: string;
  resolvedAt?: string;
  closedAt?: string;
  messages: TicketMessageDto[];
}

/** Element of GET /api/supports */
export interface TicketListDto {
  id: string;
  code: string;
  title: string;
  reporter: UserBriefDto;
  category: string;
  priority: string;
  status: string;
  createdAt: string;
}

/** GET /api/supports/stats */
export interface TicketStatsResponse {
  totalTickets: number;
  unread: number;
  inProgress: number;
  resolved: number;
}

// ── Request bodies ────────────────────────────────────────────────────────────

/** POST /api/supports */
export interface CreateTicketRequest {
  title: string;
  description: string;
  category: number;
  priority: number;
}

/** POST /api/supports/{id}/reply */
export interface TicketReplyRequest {
  content: string;
}

/** PATCH /api/supports/{id}/status */
export interface UpdateTicketStatusRequest {
  status: number;
}
