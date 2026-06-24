import { useState, useRef, useEffect, useCallback } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { semesterService } from "@/lib";
import { validatePhases, findCurrentSemester } from "@/lib";
import { SemesterDto, ImportRosterResponse } from "@/types";
import { RosterUploadDropzone } from "./RosterUploadDropzone";

interface CreateSemesterModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated?: () => void;
  /** Existing semesters — used to locate the current/active semester for phase validation. */
  semesters?: SemesterDto[];
}

interface PhaseInput {
  name: string;
  type: string;
  startDate: string;
  endDate: string;
}

const PHASES_TEMPLATE = [
  { label: "Đăng ký", name: "Đăng ký đề tài", type: "Registration", color: "text-primary" },
  { label: "Thẩm định", name: "Thẩm định đề tài", type: "Evaluation", color: "text-orange-600" },
  { label: "Thực hiện", name: "Thực hiện đồ án", type: "Implementation", color: "text-emerald-600" },
  { label: "Bảo vệ", name: "Bảo vệ đồ án", type: "Defense", color: "text-purple-600" },
];

const DRAFT_KEY = "semester_draft";

function loadDraft() {
  try {
    const raw = localStorage.getItem(DRAFT_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function CreateSemesterModal({ isOpen, onClose, onCreated, semesters }: CreateSemesterModalProps) {
  const draft = loadDraft();
  const navigate = useNavigate();

  const [name, setName] = useState(draft?.name ?? "");
  const [code, setCode] = useState(draft?.code ?? "");
  const [startDate, setStartDate] = useState(draft?.startDate ?? "");
  const [endDate, setEndDate] = useState(draft?.endDate ?? "");
  const [description, setDescription] = useState(draft?.description ?? "");
  const [phases, setPhases] = useState<PhaseInput[]>(
    draft?.phases ?? PHASES_TEMPLATE.map((p) => ({ name: p.name, type: p.type, startDate: "", endDate: "" })),
  );
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  // Optional initial roster files (full management lives on the roster page).
  const [studentFile, setStudentFile] = useState<File | null>(null);
  const [mentorFile, setMentorFile] = useState<File | null>(null);
  const [studentResult, setStudentResult] = useState<ImportRosterResponse | null>(null);
  const [mentorResult, setMentorResult] = useState<ImportRosterResponse | null>(null);
  const [createdId, setCreatedId] = useState<number | null>(null);

  const contentRef = useRef<HTMLDivElement>(null);

  // Auto-save draft to localStorage on every change (files are not persisted).
  const saveDraft = useCallback(() => {
    const data = { name, code, startDate, endDate, description, phases };
    localStorage.setItem(DRAFT_KEY, JSON.stringify(data));
  }, [name, code, startDate, endDate, description, phases]);

  useEffect(() => {
    saveDraft();
  }, [saveDraft]);

  const showError = (msg: string) => {
    setError(msg);
    contentRef.current?.scrollTo({ top: 0, behavior: "smooth" });
  };

  /** Validates extension before accepting a roster file. */
  const acceptFile = (setter: (f: File | null) => void) => (file: File) => {
    const ext = file.name.split(".").pop()?.toLowerCase();
    if (!["csv", "xlsx", "xls"].includes(ext ?? "")) {
      showError("Chỉ chấp nhận file .csv, .xlsx hoặc .xls");
      return;
    }
    setError(null);
    setter(file);
  };

  const updatePhase = (index: number, field: "startDate" | "endDate", value: string) => {
    setPhases((prev) => prev.map((p, i) => (i === index ? { ...p, [field]: value } : p)));
  };

  const deriveAcademicYearStart = (): number => {
    if (startDate) return new Date(startDate).getFullYear();
    return new Date().getFullYear();
  };

  const clearDraft = () => localStorage.removeItem(DRAFT_KEY);

  const resetForm = () => {
    setName("");
    setCode("");
    setStartDate("");
    setEndDate("");
    setDescription("");
    setPhases(PHASES_TEMPLATE.map((p) => ({ name: p.name, type: p.type, startDate: "", endDate: "" })));
    setError(null);
    setSuccess(false);
    setStudentFile(null);
    setMentorFile(null);
    setStudentResult(null);
    setMentorResult(null);
    setCreatedId(null);
    clearDraft();
  };

  // Click outside or X button → close. Reset only once the create succeeded.
  const handleDismiss = () => {
    setError(null);
    if (success) resetForm();
    onClose();
  };

  const handleClearDraft = () => resetForm();

  const goToRoster = () => {
    if (createdId == null) return;
    const id = createdId;
    resetForm();
    onClose();
    navigate(`/admin/semesters/${id}/roster`);
  };

  const handleSubmit = async () => {
    if (!name.trim()) return showError("Vui lòng nhập tên kỳ học.");
    if (!code.trim()) return showError("Vui lòng nhập mã kỳ học.");
    if (!startDate) return showError("Vui lòng chọn ngày bắt đầu.");
    if (!endDate) return showError("Vui lòng chọn ngày kết thúc.");

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const semStart = new Date(startDate);
    const semEnd = new Date(endDate);

    if (semStart < today) return showError("Ngày bắt đầu kỳ học không được nhỏ hơn ngày hiện tại.");
    if (semEnd < semStart) return showError("Ngày kết thúc phải sau ngày bắt đầu.");

    const validPhases = phases.filter((p) => p.startDate && p.endDate);
    const labelFor = (p: PhaseInput) => PHASES_TEMPLATE.find((t) => t.type === p.type)?.label ?? p.name;

    // Create-only: timeline phases must not be in the past.
    for (const p of validPhases) {
      if (new Date(p.startDate) < today) {
        return showError(`Giai đoạn ${labelFor(p)}: Ngày bắt đầu không được nhỏ hơn ngày hiện tại.`);
      }
    }

    // Type-aware validation (shared with the Edit modal).
    const current = findCurrentSemester(semesters, today);
    const phaseError = validatePhases(
      validPhases.map((p) => ({ type: p.type, startDate: p.startDate, endDate: p.endDate, label: labelFor(p) })),
      semStart,
      semEnd,
      current,
    );
    if (phaseError) return showError(phaseError);

    setIsSubmitting(true);
    setError(null);

    try {
      const res = await semesterService.createSemester({
        name: name.trim(),
        code: code.trim(),
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
        academicYearStart: deriveAcademicYearStart(),
        description: description.trim() || null,
        phases: validPhases.map((p) => ({
          name: p.name,
          type: p.type,
          startDate: new Date(p.startDate).toISOString(),
          endDate: new Date(p.endDate).toISOString(),
        })),
      });

      setCreatedId(res.id);
      setSuccess(true);
      onCreated?.();
      clearDraft();

      // Optional initial imports — surface their result instead of swallowing it.
      if (studentFile) {
        try {
          setStudentResult(await semesterService.importEligibleStudents(res.id, studentFile));
        } catch (e) {
          showError("Tải danh sách sinh viên thất bại: " + (e instanceof Error ? e.message : "Lỗi không xác định"));
        }
      }
      if (mentorFile) {
        try {
          setMentorResult(await semesterService.importEligibleMentors(res.id, mentorFile));
        } catch (e) {
          showError("Tải danh sách giảng viên thất bại: " + (e instanceof Error ? e.message : "Lỗi không xác định"));
        }
      }

      // Plain create (no files) → auto-close. With files, keep open so the admin reads the results.
      if (!studentFile && !mentorFile) {
        setTimeout(() => {
          resetForm();
          onClose();
        }, 1500);
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : "Có lỗi xảy ra khi tạo kỳ học.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const hasDraft = name || code || startDate || endDate || phases.some((p) => p.startDate || p.endDate);

  const renderResult = (title: string, result: ImportRosterResponse | null) => {
    if (!result) return null;
    // Gom các dòng lỗi theo lý do.
    const grouped = new Map<string, string[]>();
    for (const it of result.issues ?? []) {
      const arr = grouped.get(it.reason) ?? [];
      arr.push(it.code);
      grouped.set(it.reason, arr);
    }
    return (
      <div className="p-3 border rounded-md bg-slate-50 border-slate-200">
        <p className="text-sm font-semibold text-slate-700">
          {title}: đã nhập <span className="text-green-700">{result.successfullyImported}</span>/
          {result.totalProcessed}
        </p>
        {[...grouped.entries()].map(([reason, codes]) => (
          <p key={reason} className="mt-1 text-xs text-amber-700">
            <span className="font-semibold">{codes.length} mã</span> — {reason}: {codes.join(", ")}
          </p>
        ))}
      </div>
    );
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm"
          onClick={handleDismiss}
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            transition={{ type: "spring", damping: 25, stiffness: 300 }}
            onClick={(e) => e.stopPropagation()}
            className="bg-white w-full max-w-4xl max-h-[90vh] rounded-xl shadow-2xl overflow-hidden flex flex-col"
          >
            {/* Header */}
            <div className="flex items-center justify-between px-6 py-4 bg-white border-b border-slate-100 shrink-0">
              <div>
                <h2 className="text-xl font-bold text-slate-800">Tạo Kỳ Học Mới</h2>
                <p className="text-sm text-slate-500">Thiết lập thời gian và (tùy chọn) danh sách tham gia</p>
              </div>
              <div className="flex items-center gap-2">
                {hasDraft && !success && (
                  <button
                    onClick={handleClearDraft}
                    className="px-2 py-1 text-xs text-red-500 transition-colors rounded hover:text-red-700 hover:bg-red-50"
                    title="Xóa toàn bộ dữ liệu đã nhập"
                  >
                    Xóa bản nháp
                  </button>
                )}
                <button
                  onClick={handleDismiss}
                  className="p-1 transition-colors rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100"
                >
                  <span className="material-symbols-outlined">close</span>
                </button>
              </div>
            </div>

            {/* Content */}
            <div ref={contentRef} className="flex-1 p-6 space-y-8 overflow-y-auto">
              {error && (
                <div className="flex items-start gap-3 p-3 border border-red-200 rounded-md bg-red-50">
                  <span className="material-symbols-outlined text-red-600 text-[20px] mt-0.5">error</span>
                  <p className="text-sm text-red-800">{error}</p>
                </div>
              )}
              {success && (
                <div className="flex items-start gap-3 p-3 border border-green-200 rounded-md bg-green-50">
                  <span className="material-symbols-outlined text-green-600 text-[20px] mt-0.5">check_circle</span>
                  <div className="flex-1">
                    <p className="text-sm font-semibold text-green-800">Tạo kỳ học thành công!</p>
                    <p className="mt-0.5 text-xs text-green-700">
                      Bạn có thể nhập bổ sung, chỉnh sửa ngành của giảng viên và công bố danh sách ở trang quản lý.
                    </p>
                    <div className="mt-2 space-y-2">
                      {renderResult("Sinh viên", studentResult)}
                      {renderResult("Giảng viên", mentorResult)}
                    </div>
                  </div>
                </div>
              )}

              {/* Section 1: General Info */}
              <motion.section initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1 }}>
                <div className="flex items-center gap-2 mb-4">
                  <span className="flex items-center justify-center w-8 h-8 text-sm font-bold rounded bg-blue-50 text-primary">
                    1
                  </span>
                  <h3 className="font-bold text-slate-700">Thông tin chung</h3>
                </div>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">
                      Tên kỳ học <span className="text-red-500">*</span>
                    </label>
                    <input
                      className="w-full px-3 py-2 text-sm transition-all border rounded-md outline-none border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary"
                      placeholder="VD: Summer 2024"
                      type="text"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">
                      Mã kỳ học <span className="text-red-500">*</span>
                    </label>
                    <input
                      className="w-full px-3 py-2 text-sm transition-all border rounded-md outline-none border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary"
                      placeholder="VD: SU26"
                      type="text"
                      value={code}
                      onChange={(e) => setCode(e.target.value)}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">
                      Ngày bắt đầu <span className="text-red-500">*</span>
                    </label>
                    <input
                      className="w-full px-3 py-2 text-sm transition-all border rounded-md outline-none border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary"
                      type="date"
                      value={startDate}
                      onChange={(e) => setStartDate(e.target.value)}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">
                      Ngày kết thúc <span className="text-red-500">*</span>
                    </label>
                    <input
                      className="w-full px-3 py-2 text-sm transition-all border rounded-md outline-none border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary"
                      type="date"
                      value={endDate}
                      onChange={(e) => setEndDate(e.target.value)}
                    />
                  </div>
                </div>
              </motion.section>

              {/* Section 2: Timeline */}
              <motion.section initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.2 }}>
                <div className="flex items-center gap-2 mb-4">
                  <span className="flex items-center justify-center w-8 h-8 text-sm font-bold rounded bg-blue-50 text-primary">
                    2
                  </span>
                  <h3 className="font-bold text-slate-700">Thiết lập giai đoạn (Timeline)</h3>
                </div>
                <div className="p-4 border rounded-lg bg-slate-50 border-slate-100">
                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
                    {PHASES_TEMPLATE.map((phase, index) => (
                      <div
                        key={phase.label}
                        className="p-3 transition-shadow bg-white border rounded shadow-sm border-slate-200 hover:shadow-md"
                      >
                        <p className={`text-xs font-bold ${phase.color} uppercase mb-2`}>{phase.label}</p>
                        <input
                          className="w-full p-0 text-sm bg-transparent border-none focus:ring-0"
                          type="date"
                          value={phases[index].startDate}
                          onChange={(e) => updatePhase(index, "startDate", e.target.value)}
                        />
                        <div className="h-px my-1 bg-slate-100" />
                        <input
                          className="w-full p-0 text-sm bg-transparent border-none focus:ring-0"
                          type="date"
                          value={phases[index].endDate}
                          onChange={(e) => updatePhase(index, "endDate", e.target.value)}
                        />
                      </div>
                    ))}
                  </div>
                </div>
              </motion.section>

              {/* Section 3: Optional initial roster */}
              <motion.section initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.3 }}>
                <div className="flex items-center gap-2 mb-2">
                  <span className="flex items-center justify-center w-8 h-8 text-sm font-bold rounded bg-blue-50 text-primary">
                    3
                  </span>
                  <h3 className="font-bold text-slate-700">Danh sách tham gia</h3>
                  <span className="bg-slate-200 text-slate-600 text-[10px] px-1.5 py-0.5 rounded font-bold uppercase tracking-tight">
                    Tùy chọn
                  </span>
                </div>
                <p className="mb-4 text-xs text-slate-500">
                  Tải lên danh sách ban đầu nếu muốn. Bạn có thể nhập bổ sung, sửa ngành giảng viên và công bố (gửi thông
                  báo / email) sau ở trang <strong>Quản lý danh sách</strong>.
                </p>
                <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
                  <RosterUploadDropzone
                    label="Tải danh sách sinh viên"
                    hint="Kéo thả hoặc bấm để chọn file .csv / .xlsx chứa MSSV đủ điều kiện."
                    file={studentFile}
                    onSelect={acceptFile(setStudentFile)}
                    onClear={() => setStudentFile(null)}
                    templateHref="/templates/danh_sach_sinh_vien_mau.csv"
                    disabled={success}
                  />
                  <RosterUploadDropzone
                    label="Tải danh sách giảng viên"
                    hint="File .csv / .xlsx chứa mã giảng viên và ngành hướng dẫn."
                    file={mentorFile}
                    onSelect={acceptFile(setMentorFile)}
                    onClear={() => setMentorFile(null)}
                    disabled={success}
                  />
                </div>
              </motion.section>
            </div>

            {/* Footer */}
            <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-slate-100 bg-slate-50/50 shrink-0">
              {success && createdId != null ? (
                <>
                  <button
                    onClick={handleDismiss}
                    className="px-4 py-2 text-sm font-semibold transition-colors text-slate-600 hover:text-slate-800"
                  >
                    Đóng
                  </button>
                  <button
                    onClick={goToRoster}
                    className="flex items-center gap-2 px-6 py-2 text-sm font-bold text-white transition-all rounded-md shadow-lg bg-primary shadow-primary/20 hover:bg-primary/90"
                  >
                    <span className="material-symbols-outlined text-[18px]">group</span>{" "}
                    Quản lý danh sách
                  </button>
                </>
              ) : (
                <>
                  <button
                    onClick={handleDismiss}
                    disabled={isSubmitting}
                    className="px-4 py-2 text-sm font-semibold transition-colors text-slate-600 hover:text-slate-800 disabled:opacity-50"
                  >
                    Hủy bỏ
                  </button>
                  <button
                    onClick={handleSubmit}
                    disabled={isSubmitting}
                    className="flex items-center gap-2 px-6 py-2 text-sm font-bold text-white transition-all rounded-md shadow-lg bg-primary shadow-primary/20 hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {isSubmitting ? (
                      <>
                        <span className="material-symbols-outlined animate-spin text-[18px]">progress_activity</span>{" "}
                        Đang tạo...
                      </>
                    ) : (
                      "Khởi tạo kỳ học"
                    )}
                  </button>
                </>
              )}
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
