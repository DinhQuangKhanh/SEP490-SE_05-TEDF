import { auth } from "@/config/firebase";

const API_BASE = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/+$/, "");

/**
 * Bearer token for the API.
 *
 * Asks Firebase for the token rather than reusing the copy written to localStorage at sign-in:
 * a Firebase ID token expires after one hour, so the stored snapshot starts returning 401 on
 * every call once that hour is up — including GET /api/auth/session, which silently drops the
 * user back to the fallback role. getIdToken() refreshes transparently when needed.
 *
 * The localStorage copy stays as a fallback for the mock login path used when Firebase is not
 * configured, and for the brief moment before Firebase restores the session on a page load.
 */
async function getToken(): Promise<string | null> {
  const current = auth.currentUser;
  if (current) {
    try {
      return await current.getIdToken();
    } catch {
      // Refresh failed (offline, revoked session) — fall through to the stored copy.
    }
  }

  try {
    const stored = localStorage.getItem("user");
    if (!stored) return null;
    const user = JSON.parse(stored) as { firebaseToken?: string | null };
    return user.firebaseToken ?? null;
  } catch {
    return null;
  }
}

interface ApiErrorBody {
  code?: string;
  message?: string;
  errors?: Record<string, string[]>;
}

/**
 * Thrown for any non-2xx response. Carries the HTTP status so callers can tell an authentication
 * problem (401/403) apart from a server fault — a plain Error only exposed a message string, which
 * made "not signed in" and "server crashed" indistinguishable at the call site.
 *
 * A network-level failure (DNS, CORS, untrusted dev certificate) never reaches here: fetch itself
 * rejects with a TypeError before any response exists.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;

  constructor(message: string, status: number, code?: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

/** Friendly Vietnamese fallback per HTTP status, used only when the server returns no message. */
const STATUS_MESSAGES: Record<number, string> = {
  400: "Yêu cầu không hợp lệ.",
  401: "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
  403: "Bạn không có quyền thực hiện thao tác này.",
  404: "Không tìm thấy chức năng này trên máy chủ (404) — máy chủ có thể chưa được cập nhật.",
  405: "Thao tác không được máy chủ hỗ trợ.",
  409: "Dữ liệu bị xung đột. Vui lòng tải lại trang và thử lại.",
  413: "Tệp tải lên quá lớn.",
  415: "Định dạng tệp không được hỗ trợ.",
  422: "Dữ liệu không hợp lệ.",
  429: "Bạn thao tác quá nhanh. Vui lòng thử lại sau giây lát.",
  500: "Máy chủ gặp lỗi nội bộ. Vui lòng thử lại sau.",
  502: "Không kết nối được máy chủ (502). Vui lòng thử lại sau.",
  503: "Dịch vụ tạm thời không khả dụng. Vui lòng thử lại sau.",
  504: "Máy chủ phản hồi quá lâu. Vui lòng thử lại sau.",
};

/**
 * Turns a non-OK Response into a human-friendly message (+ optional error code). Prefers the
 * server's own message (ApiResponse envelope, or a short plain-text body); otherwise falls back
 * to a status-specific Vietnamese message — never a bare "HTTP 404:" with no explanation.
 */
export async function buildErrorMessage(response: Response): Promise<{ message: string; code?: string }> {
  const fallback =
    STATUS_MESSAGES[response.status] ??
    (response.statusText
      ? `Lỗi ${response.status}: ${response.statusText}`
      : `Đã xảy ra lỗi khi kết nối máy chủ (HTTP ${response.status}).`);

  let text: string;
  try {
    text = await response.text();
  } catch {
    return { message: fallback };
  }
  if (!text.trim()) {
    return { message: fallback };
  }

  try {
    const body = JSON.parse(text) as ApiErrorBody;
    let message = body?.message?.trim() || fallback;
    if (body?.errors) {
      const fieldErrors = Object.values(body.errors).flat().join(" ");
      if (fieldErrors) message += ` — ${fieldErrors}`;
    }
    return { message, code: body?.code };
  } catch {
    // Non-JSON body (e.g. a short plain-text error). Ignore HTML error pages / very long bodies.
    const trimmed = text.trim();
    if (trimmed.length <= 300 && !trimmed.startsWith("<")) {
      return { message: trimmed };
    }
    return { message: fallback };
  }
}

interface ApiEnvelope<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: Record<string, string[]>;
}

function isApiEnvelope<T>(body: unknown): body is ApiEnvelope<T> {
  return typeof body === "object" && body !== null && "success" in body && "message" in body;
}

async function readResponse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return {} as T;
  }

  const text = await response.text();
  if (!text) {
    return {} as T;
  }

  const body: unknown = JSON.parse(text);
  if (isApiEnvelope<T>(body)) {
    if (!body.success) {
      throw new Error(body.message || "Đã xảy ra lỗi không xác định.");
    }

    return body.data as T;
  }

  return body as T;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = await getToken();
  const headers = new Headers(options.headers ?? {});
  headers.set("Accept", "application/json");
  headers.set("X-Route-Path", window.location.pathname);

  if (options.body && !(options.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const { message, code } = await buildErrorMessage(response);
    throw new ApiError(message, response.status, code);
  }

  return readResponse<T>(response);
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path),

  post: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: "POST",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    }),

  put: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: "PUT",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    }),

  patch: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: "PATCH",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    }),

  delete: <T>(path: string) =>
    request<T>(path, {
      method: "DELETE",
    }),

  /** Authenticated binary GET (e.g. Excel/PDF downloads). Returns the raw Blob. */
  getBlob: async (path: string): Promise<Blob> => {
    const token = await getToken();
    const headers = new Headers();
    headers.set("X-Route-Path", window.location.pathname);
    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }

    const response = await fetch(`${API_BASE}${path}`, { headers });
    if (!response.ok) {
      const { message, code } = await buildErrorMessage(response);
      throw new ApiError(message, response.status, code);
    }
    return response.blob();
  },

  postForm: async <T>(path: string, formData: FormData): Promise<T> => {
    const token = await getToken();
    const headers = new Headers();
    headers.set("Accept", "application/json");
    headers.set("X-Route-Path", window.location.pathname);

    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }

    return fetch(`${API_BASE}${path}`, {
      method: "POST",
      headers,
      body: formData,
    }).then(async (response) => {
      if (!response.ok) {
        const { message, code } = await buildErrorMessage(response);
        throw new ApiError(message, response.status, code);
      }

      return readResponse<T>(response);
    });
  },
};
