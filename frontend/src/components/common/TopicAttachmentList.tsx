import { useMemo, useState } from "react";
import type { TopicDocument } from "@/types";
import { FileViewerModal, type ViewerFile } from "@/components/common/FileViewerModal";

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

/** Material icon name for a file, by extension. */
export function fileIconFor(fileName: string): string {
  const ext = `.${fileName.split(".").pop()?.toLowerCase() ?? ""}`;
  return FILE_ICON_MAP[ext] ?? "draft";
}

/** The capstone register form is stored as a Proposal-type document. */
export function isRegistrationForm(doc: TopicDocument): boolean {
  return doc.documentType === "Proposal";
}

/** Registration form(s) pinned to the top; otherwise newest upload first. */
export function sortAttachments(documents: TopicDocument[]): TopicDocument[] {
  return [...documents].sort((a, b) => {
    const rankA = isRegistrationForm(a) ? 0 : 1;
    const rankB = isRegistrationForm(b) ? 0 : 1;
    if (rankA !== rankB) return rankA - rankB;
    return (b.uploadedAt ?? "").localeCompare(a.uploadedAt ?? "");
  });
}

function formatFileSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  return `${Math.ceil(bytes / 1024)} KB`;
}

function formatDate(iso: string | null | undefined): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

interface TopicAttachmentListProps {
  documents: TopicDocument[];
  /** Optional heading (with count). Pass null to render just the rows inside an existing card. */
  title?: string | null;
  /** Message shown when there are no documents. */
  emptyText?: string;
}

/**
 * The topic's attachments as a clickable list. Each row opens the file in <see cref="FileViewerModal"/>
 * — PDF inline (iframe) and DOCX/Office via the Office Online viewer, no download. The register form
 * (documentType "Proposal") is labelled "Phiếu đăng ký" and pinned to the top; a document still being
 * malware-scanned (no public url yet) is shown disabled until it is promoted.
 */
export function TopicAttachmentList({
  documents,
  title = "Tài liệu đính kèm",
  emptyText = "Chưa có tài liệu đính kèm.",
}: Readonly<TopicAttachmentListProps>) {
  const [viewerFile, setViewerFile] = useState<ViewerFile | null>(null);
  const sorted = useMemo(() => sortAttachments(documents), [documents]);

  return (
    <div>
      {title !== null && (
        <div className="mb-2 flex items-center gap-2">
          <span className="material-symbols-outlined text-base text-primary">attach_file</span>
          <h4 className="text-sm font-bold text-[#101319]">
            {title} ({documents.length})
          </h4>
        </div>
      )}

      {sorted.length === 0 ? (
        <p className="text-sm text-slate-400">{emptyText}</p>
      ) : (
        <div className="flex flex-col gap-2">
          {sorted.map((doc) => {
            const isRegForm = isRegistrationForm(doc);
            const ready = !!doc.fileUrl;
            return (
              <button
                key={doc.id}
                type="button"
                disabled={!ready}
                onClick={() => ready && setViewerFile({ url: doc.fileUrl, name: doc.originalFileName })}
                title={ready ? "Xem tài liệu" : "Tệp đang được quét mã độc — chưa sẵn sàng để xem"}
                className="flex items-center gap-3 rounded-lg bg-slate-50 p-2.5 text-left transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60"
              >
                <span className={`material-symbols-outlined shrink-0 text-xl ${isRegForm ? "text-emerald-600" : "text-primary"}`}>
                  {isRegForm ? "assignment" : fileIconFor(doc.originalFileName)}
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-1.5">
                    <p className="truncate text-xs font-semibold text-[#101319]">{doc.originalFileName}</p>
                    {isRegForm && (
                      <span className="shrink-0 rounded-full bg-emerald-100 px-1.5 py-0.5 text-[9px] font-bold text-emerald-700">
                        Phiếu đăng ký
                      </span>
                    )}
                  </div>
                  <p className="text-[10px] text-slate-500">
                    {formatFileSize(doc.fileSize)} · {formatDate(doc.uploadedAt)}
                    {doc.uploadedByName ? ` · ${doc.uploadedByName}` : ""}
                  </p>
                </div>
                <span className="material-symbols-outlined shrink-0 text-[18px] text-slate-400">
                  {ready ? "visibility" : "hourglass_empty"}
                </span>
              </button>
            );
          })}
        </div>
      )}

      <FileViewerModal file={viewerFile} onClose={() => setViewerFile(null)} />
    </div>
  );
}
