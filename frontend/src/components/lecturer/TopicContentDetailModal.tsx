import { useEffect, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { topicService } from "@/lib";
import type { TopicDetail, TopicDocument } from "@/types";
import { FileViewerModal, type ViewerFile } from "@/components/common/FileViewerModal";

interface TopicContentDetailModalProps {
  /** Project to show; null closes the modal. */
  projectId: string | null;
  onClose: () => void;
}

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

function fileIcon(originalFileName: string): string {
  const ext = `.${originalFileName.split(".").pop()?.toLowerCase() ?? ""}`;
  return FILE_ICON_MAP[ext] ?? "draft";
}

function formatFileSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  return `${Math.ceil(bytes / 1024)} KB`;
}

function formatDate(iso: string | null | undefined): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

function parseTechnologies(tech: string | null): string[] {
  if (!tech) return [];
  return tech
    .split(",")
    .map((t) => t.trim())
    .filter(Boolean);
}

/**
 * Read-only content of a topic — description, objectives, scope, mentors and the attached files —
 * for the Department Head to review before assigning evaluators. Attachments open in
 * <see cref="FileViewerModal"/>, which renders PDF inline and DOCX through the Office viewer.
 */
export function TopicContentDetailModal({ projectId, onClose }: Readonly<TopicContentDetailModalProps>) {
  const [detail, setDetail] = useState<TopicDetail | null>(null);
  const [documents, setDocuments] = useState<TopicDocument[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [viewerFile, setViewerFile] = useState<ViewerFile | null>(null);

  useEffect(() => {
    if (!projectId) return;

    // Reset first so reopening never shows the previously viewed topic.
    setDetail(null);
    setDocuments([]);
    setError(null);
    setLoading(true);

    let cancelled = false;
    topicService
      .getTopicDetail(projectId)
      .then((d) => {
        if (!cancelled) setDetail(d);
      })
      .catch((e: unknown) => {
        if (!cancelled) setError(e instanceof Error ? e.message : "Không thể tải chi tiết đề tài.");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    // Attachments are a separate endpoint; a failure just leaves the list empty.
    topicService
      .getTopicDocuments(projectId)
      .then((docs) => {
        if (!cancelled) setDocuments(docs);
      })
      .catch(() => {
        if (!cancelled) setDocuments([]);
      });

    return () => {
      cancelled = true;
    };
  }, [projectId]);

  const techs = parseTechnologies(detail?.technologies ?? null);

  function renderBody() {
    if (loading) {
      return (
        <div className="flex items-center justify-center gap-3 py-12 text-slate-400">
          <span className="material-symbols-outlined animate-spin">progress_activity</span>
          <span className="text-sm">Đang tải chi tiết đề tài...</span>
        </div>
      );
    }
    if (error) {
      return (
        <div className="flex flex-col items-center gap-2 py-12 text-center">
          <span className="material-symbols-outlined text-4xl text-amber-400">report</span>
          <p className="text-sm font-medium text-slate-600">{error}</p>
        </div>
      );
    }
    if (!detail) return null;

    return (
      <div className="space-y-5">
        <Section title="Tên đề tài" icon="title">
          <p className="text-sm font-semibold text-[#101319]">{detail.nameVi}</p>
          <p className="mt-0.5 text-sm italic text-slate-500">{detail.nameEn}</p>
          {detail.nameAbbr && <p className="mt-0.5 text-xs text-slate-400">Tên viết tắt: {detail.nameAbbr}</p>}
        </Section>

        <Section title="Mô tả" icon="description">
          <p className="whitespace-pre-line text-sm leading-relaxed text-slate-600">{detail.description || "—"}</p>
        </Section>

        <Section title="Mục tiêu" icon="target">
          <p className="whitespace-pre-line text-sm leading-relaxed text-slate-600">{detail.objectives || "—"}</p>
        </Section>

        {detail.scope && (
          <Section title="Phạm vi" icon="crop_free">
            <p className="whitespace-pre-line text-sm leading-relaxed text-slate-600">{detail.scope}</p>
          </Section>
        )}

        {detail.expectedResults && (
          <Section title="Kết quả mong đợi" icon="emoji_events">
            <p className="whitespace-pre-line text-sm leading-relaxed text-slate-600">{detail.expectedResults}</p>
          </Section>
        )}

        {techs.length > 0 && (
          <Section title="Công nghệ" icon="code">
            <div className="flex flex-wrap gap-1.5">
              {techs.map((t) => (
                <span key={t} className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600">
                  {t}
                </span>
              ))}
            </div>
          </Section>
        )}

        {detail.mentors.length > 0 && (
          <Section title="Giảng viên hướng dẫn" icon="school">
            <div className="flex flex-wrap gap-2">
              {detail.mentors.map((m) => (
                <span
                  key={m.mentorId}
                  className="rounded-lg bg-slate-50 px-2.5 py-1 text-sm font-medium text-slate-700"
                >
                  {m.fullName}
                </span>
              ))}
            </div>
          </Section>
        )}

        <Section title={`Tài liệu đính kèm (${documents.length})`} icon="attach_file">
          {documents.length > 0 ? (
            <div className="flex flex-col gap-2">
              {documents.map((doc) => (
                <button
                  key={doc.id}
                  type="button"
                  disabled={!doc.fileUrl}
                  onClick={() => setViewerFile({ url: doc.fileUrl, name: doc.originalFileName })}
                  className="flex items-center gap-3 rounded-lg bg-slate-50 p-2.5 text-left transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60"
                  title={doc.fileUrl ? "Xem tài liệu" : "Tệp chưa sẵn sàng để xem"}
                >
                  <span className="material-symbols-outlined shrink-0 text-xl text-primary">
                    {fileIcon(doc.originalFileName)}
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-xs font-semibold text-[#101319]">{doc.originalFileName}</p>
                    <p className="text-[10px] text-slate-500">
                      {formatFileSize(doc.fileSize)} · {formatDate(doc.uploadedAt)} · {doc.uploadedByName}
                    </p>
                  </div>
                  {doc.fileUrl && (
                    <span className="material-symbols-outlined shrink-0 text-[18px] text-slate-400">visibility</span>
                  )}
                </button>
              ))}
            </div>
          ) : (
            <p className="text-sm text-slate-400">Chưa có tài liệu đính kèm.</p>
          )}
        </Section>
      </div>
    );
  }

  return (
    <>
      <AnimatePresence>
        {projectId && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
            onClick={onClose}
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.96, y: 10 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.96, y: 10 }}
              onClick={(e) => e.stopPropagation()}
              className="flex max-h-[92vh] w-full max-w-3xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl"
            >
              {/* Header */}
              <div className="flex items-start justify-between gap-3 border-b border-gray-200 px-6 py-4">
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-mono text-xs text-slate-400">{detail?.code ?? "—"}</span>
                    {detail && (
                      <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-600">
                        {detail.majorCode}
                      </span>
                    )}
                  </div>
                  <h3 className="mt-0.5 text-base font-bold text-slate-900">Chi tiết đề tài</h3>
                  {detail && (
                    <p className="mt-0.5 text-xs text-slate-500">
                      {detail.majorName} · Số sinh viên tối đa: {detail.maxStudents} · Tạo ngày{" "}
                      {formatDate(detail.createdAt)}
                    </p>
                  )}
                </div>
                <button
                  type="button"
                  onClick={onClose}
                  className="flex size-8 shrink-0 items-center justify-center rounded-lg text-slate-400 hover:bg-gray-100"
                >
                  <span className="material-symbols-outlined text-[20px]">close</span>
                </button>
              </div>

              {/* Body */}
              <div className="flex-1 overflow-y-auto px-6 py-4">{renderBody()}</div>

              {/* Footer */}
              <div className="flex justify-end border-t border-gray-200 px-6 py-4">
                <button
                  type="button"
                  onClick={onClose}
                  className="h-10 rounded-xl border border-gray-200 px-4 text-sm font-semibold text-slate-700 hover:bg-gray-50"
                >
                  Đóng
                </button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      <FileViewerModal file={viewerFile} onClose={() => setViewerFile(null)} />
    </>
  );
}

function Section({ title, icon, children }: Readonly<{ title: string; icon: string; children: React.ReactNode }>) {
  return (
    <div>
      <div className="mb-2 flex items-center gap-2">
        <span className="material-symbols-outlined text-base text-primary">{icon}</span>
        <h4 className="text-sm font-bold text-[#101319]">{title}</h4>
      </div>
      {children}
    </div>
  );
}
