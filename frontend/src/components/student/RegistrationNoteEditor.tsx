import { useEffect, useMemo, useRef, useState } from "react";
import ReactQuill from "react-quill-new";
import "react-quill-new/dist/quill.snow.css";
import DOMPurify from "dompurify";
import { FileViewerModal, type ViewerFile } from "@/components/common/FileViewerModal";
import {
  ACCEPTED_TYPES,
  MAX_ATTACHMENTS,
  MAX_FILE_SIZE_BYTES,
  MAX_TOTAL_SIZE_BYTES,
  formatFileSize,
  isSuspiciousDoubleExtension,
  topicPoolService,
} from "@/lib";
import type { NoteAttachment } from "@/types";

const IMAGE_TYPES = [".jpg", ".jpeg", ".png"];

const FILE_ICON_MAP: Record<string, string> = {
  ".pdf": "picture_as_pdf",
  ".doc": "description",
  ".docx": "description",
  ".xls": "table_chart",
  ".xlsx": "table_chart",
  ".ppt": "slideshow",
  ".pptx": "slideshow",
  ".zip": "folder_zip",
  ".rar": "folder_zip",
  ".jpg": "image",
  ".jpeg": "image",
  ".png": "image",
};

function getExt(name: string): string {
  return `.${name.split(".").pop()?.toLowerCase() ?? ""}`;
}

function fileIcon(name: string): string {
  return FILE_ICON_MAP[getExt(name)] ?? "draft";
}

function isImage(name: string): boolean {
  return IMAGE_TYPES.includes(getExt(name));
}

// Editor is pure rich-text now — all files (images + docs) are uploaded via the attachment section.
const FORMATS = ["bold", "italic", "underline", "strike", "list"];

const TOOLBAR = [
  ["bold", "italic", "underline", "strike"],
  [{ list: "ordered" }, { list: "bullet" }],
  ["clean"],
];

/** Quill's empty value is "<p><br></p>"; treat as empty when there's no text and no embedded media.
 *  Parsed via the DOM (no regex) so there's no backtracking / ReDoS risk on arbitrary HTML. */
export function isQuillNoteEmpty(html: string): boolean {
  const doc = new DOMParser().parseFromString(html, "text/html");
  const hasMedia = doc.body.querySelector("img, a") !== null;
  // String.trim() also strips non-breaking spaces, so an "&nbsp;"-only note counts as empty.
  const text = (doc.body.textContent ?? "").trim();
  return !hasMedia && text.length === 0;
}

interface ParsedNote {
  html: string;
  attachments: NoteAttachment[];
}

/**
 * Serializes the editor body (sanitized HTML, may contain inline images) plus the structured
 * attachment list into a single note payload (JSON), or undefined when there's nothing to send.
 * Stored in TopicRegistration.Note and rendered by <RegistrationNoteView/>.
 */
export function buildRegistrationNote(html: string, attachments: NoteAttachment[]): string | undefined {
  const empty = isQuillNoteEmpty(html);
  if (empty && attachments.length === 0) return undefined;

  const payload: ParsedNote = {
    html: empty ? "" : DOMPurify.sanitize(html, { ADD_ATTR: ["target", "rel"] }),
    attachments,
  };
  return JSON.stringify(payload);
}

/** Parses a stored note. New notes are JSON {html, attachments}; legacy notes are plain HTML. */
function parseNote(note: string): ParsedNote {
  try {
    const obj = JSON.parse(note);
    if (obj && typeof obj === "object" && ("html" in obj || "attachments" in obj)) {
      return {
        html: typeof obj.html === "string" ? obj.html : "",
        attachments: Array.isArray(obj.attachments) ? obj.attachments : [],
      };
    }
  } catch {
    /* not JSON → legacy plain-HTML note */
  }
  return { html: note, attachments: [] };
}

/**
 * Renders a stored registration note: the rich-text body (sanitized) plus attachments shown as
 * clean chips (icon + name + size + open), for every file type. Shared by student & lecturer views.
 */
export function RegistrationNoteView({ note, className }: { note: string; className?: string }) {
  const { html, attachments } = parseNote(note);
  const safeHtml = html ? DOMPurify.sanitize(html, { ADD_ATTR: ["target", "rel"] }) : "";
  const [viewerFile, setViewerFile] = useState<ViewerFile | null>(null);
  const bodyRef = useRef<HTMLDivElement>(null);

  // Inline images are injected HTML, so delegate clicks via a native listener (no JSX onClick on a
  // non-interactive container): clicking an image opens it in the preview modal.
  useEffect(() => {
    const el = bodyRef.current;
    if (!el) return;
    const onClick = (e: MouseEvent) => {
      const target = e.target as HTMLElement;
      if (target.tagName === "IMG") {
        const src = (target as HTMLImageElement).src;
        if (src) {
          e.preventDefault();
          setViewerFile({ url: src, name: "Hình ảnh" });
        }
      }
    };
    el.addEventListener("click", onClick);
    return () => el.removeEventListener("click", onClick);
  }, [safeHtml]);

  const imageAttachments = attachments.filter((a) => isImage(a.name));
  const docAttachments = attachments.filter((a) => !isImage(a.name));

  return (
    <div className={className}>
      {safeHtml && (
        <div
          ref={bodyRef}
          className="text-sm [&_img]:max-w-full [&_img]:max-h-[180px] [&_img]:object-contain [&_img]:rounded [&_img]:cursor-pointer [&_a]:text-primary [&_a]:underline"
          dangerouslySetInnerHTML={{ __html: safeHtml }}
        />
      )}
      {imageAttachments.length > 0 && (
        <div className="flex flex-wrap gap-2 mt-2">
          {imageAttachments.map((a) => (
            <button
              key={a.url}
              type="button"
              onClick={() => setViewerFile({ url: a.url, name: a.name })}
              title={a.name}
              className="h-32 w-32 rounded-lg overflow-hidden bg-slate-100 border border-slate-200 hover:opacity-90 transition-opacity"
            >
              <img src={a.url} alt={a.name} className="h-full w-full object-cover" />
            </button>
          ))}
        </div>
      )}
      {docAttachments.length > 0 && (
        <div className="flex flex-col gap-1.5 mt-2">
          {docAttachments.map((a) => (
            <button
              key={a.url}
              type="button"
              onClick={() => setViewerFile({ url: a.url, name: a.name })}
              className="flex items-center gap-2 p-2 rounded-lg bg-slate-50 hover:bg-slate-100 transition-colors text-left w-full"
            >
              <span className="material-symbols-outlined text-primary text-[18px] shrink-0">{fileIcon(a.name)}</span>
              <div className="flex-1 min-w-0">
                <p className="text-xs font-semibold text-[#101319] truncate">{a.name}</p>
                <p className="text-[10px] text-slate-500">{formatFileSize(a.size)}</p>
              </div>
              <span className="material-symbols-outlined text-slate-400 text-[18px] shrink-0">visibility</span>
            </button>
          ))}
        </div>
      )}
      <FileViewerModal file={viewerFile} onClose={() => setViewerFile(null)} />
    </div>
  );
}

interface RegistrationNoteEditorProps {
  value: string;
  onChange: (html: string) => void;
  attachments: NoteAttachment[];
  onAttachmentsChange: (next: NoteAttachment[]) => void;
  onError?: (message: string) => void;
  /** Editor placeholder. Defaults to the student wording; the lecturer reject flow overrides it. */
  placeholder?: string;
}

export function RegistrationNoteEditor({
  value,
  onChange,
  attachments,
  onAttachmentsChange,
  onError,
  placeholder = "Ghi chú cho giảng viên",
}: RegistrationNoteEditorProps) {
  const [fileUploading, setFileUploading] = useState(false);

  // Pure rich-text toolbar — no custom handlers; all files go through the attachment section below.
  const modules = useMemo(() => ({ toolbar: { container: TOOLBAR } }), []);

  const handleAddFiles = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const list = e.target.files;
    if (!list || list.length === 0) return;
    const incoming = Array.from(list);
    e.target.value = ""; // allow re-selecting the same file later

    const accepted: File[] = [];
    const rejected: string[] = [];
    let runningTotal = attachments.reduce((sum, a) => sum + a.size, 0);

    for (const file of incoming) {
      if (attachments.length + accepted.length >= MAX_ATTACHMENTS) {
        rejected.push(`Chỉ được đính kèm tối đa ${MAX_ATTACHMENTS} file.`);
        break;
      }
      if (!ACCEPTED_TYPES.includes(getExt(file.name))) {
        rejected.push(`'${file.name}' không đúng định dạng cho phép.`);
        continue;
      }
      if (isSuspiciousDoubleExtension(file.name)) {
        rejected.push(`'${file.name}' có tên file không an toàn.`);
        continue;
      }
      if (file.size > MAX_FILE_SIZE_BYTES) {
        rejected.push(`'${file.name}' vượt quá ${formatFileSize(MAX_FILE_SIZE_BYTES)}.`);
        continue;
      }
      if (runningTotal + file.size > MAX_TOTAL_SIZE_BYTES) {
        rejected.push(`Tổng dung lượng vượt quá ${formatFileSize(MAX_TOTAL_SIZE_BYTES)}.`);
        continue;
      }
      accepted.push(file);
      runningTotal += file.size;
    }

    if (rejected.length > 0) onError?.(rejected.join(" "));
    if (accepted.length === 0) return;

    try {
      setFileUploading(true);
      const uploaded: NoteAttachment[] = [];
      for (const file of accepted) {
        const res = await topicPoolService.uploadNoteAttachment(file);
        uploaded.push({ url: res.url, name: res.originalFileName, size: res.fileSize });
      }
      onAttachmentsChange([...attachments, ...uploaded]);
    } catch (err) {
      onError?.(err instanceof Error ? err.message : "Tải tệp lên thất bại.");
    } finally {
      setFileUploading(false);
    }
  };

  const removeAttachment = (url: string) => {
    onAttachmentsChange(attachments.filter((a) => a.url !== url));
  };

  const addDisabled = attachments.length >= MAX_ATTACHMENTS || fileUploading;
  const imageAttachments = attachments.filter((a) => isImage(a.name));
  const docAttachments = attachments.filter((a) => !isImage(a.name));

  return (
    <div className="flex flex-col gap-3">
      <div className="rich-note">
        <ReactQuill
          theme="snow"
          value={value}
          onChange={onChange}
          modules={modules}
          formats={FORMATS}
          placeholder={placeholder}
        />
      </div>

      {/* Attachments — single upload path for both images (shown as thumbnails) and documents */}
      <div className="flex flex-col gap-2">
        <div className="flex items-center justify-between">
          <span className="text-xs font-bold text-[#58698d] uppercase tracking-wider">Tài liệu &amp; hình ảnh đính kèm</span>
          <label
            className={`text-xs font-semibold flex items-center gap-1 ${
              addDisabled ? "text-slate-300 cursor-not-allowed" : "text-primary hover:underline cursor-pointer"
            }`}
          >
            <span className="material-symbols-outlined text-[16px]">attach_file</span>
            Thêm file
            <input
              type="file"
              multiple
              accept={ACCEPTED_TYPES.join(",")}
              className="hidden"
              disabled={addDisabled}
              onChange={handleAddFiles}
            />
          </label>
        </div>

        {fileUploading && <p className="text-xs text-primary">Đang tải tệp lên...</p>}

        {imageAttachments.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {imageAttachments.map((a) => (
              <div key={a.url} className="relative h-24 w-24 rounded-lg overflow-hidden bg-slate-100 border border-slate-200 group">
                <img src={a.url} alt={a.name} title={a.name} className="h-full w-full object-cover" />
                <button
                  type="button"
                  onClick={() => removeAttachment(a.url)}
                  title="Xoá"
                  className="absolute top-1 right-1 w-5 h-5 flex items-center justify-center rounded-full bg-black/50 text-white hover:bg-red-500 transition-colors"
                >
                  <span className="material-symbols-outlined text-[14px]">close</span>
                </button>
              </div>
            ))}
          </div>
        )}

        {docAttachments.length > 0 && (
          <div className="flex flex-col gap-1.5">
            {docAttachments.map((a) => (
              <div key={a.url} className="flex items-center gap-2 p-2 rounded-lg bg-slate-50">
                <span className="material-symbols-outlined text-primary text-[18px] shrink-0">{fileIcon(a.name)}</span>
                <div className="flex-1 min-w-0">
                  <p className="text-xs font-semibold text-[#101319] truncate">{a.name}</p>
                  <p className="text-[10px] text-slate-500">{formatFileSize(a.size)}</p>
                </div>
                <button
                  type="button"
                  onClick={() => removeAttachment(a.url)}
                  className="text-slate-400 hover:text-red-500 shrink-0"
                  title="Xoá"
                >
                  <span className="material-symbols-outlined text-[18px]">close</span>
                </button>
              </div>
            ))}
          </div>
        )}

        {attachments.length === 0 && (
          <p className="text-[11px] text-slate-400">
            Tối đa {MAX_ATTACHMENTS} file · {formatFileSize(MAX_FILE_SIZE_BYTES)}/file · {ACCEPTED_TYPES.join(", ")}
          </p>
        )}
      </div>
    </div>
  );
}
