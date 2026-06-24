import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";
import type { SessionInfo } from "@/types";

export const authService = {
  /** Session bootstrap — call after Firebase sign-in to learn the account's access state. */
  getSession: (): Promise<SessionInfo> => apiClient.get<SessionInfo>(routes.auth.session),
};
