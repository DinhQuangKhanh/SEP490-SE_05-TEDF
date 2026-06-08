// Shared API envelope shapes used by the low-level apiClient.

/** The standard backend response envelope (ApiResponse<T>). */
export interface ApiEnvelope<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: Record<string, string[]>;
}

/** Error body returned on non-2xx responses. */
export interface ApiErrorBody {
  code?: string;
  message?: string;
  errors?: Record<string, string[]>;
}
