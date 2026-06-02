import { apiClient } from "./apiClient";

// ── Types ──────────────────────────────────────────────────

/** Non-secret branding + maintenance subset, readable by any client at startup. */
export interface PublicSettings {
  primaryColor: string;
  headerName: string;
  logoUrl: string;
  maintenanceMode: boolean;
  version: string;
}

/** A single admin-editable system setting row. */
export interface SystemSetting {
  key: string;
  value: string;
  dataType: string;
  description?: string | null;
  category?: string | null;
}

/** Archived projects summarised per academic year. */
export interface ArchiveGroup {
  academicYear: string;
  projectCount: number;
  totalSizeBytes: number;
}

// ── Service ────────────────────────────────────────────────

export const settingsService = {
  /** Anonymous: branding + maintenance the SPA applies on load. */
  getPublicSettings: () => apiClient.get<PublicSettings>("/api/settings/public"),

  /** Admin: full settings list. */
  getAdminSettings: () => apiClient.get<SystemSetting[]>("/api/admin/settings"),

  /** Admin: upsert the supplied key → value map. */
  updateSettings: (settings: Record<string, string>) =>
    apiClient.put<unknown>("/api/admin/settings", settings),

  /** Admin: send a test email to the current admin's own address. */
  sendTestEmail: () => apiClient.post<unknown>("/api/admin/settings/test-email"),

  /** Admin: upload a new system logo; returns its public URL. */
  uploadLogo: (file: File) => {
    const fd = new FormData();
    fd.append("file", file);
    return apiClient.postForm<{ logoUrl: string }>("/api/admin/settings/logo", fd);
  },

  /** Admin: archived projects grouped by academic year. */
  getArchives: () => apiClient.get<ArchiveGroup[]>("/api/admin/archives"),
};
