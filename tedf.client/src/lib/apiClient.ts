const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "";
console.log("API Base URL:", API_BASE);

function getToken(): string | null {
  try {
    const stored = localStorage.getItem("user");
    if (!stored) return null;
    const user = JSON.parse(stored);
    return user?.firebaseToken ?? null;
  } catch {
    return null;
  }
}

interface ApiErrorBody {
  code?: string;
  message?: string;
  errors?: Record<string, string[]>;
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

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    "X-Route-Path": window.location.pathname,
    ...(options.headers as Record<string, string>),
  };
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${path}`, { ...options, headers });

  if (!response.ok) {
    let message = `HTTP ${response.status}: ${response.statusText}`;
    try {
      const body: ApiErrorBody = await response.json();
      if (body?.message) {
        message = body.message;
      }
      if (body?.errors) {
        const fieldErrors = Object.values(body.errors).flat().join(" ");
        if (fieldErrors) message += ` — ${fieldErrors}`;
      }
    } catch {
      // body is not JSON, keep the default message
    }
    throw new Error(message);
  }

  const text = await response.text();
  if (!text) return {} as T;
  const body: unknown = JSON.parse(text);

  if (isApiEnvelope<T>(body)) {
    if (!body.success) {
      throw new Error(body.message || "Đã xảy ra lỗi không xác định.");
    }
    return body.data as T;
  }

  return body as T;
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "POST", body: body ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PUT", body: body ? JSON.stringify(body) : undefined }),
  patch: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PATCH", body: body ? JSON.stringify(body) : undefined }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
  /** Send a FormData payload (file uploads). Browser sets Content-Type + boundary automatically. */
  postForm: <T>(path: string, formData: FormData): Promise<T> => {
    const token = getToken();
    const headers: Record<string, string> = {
      "X-Route-Path": window.location.pathname,
    };
    if (token) headers["Authorization"] = `Bearer ${token}`;
    return fetch(`${API_BASE}${path}`, { method: "POST", headers, body: formData }).then(async (res) => {
      if (!res.ok) {
        let message = `HTTP ${res.status}: ${res.statusText}`;
        try {
          const body: ApiErrorBody = await res.json();
          if (body?.message) message = body.message;
          if (body?.errors) {
            const fieldErrors = Object.values(body.errors).flat().join(" ");
            if (fieldErrors) message += ` — ${fieldErrors}`;
          }
        } catch {
          /* non-JSON body */
        }
        throw new Error(message);
      }
      const text = await res.text();
      if (!text) return {} as T;
      const body: unknown = JSON.parse(text);
      if (isApiEnvelope<T>(body)) {
        if (!body.success) {
          throw new Error(body.message || "Đã xảy ra lỗi không xác định.");
        }
        return body.data as T;
      }
      return body as T;
    });
  },
};
