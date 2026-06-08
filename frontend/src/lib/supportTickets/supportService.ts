import {
  CreateTicketRequest,
  TicketListDto,
  TicketReplyRequest,
  TicketResponse,
  TicketStatsResponse,
  UpdateTicketStatusRequest,
} from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const supportService = {
  // Queries
  getTickets: (): Promise<TicketListDto[]> => apiClient.get<TicketListDto[]>(routes.support.base),

  getStats: (): Promise<TicketStatsResponse> => apiClient.get<TicketStatsResponse>(routes.support.stats),

  getTicket: (id: string): Promise<TicketResponse> => apiClient.get<TicketResponse>(routes.support.ticket(id)),

  // Commands
  createTicket: (body: CreateTicketRequest): Promise<void> => apiClient.post<void>(routes.support.base, body),

  reply: (id: string, content: string): Promise<void> =>
    apiClient.post<void>(routes.support.reply(id), { content } satisfies TicketReplyRequest),

  updateStatus: (id: string, status: number): Promise<void> =>
    apiClient.patch<void>(routes.support.status(id), { status } satisfies UpdateTicketStatusRequest),
};
