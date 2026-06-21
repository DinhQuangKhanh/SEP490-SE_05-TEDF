import { NoteAttachmentUploadResponse } from "@/types";
import { apiClient } from "../common/apiClient";
import { routes } from "../common/routes";

export const uploadService = {
  /**
   * Uploads a single file (image or attachment) for the registration-note editor and returns
   * its public URL immediately. Format/size are validated server-side; no async malware scan.
   */
  uploadNoteAttachment: (file: File): Promise<NoteAttachmentUploadResponse> => {
    const formData = new FormData();
    formData.append("file", file);
    return apiClient.postForm<NoteAttachmentUploadResponse>(routes.uploads.noteAttachment, formData);
  },
};
