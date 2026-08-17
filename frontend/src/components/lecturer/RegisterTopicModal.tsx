import { useEffect, useRef, useState } from "react";
import ReactQuill from "react-quill-new";
import "react-quill-new/dist/quill.snow.css";
import DOMPurify from "dompurify";
import { motion, AnimatePresence } from "framer-motion";
import type { RegisterFormPreview, TopicPoolDto } from "@/types";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { topicPoolService } from "@/lib";
import { validateRegisterFormFile, formatFileSize } from "@/lib/common/fileUploadUtils";

interface RegisterTopicModalProps {
  isOpen: boolean;
  onClose: (success?: boolean) => void;
}

const REGISTER_FORM_ACCEPT = ".pdf,.doc,.docx";
const QUILL_TOOLBAR = [["bold", "italic", "underline"], [{ list: "ordered" }, { list: "bullet" }], ["link"], ["clean"]];
const QUILL_FORMATS = ["bold", "italic", "underline", "list", "link"];

/** Quill's empty document is "<p><br></p>"; treat as empty when there is no text and no link. */
function isQuillEmpty(html: string): boolean {
  const doc = new DOMParser().parseFromString(html, "text/html");
  const hasLink = doc.body.querySelector("a") !== null;
  return !hasLink && (doc.body.textContent ?? "").trim().length === 0;
}

/**
 * Propose a topic into a pool by uploading the completed "Capstone Project Register" form. The form is
 * scanned + parsed + validated server-side on upload (step A); only when it comes back clean & complete
 * does "Gửi phê duyệt" unlock (step B). The single free-text input is an optional rich-text note.
 */
export function RegisterTopicModal({ isOpen, onClose }: RegisterTopicModalProps) {
  const { showError } = useSystemError();

  const [pools, setPools] = useState<TopicPoolDto[]>([]);
  const [loadingPools, setLoadingPools] = useState(false);
  const [poolId, setPoolId] = useState("");

  const [file, setFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [validating, setValidating] = useState(false);
  const [preview, setPreview] = useState<RegisterFormPreview | null>(null);

  const [note, setNote] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Load the pools accepting proposals when the modal opens.
  useEffect(() => {
    if (!isOpen) return;
    setLoadingPools(true);
    topicPoolService
      .getTopicPools()
      .then((data) => setPools(data.filter((p) => p.statusName === "Active")))
      .catch(() => showError("Không thể tải danh sách kho đề tài."))
      .finally(() => setLoadingPools(false));
  }, [isOpen, showError]);

  // Reset everything when closed so a re-open starts clean.
  useEffect(() => {
    if (isOpen) return;
    setPoolId("");
    setFile(null);
    setFileError(null);
    setValidating(false);
    setPreview(null);
    setNote("");
    setSubmitting(false);
    setShowSuccess(false);
  }, [isOpen]);

  // Step A: scan + parse + validate the form against the chosen pool.
  const runValidation = async (targetPoolId: string, picked: File) => {
    setValidating(true);
    setFileError(null);
    setPreview(null);
    try {
      setPreview(await topicPoolService.validateRegisterForm(targetPoolId, picked));
    } catch (err) {
      setFileError(err instanceof Error ? err.message : "Phiếu đăng ký không hợp lệ.");
    } finally {
      setValidating(false);
    }
  };

  const handleFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const picked = e.target.files?.[0];
    e.target.value = ""; // allow re-picking the same file after a rejection
    if (!picked) return;

    const localError = validateRegisterFormFile(picked); // extension + size
    if (localError) {
      setFile(null);
      setPreview(null);
      setFileError(localError);
      return;
    }

    setFile(picked);
    setPreview(null);
    if (!poolId) {
      setFileError("Vui lòng chọn kho đề tài trước khi tải phiếu lên.");
      return;
    }
    void runValidation(poolId, picked);
  };

  const handlePoolChange = (id: string) => {
    setPoolId(id);
    setPreview(null);
    if (file && id) void runValidation(id, file);
  };

  const canSubmit = Boolean(poolId) && Boolean(file) && Boolean(preview) && !validating && !submitting;

  const handleSubmit = async () => {
    if (!canSubmit || !file) return;
    setSubmitting(true);
    try {
      const formData = new FormData();
      formData.append("registerForm", file);
      if (!isQuillEmpty(note))
        formData.append("note", DOMPurify.sanitize(note, { ADD_ATTR: ["target", "rel"] }));

      await topicPoolService.proposeTopic(poolId, formData);
      setShowSuccess(true);
      setTimeout(() => onClose(true), 1600);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Đề xuất đề tài thất bại. Vui lòng thử lại.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm"
          onClick={() => onClose()}
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.96, y: 16 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.96, y: 16 }}
            transition={{ type: "spring", damping: 26, stiffness: 320 }}
            onClick={(e) => e.stopPropagation()}
            className="flex max-h-[90vh] w-[92vw] max-w-[720px] flex-col overflow-hidden rounded-2xl bg-white shadow-2xl"
          >
            {showSuccess ? (
              <div className="flex flex-col items-center justify-center px-8 py-16 text-center">
                <motion.div
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  transition={{ type: "spring", damping: 15, stiffness: 200, delay: 0.1 }}
                  className="mb-6 flex size-20 items-center justify-center rounded-full bg-emerald-100"
                >
                  <span className="material-symbols-outlined text-[44px] text-emerald-600">check_circle</span>
                </motion.div>
                <h3 className="text-lg font-bold text-slate-800">Đề xuất đề tài thành công</h3>
                <p className="mt-1 text-sm text-slate-500">Đề tài đã được đưa vào kho.</p>
              </div>
            ) : (
              <>
                {/* Header */}
                <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
                  <div>
                    <h3 className="text-base font-bold text-slate-800">Đề xuất đề tài vào kho</h3>
                    <p className="text-xs text-slate-500">Tải lên phiếu đăng ký đã điền — hệ thống tự đọc &amp; kiểm tra.</p>
                  </div>
                  <button
                    type="button"
                    onClick={() => onClose()}
                    className="flex size-8 items-center justify-center rounded-lg text-slate-400 hover:bg-slate-100 hover:text-slate-600"
                  >
                    <span className="material-symbols-outlined text-[20px]">close</span>
                  </button>
                </div>

                {/* Body */}
                <div className="flex-1 space-y-5 overflow-y-auto px-6 py-5">
                  {/* 1. Pool */}
                  <div>
                    <label className="mb-1.5 block text-sm font-semibold text-slate-700">
                      Kho đề tài <span className="text-red-500">*</span>
                    </label>
                    <select
                      value={poolId}
                      onChange={(e) => handlePoolChange(e.target.value)}
                      disabled={loadingPools}
                      className="block w-full rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:opacity-60"
                    >
                      <option value="">{loadingPools ? "Đang tải kho đề tài…" : "— Chọn kho đề tài —"}</option>
                      {pools.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* 2. Register form upload */}
                  <div>
                    <label className="mb-1.5 block text-sm font-semibold text-slate-700">
                      Phiếu đăng ký (PDF / DOC / DOCX) <span className="text-red-500">*</span>
                    </label>

                    <label
                      className={`flex cursor-pointer flex-col items-center gap-2 rounded-xl border-2 border-dashed px-6 py-6 text-center transition-colors ${
                        preview
                          ? "border-emerald-300 bg-emerald-50/50"
                          : fileError
                            ? "border-red-300 bg-red-50/40"
                            : "border-slate-300 hover:border-primary/40 hover:bg-slate-50"
                      }`}
                    >
                      <span className="material-symbols-outlined text-[26px] text-slate-400">upload_file</span>
                      {file ? (
                        <span className="text-sm font-medium text-slate-700">
                          {file.name} <span className="text-slate-400">({formatFileSize(file.size)})</span>
                        </span>
                      ) : (
                        <span className="text-sm text-slate-500">
                          <span className="font-semibold text-primary">Chọn phiếu đăng ký</span> hoặc kéo thả vào đây
                        </span>
                      )}
                      <input
                        ref={fileInputRef}
                        type="file"
                        className="sr-only"
                        accept={REGISTER_FORM_ACCEPT}
                        onChange={handleFile}
                      />
                    </label>

                    {validating && (
                      <p className="mt-2 flex items-center gap-1.5 text-xs font-medium text-primary">
                        <span className="material-symbols-outlined animate-spin text-[15px]">progress_activity</span>
                        Đang quét &amp; đọc phiếu đăng ký…
                      </p>
                    )}
                    {fileError && !validating && (
                      <p className="mt-2 flex items-start gap-1.5 text-xs font-medium text-red-600">
                        <span className="material-symbols-outlined text-[15px]">error</span>
                        {fileError}
                      </p>
                    )}

                    {preview && !validating && (
                      <div className="mt-3 space-y-3 rounded-xl border border-emerald-200 bg-emerald-50/40 p-3">
                        <p className="flex items-center gap-1 text-[11px] font-bold uppercase tracking-wider text-emerald-700">
                          <span className="material-symbols-outlined text-[15px]">task_alt</span>
                          Đã đọc từ phiếu đăng ký
                        </p>

                        <PreviewGroup title="3.1 · Tên &amp; mã">
                          <PreviewInline label="Tên (EN)" value={preview.nameEn} />
                          <PreviewInline label="Tên (VI)" value={preview.nameVi} />
                          <PreviewInline label="Viết tắt" value={preview.nameAbbr} />
                        </PreviewGroup>

                        <PreviewGroup title="3.2 · Nội dung">
                          <PreviewBlock label="Mô tả" value={preview.description} />
                          <PreviewBlock label="Mục tiêu" value={preview.objectives} />
                          <PreviewChips label="Công nghệ" value={preview.technologies} />
                        </PreviewGroup>

                        {hasText(preview.expectedResults) && (
                          <PreviewGroup title="3.3 · Kết quả kỳ vọng">
                            <PreviewBlock value={preview.expectedResults} />
                          </PreviewGroup>
                        )}

                        {hasText(preview.scope) && (
                          <PreviewGroup title="3.4 · Phạm vi">
                            <PreviewBlock value={preview.scope} />
                          </PreviewGroup>
                        )}

                        <p className="border-t border-emerald-200/70 pt-2 text-[11px] font-medium text-emerald-700">
                          Khớp {preview.mentorCount} giảng viên hướng dẫn trong danh sách đã công bố.
                        </p>
                      </div>
                    )}
                  </div>

                  {/* 3. Note */}
                  <div>
                    <label className="mb-1.5 block text-sm font-semibold text-slate-700">Ghi chú (Optional)</label>
                    <p className="mb-1.5 text-xs text-slate-400">
                      Ví dụ: yêu cầu về năng lực của nhóm sinh viên muốn đăng ký đề tài này.
                    </p>
                    <div className="rich-note [&_.ql-container]:min-h-[150px] [&_.ql-editor]:min-h-[150px]">
                      <ReactQuill
                        theme="snow"
                        value={note}
                        onChange={setNote}
                        modules={{ toolbar: { container: QUILL_TOOLBAR } }}
                        formats={QUILL_FORMATS}
                        placeholder="Nhập ghi chú (nếu có)…"
                      />
                    </div>
                  </div>
                </div>

                {/* Footer */}
                <div className="flex items-center justify-end gap-3 border-t border-slate-100 px-6 py-4">
                  <button
                    type="button"
                    onClick={() => onClose()}
                    className="rounded-xl px-4 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-100"
                  >
                    Huỷ
                  </button>
                  <button
                    type="button"
                    onClick={handleSubmit}
                    disabled={!canSubmit}
                    className="inline-flex items-center gap-1.5 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    {submitting ? (
                      <>
                        <span className="material-symbols-outlined animate-spin text-[16px]">progress_activity</span>
                        Đang gửi…
                      </>
                    ) : (
                      <>
                        <span className="material-symbols-outlined text-[16px]">send</span>
                        Gửi phê duyệt
                      </>
                    )}
                  </button>
                </div>
              </>
            )}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

/** True (and narrows the type) when the parsed field actually carries text. Empty optional fields are hidden. */
function hasText(value: string | null | undefined): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

/** A titled section of the parsed-form preview, mirroring the register form's own 3.1–3.4 headings. */
function PreviewGroup({ title, children }: Readonly<{ title: string; children: React.ReactNode }>) {
  return (
    <div className="space-y-1.5">
      <p className="text-[10px] font-bold uppercase tracking-wider text-emerald-600/80">{title}</p>
      {children}
    </div>
  );
}

/** Short single-value field (title / abbreviation): label + value on one line, shown in full — never clamped. */
function PreviewInline({ label, value }: Readonly<{ label: string; value: string | null }>) {
  if (!hasText(value)) return null;
  return (
    <div className="flex gap-2 text-[12px]">
      <span className="w-16 shrink-0 font-semibold text-slate-500">{label}</span>
      <span className="min-w-0 flex-1 break-words text-slate-700">{value}</span>
    </div>
  );
}

/**
 * A long, possibly multi-paragraph field (description / objectives / expected results / scope). Shown in
 * full with its line breaks preserved; a bounded height with its own scrollbar keeps a 4000-char field
 * from blowing up the modal — the content scrolls, it is never cut with an ellipsis.
 */
function PreviewBlock({ label, value }: Readonly<{ label?: string; value: string | null }>) {
  if (!hasText(value)) return null;
  return (
    <div className="text-[12px]">
      {label && <span className="mb-0.5 block font-semibold text-slate-500">{label}</span>}
      <div className="max-h-56 overflow-y-auto whitespace-pre-line break-words rounded-lg border border-emerald-100 bg-white/70 px-2.5 py-2 leading-relaxed text-slate-700">
        {value}
      </div>
    </div>
  );
}

/** The comma-joined technology list rendered as chips so every entry is visible at a glance. */
function PreviewChips({ label, value }: Readonly<{ label: string; value: string | null }>) {
  if (!hasText(value)) return null;
  const items = value.split(",").map((t) => t.trim()).filter(Boolean);
  if (items.length === 0) return null;
  return (
    <div className="text-[12px]">
      <span className="mb-1 block font-semibold text-slate-500">{label}</span>
      <div className="flex flex-wrap gap-1">
        {items.map((tech, i) => (
          <span
            key={`${tech}-${i}`}
            className="rounded-md border border-emerald-200 bg-white px-1.5 py-0.5 text-[11px] text-slate-600"
          >
            {tech}
          </span>
        ))}
      </div>
    </div>
  );
}
