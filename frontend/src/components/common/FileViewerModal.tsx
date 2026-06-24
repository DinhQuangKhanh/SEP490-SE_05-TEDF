import { createPortal } from "react-dom";
import type { ReactNode } from "react";

export interface ViewerFile {
  url: string;
  name: string;
}

const IMAGE_EXTS = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg"];
const VIDEO_EXTS = [".mp4", ".webm", ".ogg", ".mov"];
const OFFICE_EXTS = [".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"];

function getExt(value: string): string {
  // Strip query/hash so storage URLs like ".../{guid}.jpg?token=..." resolve correctly.
  const clean = value.split("?")[0].split("#")[0];
  const lastDot = clean.lastIndexOf(".");
  const lastSlash = clean.lastIndexOf("/");
  // Only treat as an extension if the dot is in the final path segment.
  if (lastDot <= lastSlash) return "";
  return `.${clean.slice(lastDot + 1).toLowerCase()}`;
}

/** Detect the file extension from the URL (which always carries the real one, e.g.
 *  inline images whose display name is just "Hình ảnh"), falling back to the name. */
function resolveExt(file: ViewerFile): string {
  return getExt(file.url) || getExt(file.name);
}

function headerIcon(ext: string): string {
  if (IMAGE_EXTS.includes(ext)) return "image";
  if (VIDEO_EXTS.includes(ext)) return "movie";
  if (ext === ".pdf") return "picture_as_pdf";
  if ([".doc", ".docx"].includes(ext)) return "description";
  if ([".xls", ".xlsx"].includes(ext)) return "table_chart";
  if ([".ppt", ".pptx"].includes(ext)) return "slideshow";
  if ([".zip", ".rar"].includes(ext)) return "folder_zip";
  return "draft";
}

/**
 * In-place file preview modal: header shows the file name, the body embeds the file
 * (image / video / PDF / Office docs). The dimmed/blurred backdrop does NOT close the modal —
 * only the explicit close button does.
 */
export function FileViewerModal({ file, onClose }: { file: ViewerFile | null; onClose: () => void }) {
  if (!file) return null;

  const ext = resolveExt(file);
  let body: ReactNode;

  if (IMAGE_EXTS.includes(ext)) {
    body = <img src={file.url} alt={file.name} className="max-w-full max-h-full object-contain mx-auto" />;
  } else if (VIDEO_EXTS.includes(ext)) {
    body = <video src={file.url} controls className="max-w-full max-h-full mx-auto" />;
  } else if (ext === ".pdf") {
    body = <iframe src={file.url} title={file.name} className="w-full h-full border-0" />;
  } else if (OFFICE_EXTS.includes(ext)) {
    // Office Online viewer renders public doc/xls/ppt URLs inline.
    body = (
      <iframe
        src={`https://view.officeapps.live.com/op/embed.aspx?src=${encodeURIComponent(file.url)}`}
        title={file.name}
        className="w-full h-full border-0"
      />
    );
  } else {
    body = (
      <div className="flex flex-col items-center justify-center h-full gap-3 text-center text-slate-500">
        <span className="material-symbols-outlined text-5xl text-slate-300">draft</span>
        <p>Không thể xem trước loại tệp này.</p>
        <a
          href={file.url}
          target="_blank"
          rel="noopener noreferrer"
          className="px-4 py-2 bg-primary text-white rounded-lg text-sm font-semibold no-underline"
        >
          Tải xuống
        </a>
      </div>
    );
  }

  return createPortal(
    <div className="fixed inset-0 z-[100] bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-5xl h-[85vh] flex flex-col overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between gap-4 px-5 py-3 border-b border-slate-200 shrink-0">
          <div className="flex items-center gap-2 min-w-0">
            <span className="material-symbols-outlined text-primary shrink-0">{headerIcon(ext)}</span>
            <h3 className="font-semibold text-slate-900 truncate" title={file.name}>
              {file.name}
            </h3>
          </div>
          <div className="flex items-center gap-1 shrink-0">
            <a
              href={file.url}
              target="_blank"
              rel="noopener noreferrer"
              title="Mở ở tab mới"
              className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-500 hover:text-slate-700 transition-colors"
            >
              <span className="material-symbols-outlined text-[20px]">open_in_new</span>
            </a>
            <button
              onClick={onClose}
              title="Đóng"
              className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-500 hover:text-slate-700 transition-colors"
            >
              <span className="material-symbols-outlined text-[20px]">close</span>
            </button>
          </div>
        </div>

        {/* Body */}
        <div className="flex-1 min-h-0 overflow-auto bg-slate-50 p-3 flex">{body}</div>
      </div>
    </div>,
    document.body,
  );
}
