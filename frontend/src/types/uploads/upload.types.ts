// Synchronous file upload (note editor images + attachments).
// POST /api/uploads/note-attachment

export interface NoteAttachmentUploadResponse {
  url: string;
  originalFileName: string;
  fileSize: number;
  contentType: string;
}

/** An attachment already uploaded to storage, kept in the registration-note editor's list. */
export interface NoteAttachment {
  url: string;
  name: string;
  size: number;
}
