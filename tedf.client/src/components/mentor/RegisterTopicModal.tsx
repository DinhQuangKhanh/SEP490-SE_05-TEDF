import { useEffect, useRef, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { apiClient } from "@/lib/apiClient";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { validateFiles, formatFileSize, ACCEPTED_TYPES, MAX_ATTACHMENTS } from "@/lib/fileUploadUtils";

// ── Types ───────────────────────────────────────────────────────────────────

interface TopicPoolOption {
  id: string;
  code: string;
  name: string;
  statusName: string;
}

interface RegisterTopicModalProps {
  isOpen: boolean;
  onClose: (success?: boolean) => void;
}

interface FormData {
  poolId: string;
  nameVi: string;
  nameEn: string;
  nameAbbr: string;
  description: string;
  objectives: string;
  scope: string;
  technologies: string;
  expectedResults: string;
  maxStudents: number;
}

const emptyForm: FormData = {
  poolId: "",
  nameVi: "",
  nameEn: "",
  nameAbbr: "",
  description: "",
  objectives: "",
  scope: "",
  technologies: "",
  expectedResults: "",
  maxStudents: 5,
};

const STEPS = [
  { label: "Kho đề tài", icon: "inventory_2" },
  { label: "Thông tin", icon: "info" },
  { label: "Nội dung", icon: "description" },
  { label: "Tài liệu", icon: "attachment" },
];

// ── Input classes ───────────────────────────────────────────────────────────

const inputClass =
  "block w-full border border-slate-300 px-4 py-3 rounded-xl bg-white text-sm placeholder:text-slate-400 focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all outline-none";
const textareaClass = `${inputClass} resize-none leading-relaxed`;
const selectClass = `${inputClass} text-slate-700`;
const labelClass = "block text-sm font-semibold text-slate-700 mb-1.5";

// ── Slide animation ─────────────────────────────────────────────────────────

const slideVariants = {
  enter: (dir: number) => ({ x: dir > 0 ? 80 : -80, opacity: 0 }),
  center: { x: 0, opacity: 1 },
  exit: (dir: number) => ({ x: dir > 0 ? -80 : 80, opacity: 0 }),
};

// ── Component ───────────────────────────────────────────────────────────────

export function RegisterTopicModal({ isOpen, onClose }: RegisterTopicModalProps) {
  const [step, setStep] = useState(0);
  const [dir, setDir] = useState(1);
  const [form, setForm] = useState<FormData>({ ...emptyForm });
  const [pools, setPools] = useState<TopicPoolOption[]>([]);
  const [loadingPools, setLoadingPools] = useState(false);
  const [attachments, setAttachments] = useState<File[]>([]);
  const [fileWarnings, setFileWarnings] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showSuccess, setShowSuccess] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { showError } = useSystemError();

  // Fetch pools when modal opens
  useEffect(() => {
    if (!isOpen) return;
    setLoadingPools(true);
    apiClient
      .get<TopicPoolOption[]>("/api/topic-pools")
      .then((data) => setPools(data.filter((p) => p.statusName === "Active")))
      .catch(() => showError("Không thể tải danh sách kho đề tài."))
      .finally(() => setLoadingPools(false));
  }, [isOpen]);

  // Reset on close
  useEffect(() => {
    if (!isOpen) {
      setStep(0);
      setDir(1);
      setForm({ ...emptyForm });
      setAttachments([]);
      setFileWarnings([]);
      setError(null);
      setShowSuccess(false);
    }
  }, [isOpen]);

  const set = (field: keyof FormData, val: string | number) => setForm((prev) => ({ ...prev, [field]: val }));

  const goNext = () => {
    setDir(1);
    setStep((s) => Math.min(s + 1, STEPS.length - 1));
  };
  const goPrev = () => {
    setDir(-1);
    setStep((s) => Math.max(s - 1, 0));
  };

  const canProceed = (): boolean => {
    switch (step) {
      case 0:
        return !!form.poolId;
      case 1:
        return !!form.nameVi.trim() && !!form.nameAbbr.trim();
      case 2:
        return !!form.objectives.trim() && !!form.scope.trim() && !!form.technologies.trim();
      default:
        return true;
    }
  };

  const handleFiles = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files) return;
    const incoming = Array.from(e.target.files);
    const { accepted, rejected } = validateFiles(attachments, incoming);
    if (accepted.length > 0) setAttachments((prev) => [...prev, ...accepted]);
    setFileWarnings(rejected);
    e.target.value = "";
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const incoming = Array.from(e.dataTransfer.files);
    const { accepted, rejected } = validateFiles(attachments, incoming);
    if (accepted.length > 0) setAttachments((prev) => [...prev, ...accepted]);
    setFileWarnings(rejected);
  };

  const removeFile = (idx: number) => {
    setAttachments((prev) => prev.filter((_, i) => i !== idx));
    setFileWarnings([]);
  };

  const handleSubmit = async () => {
    setSubmitting(true);
    setError(null);
    try {
      const fd = new window.FormData();
      fd.append("nameVi", form.nameVi);
      fd.append("nameEn", form.nameEn);
      fd.append("nameAbbr", form.nameAbbr);
      fd.append("description", form.description || form.objectives);
      fd.append("objectives", form.objectives);
      if (form.scope) fd.append("scope", form.scope);
      if (form.technologies) fd.append("technologies", form.technologies);
      if (form.expectedResults) fd.append("expectedResults", form.expectedResults);
      fd.append("maxStudents", form.maxStudents.toString());
      attachments.forEach((f) => fd.append("attachments", f));

      await apiClient.postForm(`/api/topic-pools/${form.poolId}/propose`, fd);
      setShowSuccess(true);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Đề xuất thất bại. Vui lòng thử lại.");
    } finally {
      setSubmitting(false);
    }
  };

  const selectedPool = pools.find((p) => p.id === form.poolId);

  return (
    <AnimatePresence>
      {isOpen && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4"
          onClick={() => onClose()}
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            transition={{ type: "spring", damping: 25, stiffness: 300 }}
            onClick={(e) => e.stopPropagation()}
            className="bg-white w-full max-w-3xl max-h-[90vh] rounded-2xl shadow-2xl overflow-hidden flex flex-col"
          >
            {/* Success overlay */}
            {showSuccess ? (
              <div className="flex flex-col items-center justify-center py-16 px-8 text-center">
                <motion.div
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  transition={{ type: "spring", damping: 15, stiffness: 200, delay: 0.1 }}
                  className="size-20 bg-emerald-100 rounded-full flex items-center justify-center mb-6"
                >
                  <motion.span
                    initial={{ opacity: 0, scale: 0 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 0.3, type: "spring", damping: 12 }}
                    className="material-symbols-outlined text-emerald-600 text-[40px]"
                  >
                    check_circle
                  </motion.span>
                </motion.div>
                <motion.h2
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.4 }}
                  className="text-xl font-bold text-slate-900 mb-3"
                >
                  Đề xuất thành công!
                </motion.h2>
                <motion.p
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.5 }}
                  className="text-sm text-slate-600 max-w-md leading-relaxed mb-8"
                >
                  Đã đề xuất thành công, chủ nhiệm bộ môn sẽ phân công người thẩm định và sớm gửi kết quả thẩm định về cho bạn.
                </motion.p>
                <motion.button
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.6 }}
                  onClick={() => onClose(true)}
                  className="px-8 py-3 rounded-xl bg-primary hover:bg-primary/90 text-white text-sm font-semibold shadow-lg shadow-blue-900/10 transition-all"
                >
                  Đóng
                </motion.button>
              </div>
            ) : (
            <>
            {/* Header with breadcrumb */}
            <div className="px-8 py-5 border-b border-slate-100 flex items-center justify-between shrink-0">
              <div>
                <h1 className="text-xl font-extrabold text-slate-900">Đăng Ký Đề Tài Mới</h1>
                <p className="text-xs text-slate-500 mt-0.5">Điền thông tin để đề xuất đề tài vào kho</p>
              </div>
              <button
                onClick={() => onClose()}
                className="text-slate-400 hover:text-slate-600 transition-colors p-1.5 hover:bg-slate-100 rounded-lg"
              >
                <span className="material-symbols-outlined">close</span>
              </button>
            </div>

            {/* Step indicator */}
            <div className="px-8 py-4 border-b border-slate-50 bg-slate-50/50 shrink-0">
              <div className="flex items-center gap-2">
                {STEPS.map((s, idx) => (
                  <div key={s.label} className="flex items-center gap-2 flex-1">
                    <div
                      className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold transition-all ${
                        idx < step
                          ? "bg-emerald-100 text-emerald-700"
                          : idx === step
                            ? "bg-primary text-white shadow-sm"
                            : "bg-slate-100 text-slate-400"
                      }`}
                    >
                      <span className="material-symbols-outlined text-[14px]">
                        {idx < step ? "check_circle" : s.icon}
                      </span>
                      <span className="hidden sm:inline">{s.label}</span>
                    </div>
                    {idx < STEPS.length - 1 && (
                      <div className={`flex-1 h-px ${idx < step ? "bg-emerald-300" : "bg-slate-200"}`} />
                    )}
                  </div>
                ))}
              </div>
            </div>

            {/* Content area */}
            <div className="flex-1 overflow-y-auto px-8 py-6 relative min-h-[320px]">
              <AnimatePresence mode="wait" custom={dir}>
                <motion.div
                  key={step}
                  custom={dir}
                  variants={slideVariants}
                  initial="enter"
                  animate="center"
                  exit="exit"
                  transition={{ duration: 0.25, ease: "easeInOut" }}
                >
                  {step === 0 && (
                    <StepPool
                      pools={pools}
                      loading={loadingPools}
                      selectedId={form.poolId}
                      onSelect={(id) => set("poolId", id)}
                    />
                  )}
                  {step === 1 && <StepBasicInfo form={form} set={set} />}
                  {step === 2 && <StepContent form={form} set={set} />}
                  {step === 3 && (
                    <StepAttachments
                      attachments={attachments}
                      warnings={fileWarnings}
                      fileInputRef={fileInputRef}
                      onFileChange={handleFiles}
                      onDrop={handleDrop}
                      onRemove={removeFile}
                      selectedPool={selectedPool}
                      form={form}
                    />
                  )}
                </motion.div>
              </AnimatePresence>
            </div>

            {/* Error */}
            {error && (
              <div className="px-8 py-2">
                <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg px-4 py-2.5 flex items-center gap-2">
                  <span className="material-symbols-outlined text-[18px]">error</span>
                  {error}
                </div>
              </div>
            )}

            {/* Footer */}
            <div className="px-8 py-5 bg-slate-50 border-t border-slate-200 flex items-center justify-between shrink-0">
              <div>
                {step > 0 && (
                  <button
                    onClick={goPrev}
                    className="px-4 py-2.5 rounded-lg text-slate-600 text-sm font-semibold hover:bg-slate-100 transition-all flex items-center gap-1"
                  >
                    <span className="material-symbols-outlined text-[18px]">arrow_back</span>
                    Quay lại
                  </button>
                )}
              </div>
              <div className="flex gap-3">
                <button
                  onClick={() => onClose()}
                  className="px-5 py-2.5 rounded-lg border border-slate-300 bg-white text-slate-700 text-sm font-semibold shadow-sm hover:bg-slate-50 transition-all"
                >
                  Hủy bỏ
                </button>
                {step < STEPS.length - 1 ? (
                  <button
                    onClick={goNext}
                    disabled={!canProceed()}
                    className="px-6 py-2.5 rounded-lg bg-primary hover:bg-primary/90 text-white text-sm font-semibold shadow-md shadow-blue-900/10 transition-all flex items-center gap-1.5 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Tiếp theo
                    <span className="material-symbols-outlined text-[18px]">arrow_forward</span>
                  </button>
                ) : (
                  <button
                    onClick={handleSubmit}
                    disabled={submitting}
                    className="px-6 py-2.5 rounded-lg bg-primary hover:bg-primary/90 text-white text-sm font-semibold shadow-md shadow-blue-900/10 transition-all flex items-center gap-2 disabled:opacity-50"
                  >
                    {submitting ? (
                      <>
                        <span className="size-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                        Đang gửi...
                      </>
                    ) : (
                      <>
                        <span className="material-symbols-outlined text-[18px]">send</span>
                        Gửi phê duyệt
                      </>
                    )}
                  </button>
                )}
              </div>
            </div>
            </>
            )}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

// ── Step 0: Pool Selection ──────────────────────────────────────────────────

function StepPool({
  pools,
  loading,
  selectedId,
  onSelect,
}: {
  pools: TopicPoolOption[];
  loading: boolean;
  selectedId: string;
  onSelect: (id: string) => void;
}) {
  if (loading) {
    return (
      <div className="space-y-3">
        <p className="text-sm text-slate-500 mb-4">Chọn kho đề tài bạn muốn đề xuất vào:</p>
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="h-16 bg-slate-100 rounded-xl animate-pulse" />
        ))}
      </div>
    );
  }

  if (pools.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <span className="material-symbols-outlined text-[48px] text-slate-200 mb-3">inventory_2</span>
        <p className="text-slate-500 font-medium">Không có kho đề tài nào đang mở</p>
        <p className="text-slate-400 text-sm mt-1">Vui lòng liên hệ quản trị viên.</p>
      </div>
    );
  }

  return (
    <div>
      <p className="text-sm text-slate-500 mb-4">Chọn kho đề tài bạn muốn đề xuất vào:</p>
      <div className="space-y-3">
        {pools.map((pool) => (
          <button
            key={pool.id}
            onClick={() => onSelect(pool.id)}
            className={`w-full text-left p-4 rounded-xl border-2 transition-all flex items-center gap-4 ${
              selectedId === pool.id
                ? "border-primary bg-blue-50/50 shadow-sm"
                : "border-slate-200 bg-white hover:border-slate-300 hover:bg-slate-50"
            }`}
          >
            <div
              className={`p-2.5 rounded-lg ${
                selectedId === pool.id ? "bg-primary text-white" : "bg-slate-100 text-slate-500"
              }`}
            >
              <span className="material-symbols-outlined text-[20px]">inventory_2</span>
            </div>
            <div className="flex-1">
              <p className="font-bold text-slate-800 text-sm">{pool.name}</p>
              <p className="text-xs text-slate-500 mt-0.5">Mã: {pool.code}</p>
            </div>
            {selectedId === pool.id && (
              <span className="material-symbols-outlined text-primary text-[22px]">check_circle</span>
            )}
          </button>
        ))}
      </div>
    </div>
  );
}

// ── Step 1: Basic Info ──────────────────────────────────────────────────────

function StepBasicInfo({ form, set }: { form: FormData; set: (f: keyof FormData, v: string | number) => void }) {
  return (
    <div className="space-y-5">
      <div>
        <label className={labelClass}>
          Tên đề tài (Tiếng Việt) <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          value={form.nameVi}
          onChange={(e) => set("nameVi", e.target.value)}
          className={inputClass}
          placeholder="Nhập tên đề tài đầy đủ bằng tiếng Việt..."
        />
      </div>
      <div>
        <label className={labelClass}>Tên đề tài (Tiếng Anh)</label>
        <input
          type="text"
          value={form.nameEn}
          onChange={(e) => set("nameEn", e.target.value)}
          className={inputClass}
          placeholder="Enter project name in English..."
        />
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <div>
          <label className={labelClass}>
            Tên viết tắt <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={form.nameAbbr}
            onChange={(e) => set("nameAbbr", e.target.value)}
            className={inputClass}
            placeholder="VD: QLBV, TMDT..."
          />
        </div>
        <div>
          <label className={labelClass}>Số lượng sinh viên tối đa</label>
          <select
            value={form.maxStudents}
            onChange={(e) => set("maxStudents", Number(e.target.value))}
            className={selectClass}
          >
            <option value={3}>3 Sinh viên</option>
            <option value={4}>4 Sinh viên (Tiêu chuẩn)</option>
            <option value={5}>5 Sinh viên</option>
          </select>
        </div>
      </div>
    </div>
  );
}

// ── Step 2: Content ─────────────────────────────────────────────────────────

function StepContent({ form, set }: { form: FormData; set: (f: keyof FormData, v: string | number) => void }) {
  return (
    <div className="space-y-5">
      <div>
        <label className={labelClass}>
          Mục tiêu đề tài <span className="text-red-500">*</span>
        </label>
        <textarea
          rows={4}
          value={form.objectives}
          onChange={(e) => set("objectives", e.target.value)}
          className={textareaClass}
          placeholder="Mô tả mục tiêu chính mà sinh viên cần đạt được..."
          maxLength={500}
        />
        <p className="text-xs text-slate-400 text-right mt-1">{form.objectives.length}/500 ký tự</p>
      </div>
      <div>
        <label className={labelClass}>
          Phạm vi <span className="text-red-500">*</span>
        </label>
        <textarea
          rows={3}
          value={form.scope}
          onChange={(e) => set("scope", e.target.value)}
          className={textareaClass}
          placeholder="Giới hạn phạm vi nghiên cứu, những gì đề tài sẽ và sẽ không bao gồm..."
        />
      </div>
      <div>
        <label className={labelClass}>
          Công nghệ sử dụng <span className="text-red-500">*</span>
        </label>
        <textarea
          rows={2}
          value={form.technologies}
          onChange={(e) => set("technologies", e.target.value)}
          className={textareaClass}
          placeholder="React, .NET, SQL Server, Docker..."
        />
      </div>
      <div>
        <label className={labelClass}>Kết quả mong đợi</label>
        <textarea
          rows={3}
          value={form.expectedResults}
          onChange={(e) => set("expectedResults", e.target.value)}
          className={textareaClass}
          placeholder="Mô tả sản phẩm hoặc kết quả cuối cùng..."
        />
      </div>
    </div>
  );
}

// ── Step 3: Attachments + Review Summary ────────────────────────────────────

function StepAttachments({
  attachments,
  warnings,
  fileInputRef,
  onFileChange,
  onDrop,
  onRemove,
  selectedPool,
  form,
}: {
  attachments: File[];
  warnings: string[];
  fileInputRef: React.RefObject<HTMLInputElement | null>;
  onFileChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onDrop: (e: React.DragEvent) => void;
  onRemove: (idx: number) => void;
  selectedPool: TopicPoolOption | undefined;
  form: FormData;
}) {
  return (
    <div className="space-y-6">
      {/* Summary */}
      <div className="bg-slate-50 rounded-xl p-4 border border-slate-100">
        <h4 className="text-sm font-bold text-slate-700 mb-2 flex items-center gap-1.5">
          <span className="material-symbols-outlined text-[16px] text-primary">summarize</span>
          Tóm tắt đề tài
        </h4>
        <div className="grid grid-cols-2 gap-2 text-xs">
          <div>
            <span className="text-slate-500">Kho:</span>{" "}
            <span className="font-medium text-slate-700">{selectedPool?.name ?? "—"}</span>
          </div>
          <div>
            <span className="text-slate-500">Tên:</span>{" "}
            <span className="font-medium text-slate-700">{form.nameVi || "—"}</span>
          </div>
          <div>
            <span className="text-slate-500">Viết tắt:</span>{" "}
            <span className="font-medium text-slate-700">{form.nameAbbr || "—"}</span>
          </div>
          <div>
            <span className="text-slate-500">SV tối đa:</span>{" "}
            <span className="font-medium text-slate-700">{form.maxStudents}</span>
          </div>
          <div className="col-span-2">
            <span className="text-slate-500">Phạm vi:</span>{" "}
            <span className="font-medium text-slate-700">{form.scope || "—"}</span>
          </div>
          <div className="col-span-2">
            <span className="text-slate-500">Công nghệ:</span>{" "}
            <span className="font-medium text-slate-700">{form.technologies || "—"}</span>
          </div>
        </div>
      </div>

      {/* Upload area */}
      <div>
        <label className={labelClass}>Tài liệu đính kèm (tùy chọn)</label>
        <div
          onDragOver={(e) => e.preventDefault()}
          onDrop={onDrop}
          onClick={() => fileInputRef.current?.click()}
          className="mt-1 flex justify-center px-6 pt-6 pb-7 border-2 border-slate-300 border-dashed rounded-xl hover:bg-slate-50 hover:border-primary/40 transition-colors cursor-pointer group"
        >
          <div className="space-y-2 text-center">
            <div className="mx-auto size-12 bg-slate-100 rounded-full flex items-center justify-center text-slate-400 group-hover:text-primary group-hover:bg-blue-50 transition-colors">
              <span className="material-symbols-outlined text-[24px]">cloud_upload</span>
            </div>
            <div className="text-sm text-slate-600">
              <span className="font-medium text-primary">Tải lên file</span> hoặc kéo thả vào đây
            </div>
            <p className="text-xs text-slate-400">
              {ACCEPTED_TYPES.map((t) => t.replace(".", "").toUpperCase()).join(", ")} &mdash; tối đa 10MB/file,{" "}
              {MAX_ATTACHMENTS} file
            </p>
          </div>
          <input
            ref={fileInputRef}
            type="file"
            className="sr-only"
            multiple
            accept={ACCEPTED_TYPES.join(",")}
            onChange={onFileChange}
          />
        </div>
      </div>

      {/* File list */}
      {attachments.length > 0 && (
        <div className="space-y-2">
          {attachments.map((file, idx) => (
            <div
              key={`${file.name}-${idx}`}
              className="flex items-center gap-3 bg-slate-50 rounded-lg px-4 py-2.5 border border-slate-100"
            >
              <span className="material-symbols-outlined text-primary text-[20px]">description</span>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-slate-700 truncate">{file.name}</p>
                <p className="text-[10px] text-slate-400">{formatFileSize(file.size)}</p>
              </div>
              <button onClick={() => onRemove(idx)} className="text-slate-400 hover:text-red-500 transition-colors p-1">
                <span className="material-symbols-outlined text-[18px]">close</span>
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Warnings */}
      {warnings.length > 0 && (
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 space-y-1">
          {warnings.map((w, i) => (
            <p key={i} className="text-xs text-amber-700 flex items-start gap-1.5">
              <span className="material-symbols-outlined text-[14px] mt-0.5">warning</span>
              {w}
            </p>
          ))}
        </div>
      )}
    </div>
  );
}
