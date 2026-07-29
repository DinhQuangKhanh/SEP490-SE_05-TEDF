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
    let message = `HTTP ${response.status}: ${response.statusText}`;
    let code: string | undefined;

    try {
      const body = (await response.json()) as ApiErrorBody;
      code = body?.code;
      if (body?.message) {
        message = body.message;
      }

      if (body?.errors) {
        const fieldErrors = Object.values(body.errors).flat().join(" ");
        if (fieldErrors) {
          message += ` — ${fieldErrors}`;
        }
      }
    } catch {
      // non-JSON error response, keep the fallback message
    }

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
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
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
        let message = `HTTP ${response.status}: ${response.statusText}`;

        try {
          const body = (await response.json()) as ApiErrorBody;
          if (body?.message) {
            message = body.message;
          }

          if (body?.errors) {
            const fieldErrors = Object.values(body.errors).flat().join(" ");
            if (fieldErrors) {
              message += ` — ${fieldErrors}`;
            }
          }
        } catch {
          // non-JSON error response
        }

        throw new Error(message);
      }

      return readResponse<T>(response);
    });
  },
};