import { ArchiveGroup, PublicSettings, SystemSetting } from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

// ── Service ────────────────────────────────────────────────
export const settingsService = {
  /** Anonymous: branding + maintenance the SPA applies on load. */
  getPublicSettings: () => apiClient.get<PublicSettings>(routes.common.publicSettings),

  /** Admin: full settings list. */
  getAdminSettings: () => apiClient.get<SystemSetting[]>(routes.admin.settings),

  /** Admin: upsert the supplied key → value map. */
  updateSettings: (settings: Record<string, string>) => apiClient.put<unknown>(routes.admin.settings, settings),

  /** Admin: send a test email to the current admin's own address. */
  sendTestEmail: () => apiClient.post<unknown>(routes.admin.settingsTestEmail),

  /** Admin: upload a new system logo; returns its public URL. */
  uploadLogo: (file: File) => {
    const fd = new FormData();
    fd.append("file", file);
    return apiClient.postForm<{ logoUrl: string }>(routes.admin.settingsLogo, fd);
  },

  /** Admin: archived projects grouped by academic year. */
  getArchives: () => apiClient.get<ArchiveGroup[]>(routes.admin.archives),
};
