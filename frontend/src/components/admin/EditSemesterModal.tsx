import { useState, useRef, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { semesterService } from "@/lib";
import { SemesterDto } from "@/types";
import { findCurrentSemester, validatePhases } from "@/lib";

interface EditSemesterModalProps {
  isOpen: boolean;
  onClose: () => void;
  onUpdated?: () => void;
  initialData: SemesterDto | null;
  /** Existing semesters — used to locate the current/active semester for phase validation. */
  semesters?: SemesterDto[];
}

interface PhaseInput {
  id: number;
  name: string;
  type: string;
  startDate: string;
  endDate: string;
}

// The defense phase was dropped from the process, so it is no longer offered for editing. A
// semester created before that still has the row: it stays untouched and is simply not shown.
const PHASES_TEMPLATE = [
  { label: "1. Đăng ký", name: "Đăng ký đề tài", type: "Registration", color: "text-primary" },
  { label: "2. Thẩm định", name: "Thẩm định đề tài", type: "Evaluation", color: "text-orange-600" },
  { label: "3. Thực hiện", name: "Thực hiện đồ án", type: "Implementation", color: "text-emerald-600" },
];

export function EditSemesterModal({ isOpen, onClose, onUpdated, initialData, semesters }: EditSemesterModalProps) {
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [description, setDescription] = useState("");
  const [phases, setPhases] = useState<PhaseInput[]>([]);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const contentRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (initialData && isOpen) {
      setName(initialData.name);
      setStartDate(initialData.startDate.split("T")[0]);
      setEndDate(initialData.endDate.split("T")[0]);
      setDescription(initialData.description || "");
      setPhases(
        initialData.phases
          .slice()
          .sort((a, b) => a.order - b.order)
          .map((p) => ({
            id: p.id,
            name: p.name,
            type: p.type,
            startDate: p.startDate.split("T")[0],
            endDate: p.endDate.split("T")[0],
          })),
      );
      setError(null);
      setSuccess(false);
    }
  }, [initialData, isOpen]);

  const showError = (msg: string) => {
    setError(msg);
    contentRef.current?.scrollTo({ top: 0, behavior: "smooth" });
  };

  const handleDismiss = () => {
    setError(null);
    onClose();
  };

  const updatePhase = (index: number, field: "startDate" | "endDate", value: string) => {
    setPhases((prev) => prev.map((p, i) => (i === index ? { ...p, [field]: value } : p)));
  };

  const goToRoster = () => {
    if (!initialData) return;
    const id = initialData.id;
    onClose();
    navigate(`/admin/semesters/${id}/roster`);
  };

  const handleSubmit = async () => {
    if (!initialData) return;

    if (!name.trim()) return showError("Vui lòng nhập tên kỳ học.");
    if (!startDate) return showError("Vui lòng chọn ngày bắt đầu.");
    if (!endDate) return showError("Vui lòng chọn ngày kết thúc.");

    const semStart = new Date(startDate);
    const semEnd = new Date(endDate);
    if (semEnd <= semStart) return showError("Ngày kết thúc phải sau ngày bắt đầu.");

    // Validate phases (shared with the Create modal): Registration/Evaluation fall within the
    // current semester; Implementation falls within this semester.
    const validPhases = phases.filter((p) => p.startDate && p.endDate);
    const labelFor = (p: PhaseInput) => PHASES_TEMPLATE.find((t) => t.type === p.type)?.label ?? p.name;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
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
      await semesterService.updateSemester(initialData.id, {
        id: initialData.id,
        name: name.trim(),
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
        description: description.trim() || null,
        phases: validPhases.map((p) => ({
          id: p.id,
          startDate: new Date(p.startDate).toISOString(),
          endDate: new Date(p.endDate).toISOString(),
        })),
      });
      setSuccess(true);
      onUpdated?.();
      setTimeout(() => onClose(), 1500);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Có lỗi xảy ra khi cập nhật kỳ học.");
    } finally {
      setIsSubmitting(false);
    }
  };

  // Build phase display: use existing phases matched to template order.
  const displayPhases = PHASES_TEMPLATE.map((template, index) => {
    const existing = phases.find((p) => p.type === template.type);
    return {
      ...template,
      id: existing?.id ?? 0,
      startDate: existing?.startDate ?? "",
      endDate: existing?.endDate ?? "",
      index: existing ? phases.indexOf(existing) : index,
    };
  });

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
                <h2 className="text-xl font-bold text-slate-800">Chỉnh sửa Kỳ Học</h2>
                <p className="text-sm text-slate-500">Cập nhật thông tin và thời gian các giai đoạn</p>
              </div>
              <button
                onClick={handleDismiss}
                className="p-1 transition-colors rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100"
              >
                <span className="material-symbols-outlined">close</span>
              </button>
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
                  <p className="text-sm font-semibold text-green-800">Cập nhật kỳ học thành công!</p>
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
                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Mã kỳ học</label>
                    <input
                      className="w-full px-3 py-2 text-sm transition-all border rounded-md outline-none cursor-not-allowed border-slate-200 bg-slate-50 text-slate-500"
                      type="text"
                      value={initialData?.code ?? ""}
                      readOnly
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
                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
                    {displayPhases.map((phase) => (
                      <div
                        key={phase.type}
                        className="p-3 transition-shadow bg-white border rounded shadow-sm border-slate-200 hover:shadow-md"
                      >
                        <p className={`text-xs font-bold ${phase.color} uppercase mb-2`}>{phase.label}</p>
                        <input
                          className="w-full p-0 text-sm bg-transparent border-none focus:ring-0"
                          type="date"
                          value={phase.startDate}
                          onChange={(e) => updatePhase(phase.index, "startDate", e.target.value)}
                        />
                        <div className="h-px my-1 bg-slate-100" />
                        <input
                          className="w-full p-0 text-sm bg-transparent border-none focus:ring-0"
                          type="date"
                          value={phase.endDate}
                          onChange={(e) => updatePhase(phase.index, "endDate", e.target.value)}
                        />
                      </div>
                    ))}
                  </div>
                </div>
              </motion.section>

              {/* Roster management lives on its own page */}
              <div className="flex items-center justify-between gap-3 p-4 border rounded-lg border-slate-200 bg-slate-50/60">
                <div className="flex items-start gap-2">
                  <span className="material-symbols-outlined text-primary text-[20px] mt-0.5">group</span>
                  <p className="text-xs leading-relaxed text-slate-600">
                    Quản lý danh sách sinh viên / giảng viên đủ điều kiện (nhập Excel, sửa ngành, công bố &amp; gửi thông
                    báo) tại trang riêng.
                  </p>
                </div>
                <button
                  type="button"
                  onClick={goToRoster}
                  className="flex items-center gap-1 px-3 py-2 text-xs font-bold transition-colors border rounded-md whitespace-nowrap text-primary border-primary/30 bg-primary/5 hover:bg-primary/10"
                >
                  Quản lý danh sách{" "}
                  <span className="material-symbols-outlined text-[16px]">arrow_forward</span>
                </button>
              </div>
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
                    <span className="material-symbols-outlined animate-spin text-[18px]">progress_activity</span>{" "}
                    Đang lưu...
                  </>
                ) : (
                  "Cập nhật kỳ học"
                )}
              </button>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
