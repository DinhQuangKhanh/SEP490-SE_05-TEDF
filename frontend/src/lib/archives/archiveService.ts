import { ArchiveGroup } from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const archiveService = {
  /** Admin: archived projects grouped by academic year. */
  getArchives: (): Promise<ArchiveGroup[]> => apiClient.get<ArchiveGroup[]>(routes.admin.archives),

  /** Admin: URL to download an archived project (server returns a redirect). */
  downloadArchiveUrl: (id: string): string => routes.admin.archiveDownload(id),
};
