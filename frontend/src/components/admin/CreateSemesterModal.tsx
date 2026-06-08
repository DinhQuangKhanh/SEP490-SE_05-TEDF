import { useState, useRef, useEffect, useCallback } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { semesterService } from "@/lib";
import { majorService } from "@/lib";
import { validatePhases, findCurrentSemester } from "@/lib";
import { SemesterDto } from "@/types";

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
  const [uploadedFile, setUploadedFile] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);

  // Fetch Majors
  const [majors, setMajors] = useState<{ id: number; name: string }[]>([]);

  useEffect(() => {
    majorService
      .getMajors()
      .then((res) => setMajors(res))
      .catch((err) => console.error("Could not fetch majors", err));
  }, []);

  // Generate cohorts
  const currentYear = new Date().getFullYear();
  const cohorts = Array.from({ length: 6 }, (_, i) => currentYear - 4 + i).map((year) => ({
    label: `K${year}`,
    value: year,
  }));

  // Auto-save draft to localStorage on every change
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

  const handleFileSelect = (file: File | undefined) => {
    if (!file) return;
    const ext = file.name.split(".").pop()?.toLowerCase();
    if (!["csv", "xlsx", "xls"].includes(ext ?? "")) {
      showError("Chỉ chấp nhận file .csv, .xlsx hoặc .xls");
      return;
    }
    setUploadedFile(file);
    setError(null);
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
    setUploadedFile(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
    clearDraft();
  };

  // Click outside or X button → just close, keep data in localStorage
  const handleDismiss = () => {
    setError(null);
    onClose();
  };

  // Explicit clear button
  const handleClearDraft = () => {
    resetForm();
  };

  const handleSubmit = async () => {
    if (!name.trim()) {
      showError("Vui lòng nhập tên kỳ học.");
      return;
    }
    if (!code.trim()) {
      showError("Vui lòng nhập mã kỳ học.");
      return;
    }
    if (!startDate) {
      showError("Vui lòng chọn ngày bắt đầu.");
      return;
    }
    if (!endDate) {
      showError("Vui lòng chọn ngày kết thúc.");
      return;
    }

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const semStart = new Date(startDate);
    const semEnd = new Date(endDate);
    console.log(semStart, semEnd, today);

    if (semStart < today) {
      showError("Ngày bắt đầu kỳ học không được nhỏ hơn ngày hiện tại.");
      return;
    }
    if (semEnd < semStart) {
      showError("Ngày kết thúc phải sau ngày bắt đầu.");
      return;
    }

    const validPhases = phases.filter((p) => p.startDate && p.endDate);
    const labelFor = (p: PhaseInput) => PHASES_TEMPLATE.find((t) => t.type === p.type)?.label ?? p.name;

    // Create-only: timeline phases must not be in the past.
    for (const p of validPhases) {
      if (new Date(p.startDate) < today) {
        showError(`Giai đoạn ${labelFor(p)}: Ngày bắt đầu không được nhỏ hơn ngày hiện tại.`);
        return;
      }
    }

    // Type-aware validation (shared with the Edit modal): Registration/Evaluation fall within the
    // current semester; Implementation/Defense fall within the new semester.
    const current = findCurrentSemester(semesters, today);
    const phaseError = validatePhases(
      validPhases.map((p) => ({ type: p.type, startDate: p.startDate, endDate: p.endDate, label: labelFor(p) })),
      semStart,
      semEnd,
      current,
    );
    if (phaseError) {
      showError(phaseError);
      return;
    }

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

      // Upload eligible students if selected
      if (uploadedFile) {
        try {
          await semesterService.importEligibleStudents(res.id, uploadedFile);
        } catch (uploadErr) {
          showError(
            "Tạo học kỳ thành công nhưng có lỗi khi tải danh sách sinh viên: " +
              (uploadErr instanceof Error ? uploadErr.message : "Unknown error"),
          );
          setIsSubmitting(false);
          return; // Stop further success flow to let user see error
        }
      }

      setSuccess(true);
      onCreated?.();
      clearDraft();
      setTimeout(() => {
        resetForm();
        onClose();
      }, 1500);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Có lỗi xảy ra khi tạo kỳ học.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const hasDraft = name || code || startDate || endDate || phases.some((p) => p.startDate || p.endDate);

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
                <p className="text-sm text-slate-500">Thiết lập thời gian và đối tượng tham gia đồ án</p>
              </div>
              <div className="flex items-center gap-2">
                {hasDraft && (
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
              {/* Error / Success */}
              {error && (
                <div className="flex items-start gap-3 p-3 border border-red-200 rounded-md bg-red-50">
                  <span className="material-symbols-outlined text-red-600 text-[20px] mt-0.5">error</span>
                  <p className="text-sm text-red-800">{error}</p>
                </div>
              )}
              {success && (
                <div className="flex items-start gap-3 p-3 border border-green-200 rounded-md bg-green-50">
                  <span className="material-symbols-outlined text-green-600 text-[20px] mt-0.5">check_circle</span>
                  <p className="text-sm font-semibold text-green-800">Tạo kỳ học thành công!</p>
                </div>
              )}

              {/* Section 1: General Info */}
              <motion.section
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.1 }}
              >
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
              <motion.section
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.2 }}
              >
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
                          placeholder="Từ ngày"
                          value={phases[index].startDate}
                          onChange={(e) => updatePhase(index, "startDate", e.target.value)}
                        />
                        <div className="h-px my-1 bg-slate-100" />
                        <input
                          className="w-full p-0 text-sm bg-transparent border-none focus:ring-0"
                          type="date"
                          placeholder="Đến ngày"
                          value={phases[index].endDate}
                          onChange={(e) => updatePhase(index, "endDate", e.target.value)}
                        />
                      </div>
                    ))}
                  </div>
                </div>
              </motion.section>

              {/* Section 3: Participants */}
              <motion.section
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.3 }}
              >
                <div className="flex items-center gap-2 mb-4">
                  <span className="flex items-center justify-center w-8 h-8 text-sm font-bold rounded bg-blue-50 text-primary">
                    3
                  </span>
                  <div className="flex items-center gap-2">
                    <h3 className="font-bold text-slate-700">Đối tượng tham gia</h3>
                    <span className="bg-primary text-white text-[10px] px-1.5 py-0.5 rounded font-bold uppercase tracking-tight">
                      Quan trọng
                    </span>
                  </div>
                </div>
                <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
                  {/* Upload Excel */}
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept=".csv,.xlsx,.xls"
                    className="hidden"
                    onChange={(e) => handleFileSelect(e.target.files?.[0])}
                  />
                  <div
                    className={`border-2 border-dashed rounded-xl p-6 transition-colors group cursor-pointer ${
                      uploadedFile ? "border-green-300 bg-green-50/30" : "border-slate-200 hover:border-primary/50"
                    }`}
                    onClick={() => !uploadedFile && fileInputRef.current?.click()}
                    onDragOver={(e) => {
                      e.preventDefault();
                      e.stopPropagation();
                    }}
                    onDrop={(e) => {
                      e.preventDefault();
                      e.stopPropagation();
                      handleFileSelect(e.dataTransfer.files?.[0]);
                    }}
                  >
                    <div className="flex flex-col items-center text-center">
                      {uploadedFile ? (
                        <>
                          <div className="flex items-center justify-center w-12 h-12 mb-3 text-green-600 bg-green-100 rounded-full">
                            <span className="text-3xl material-symbols-outlined">check_circle</span>
                          </div>
                          <h4 className="mb-1 font-bold text-slate-800">Đã tải lên</h4>
                          <p className="mb-1 text-xs font-medium text-slate-600">{uploadedFile.name}</p>
                          <p className="text-[10px] text-slate-400 mb-3">{(uploadedFile.size / 1024).toFixed(1)} KB</p>
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              setUploadedFile(null);
                              if (fileInputRef.current) fileInputRef.current.value = "";
                            }}
                            className="py-1.5 px-4 bg-red-50 border border-red-200 rounded-md text-xs font-semibold text-red-600 hover:bg-red-100 transition-colors"
                          >
                            Xóa tệp tin
                          </button>
                        </>
                      ) : (
                        <>
                          <div className="flex items-center justify-center w-12 h-12 mb-3 transition-colors rounded-full bg-slate-100 text-slate-400 group-hover:bg-primary/10 group-hover:text-primary">
                            <span className="text-3xl material-symbols-outlined">upload_file</span>
                          </div>
                          <h4 className="mb-1 font-bold text-slate-800">Tải danh sách Excel</h4>
                          <p className="px-4 mb-4 text-xs text-slate-500">
                            Kéo thả hoặc bấm để tải lên file .csv / .xlsx chứa danh sách MSSV đủ điều kiện.
                          </p>
                          <button
                            type="button"
                            onClick={(e) => {
                              e.stopPropagation();
                              fileInputRef.current?.click();
                            }}
                            className="w-full px-4 py-2 text-sm font-semibold transition-colors bg-white border rounded-md border-slate-300 text-slate-700 hover:bg-slate-50"
                          >
                            Chọn tệp tin
                          </button>
                        </>
                      )}
                      <a
                        className="mt-3 text-[11px] text-primary hover:underline flex items-center gap-1"
                        href="/templates/danh_sach_sinh_vien_mau.csv"
                        download
                      >
                        <span className="material-symbols-outlined text-[14px]">download</span> Tải file mẫu (.csv)
                      </a>
                    </div>
                  </div>

                  {/* Filter */}
                  <div className="p-6 border border-slate-200 rounded-xl bg-slate-50/50">
                    <h4 className="flex items-center gap-2 mb-3 font-bold text-slate-800">
                      <span className="material-symbols-outlined text-[20px] text-primary">filter_alt</span>
                      Lọc theo điều kiện
                    </h4>
                    <div className="space-y-4">
                      <div>
                        <label className="block text-xs font-semibold text-slate-500 uppercase mb-1.5">
                          Khóa &amp; Ngành
                        </label>
                        <div className="grid grid-cols-2 gap-2">
                          <select
                            className="px-2 py-2 text-xs border rounded-md outline-none border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary"
                            value={startDate ? deriveAcademicYearStart() : currentYear}
                            disabled
                          >
                            {cohorts.map((c) => (
                              <option key={c.value} value={c.value}>
                                {c.label}
                              </option>
                            ))}
                          </select>
                          <select className="px-2 py-2 text-xs border rounded-md outline-none border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary">
                            <option value="">Tất cả Ngành</option>
                            {majors.map((m) => (
                              <option key={m.id} value={m.id}>
                                {m.name}
                              </option>
                            ))}
                          </select>
                        </div>
                      </div>
                      <div>
                        <label className="block text-xs font-semibold text-slate-500 uppercase mb-1.5">
                          Điểm tích lũy (GPA) tối thiểu
                        </label>
                        <div className="flex items-center gap-3">
                          <input
                            className="w-full px-2 py-2 text-xs border rounded-md outline-none border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary"
                            placeholder="VD: 2.5"
                            step="0.1"
                            type="number"
                          />
                          <span className="text-xs font-medium text-slate-400 whitespace-nowrap">/ 4.0</span>
                        </div>
                      </div>
                      <div className="pt-2">
                        <button className="w-full px-4 py-2 text-xs font-bold transition-all border rounded-md bg-primary/10 text-primary border-primary/20 hover:bg-primary/20">
                          Xem trước danh sách (245 SV)
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Info Note */}
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  transition={{ delay: 0.4 }}
                  className="flex items-start gap-3 p-3 mt-4 border border-blue-100 rounded-md bg-blue-50"
                >
                  <span className="material-symbols-outlined text-blue-600 text-[20px] mt-0.5">verified</span>
                  <p className="text-xs leading-relaxed text-blue-800">
                    <strong>Lưu ý:</strong> Sau khi xác nhận, hệ thống sẽ tự động gắn thẻ
                    <span className="px-1.5 py-0.5 bg-blue-100 border border-blue-200 rounded text-[10px] font-bold mx-1">
                      ĐỦ ĐIỀU KIỆN LÀM ĐỒ ÁN
                    </span>
                    cho các sinh viên thuộc danh sách trên. Chỉ những SV này mới có quyền đăng ký đề tài trong kỳ học.
                  </p>
                </motion.div>
              </motion.section>
            </div>

            {/* Footer */}
            <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-slate-100 bg-slate-50/50 shrink-0">
              <button
                onClick={handleDismiss}
                disabled={isSubmitting}
                className="px-4 py-2 text-sm font-semibold transition-colors text-slate-600 hover:text-slate-800 disabled:opacity-50"
              >
                Hủy bỏ
              </button>
              <button
                onClick={handleSubmit}
                disabled={isSubmitting || success}
                className="flex items-center gap-2 px-6 py-2 text-sm font-bold text-white transition-all rounded-md shadow-lg bg-primary shadow-primary/20 hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isSubmitting ? (
                  <>
                    <span className="material-symbols-outlined animate-spin text-[18px]">progress_activity</span>Đang
                    tạo...
                  </>
                ) : (
                  "Khởi tạo kỳ học"
                )}
              </button>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
