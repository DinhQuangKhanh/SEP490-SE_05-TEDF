import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { motion, AnimatePresence } from "framer-motion";
import { AutoResizeTextarea } from "@/components/common/AutoResizeTextarea";
import { proposedTopicService } from "@/lib";
import { AvailableMentor, CreateProposedTopicRequest } from "@/types";

interface Props {
  groupId: string;
  onCreated: () => void;
  onCancel: () => void;
}

const inputClass =
  "block w-full border border-slate-300 px-4 py-3 rounded-xl bg-white text-sm placeholder:text-slate-400 focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all outline-none";
const textareaClass = `${inputClass} leading-relaxed`;
const labelClass = "block text-sm font-semibold text-slate-700 mb-1.5";

const STEPS = [
  { label: "Thông tin", icon: "info" },
  { label: "Nội dung", icon: "description" },
  { label: "Xác nhận", icon: "task_alt" },
];

const slideVariants = {
  enter: (dir: number) => ({ x: dir > 0 ? 80 : -80, opacity: 0 }),
  center: { x: 0, opacity: 1 },
  exit: (dir: number) => ({ x: dir > 0 ? -80 : 80, opacity: 0 }),
};

const emptyForm = {
  nameVi: "",
  nameEn: "",
  nameAbbr: "",
  description: "",
  objectives: "",
  scope: "",
  technologies: "",
  expectedResults: "",
  mentorId: "",
  majorId: 0,
};

export function CreateProposedTopicForm({ groupId, onCreated, onCancel }: Props) {
  const [step, setStep] = useState(0);
  const [dir, setDir] = useState(1);
  const [form, setForm] = useState({ ...emptyForm });
  const [mentors, setMentors] = useState<AvailableMentor[]>([]);
  const [majorName, setMajorName] = useState("");
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showSuccess, setShowSuccess] = useState(false);

  const [mentorSearch, setMentorSearch] = useState("");
  const [mentorOpen, setMentorOpen] = useState(false);
  const mentorRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // The student's program is fixed by the roster; the form only displays it (read-only).
    proposedTopicService
      .getAvailableMentors()
      .then((res) => {
        setMentors(res.mentors);
        setMajorName(res.majorName);
        setForm((f) => ({ ...f, majorId: res.majorId }));
      })
      .catch((err) => setError(err instanceof Error ? err.message : "Không thể tải dữ liệu đề xuất."))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (mentorRef.current && !mentorRef.current.contains(e.target as Node)) setMentorOpen(false);
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const set = (key: string, value: string | number) => setForm((prev) => ({ ...prev, [key]: value }));

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
        return form.majorId > 0 && !!form.mentorId && !!form.nameVi.trim() && !!form.nameAbbr.trim();
      case 1:
        return !!form.description.trim() && !!form.objectives.trim();
      default:
        return true;
    }
  };

  const handleSubmit = async () => {
    setSubmitting(true);
    setError(null);
    try {
      const payload: CreateProposedTopicRequest = {
        nameVi: form.nameVi.trim(),
        nameEn: form.nameEn.trim(),
        nameAbbr: form.nameAbbr.trim(),
        description: form.description.trim(),
        objectives: form.objectives.trim(),
        scope: form.scope.trim() || undefined,
        technologies: form.technologies.trim() || undefined,
        expectedResults: form.expectedResults.trim() || undefined,
        mentorId: form.mentorId,
        groupId,
        majorId: form.majorId,
      };
      await proposedTopicService.createProposedTopic(payload);
      setShowSuccess(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Đã xảy ra lỗi.");
    } finally {
      setSubmitting(false);
    }
  };

  const selectedMentor = mentors.find((m) => m.mentorId === form.mentorId);
  const filteredMentors = mentors.filter((m) => {
    if (!mentorSearch.trim()) return true;
    const q = mentorSearch.toLowerCase();
    return `${m.academicTitle ? m.academicTitle + ". " : ""}${m.fullName} ${m.email}`.toLowerCase().includes(q);
  });

  return createPortal(
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm"
      onClick={() => onCancel()}
    >
      <motion.div
        initial={{ opacity: 0, scale: 0.95, y: 20 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        transition={{ type: "spring", damping: 25, stiffness: 300 }}
        onClick={(e) => e.stopPropagation()}
        className="bg-white w-full max-w-3xl max-h-[90vh] rounded-2xl shadow-2xl overflow-hidden flex flex-col"
      >
        {showSuccess ? (
          <div className="flex flex-col items-center justify-center px-8 py-16 text-center">
            <div className="flex items-center justify-center mb-6 rounded-full size-20 bg-emerald-100">
              <span className="material-symbols-outlined text-emerald-600 text-[40px]">check_circle</span>
            </div>
            <h2 className="mb-3 text-xl font-bold text-slate-900">Đề xuất thành công!</h2>
            <p className="max-w-md mb-8 text-sm leading-relaxed text-slate-600">
              Đề tài đã được tạo. Hãy gửi cho giảng viên hướng dẫn để được xem xét và duyệt.
            </p>
            <button
              onClick={() => onCreated()}
              className="px-8 py-3 text-sm font-semibold text-white transition-all shadow-lg rounded-xl bg-primary hover:bg-primary/90"
            >
              Hoàn tất
            </button>
          </div>
        ) : (
          <>
            {/* Header */}
            <div className="flex items-center justify-between px-8 py-5 border-b border-slate-100 shrink-0">
              <div>
                <h1 className="text-xl font-extrabold text-slate-900">Đề Xuất Đề Tài Mới</h1>
                <p className="text-xs text-slate-500 mt-0.5">Điền thông tin để đề xuất đề tài cho nhóm</p>
              </div>
              <button
                onClick={() => onCancel()}
                className="text-slate-400 hover:text-slate-600 transition-colors p-1.5 hover:bg-slate-100 rounded-lg"
              >
                <span className="material-symbols-outlined">close</span>
              </button>
            </div>

            {/* Step indicator */}
            <div className="px-8 py-4 border-b border-slate-50 bg-slate-50/50 shrink-0">
              <div className="flex items-center gap-2">
                {STEPS.map((s, idx) => (
                  <div key={s.label} className="flex items-center flex-1 gap-2">
                    <div
                      className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold transition-all ${
                        idx < step
                          ? "bg-emerald-100 text-emerald-700"
                          : idx === step
                            ? "bg-primary text-white shadow-sm"
                            : "bg-slate-100 text-slate-400"
                      }`}
                    >
                      <span className="material-symbols-outlined text-[14px]">{idx < step ? "check_circle" : s.icon}</span>
                      <span className="hidden sm:inline">{s.label}</span>
                    </div>
                    {idx < STEPS.length - 1 && (
                      <div className={`flex-1 h-px ${idx < step ? "bg-emerald-300" : "bg-slate-200"}`} />
                    )}
                  </div>
                ))}
              </div>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto px-8 py-6 relative min-h-[320px]">
              {loading ? (
                <div className="flex items-center justify-center h-48">
                  <div className="w-8 h-8 border-b-2 rounded-full animate-spin border-primary" />
                </div>
              ) : (
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
                      <div className="space-y-5">
                        {/* Major — read-only */}
                        <div>
                          <label className={labelClass}>Chuyên ngành</label>
                          <input
                            value={majorName}
                            disabled
                            readOnly
                            className={`${inputClass} bg-slate-50 text-slate-500 cursor-not-allowed`}
                          />
                          <p className="mt-1 text-xs text-slate-400">
                            Đề tài thuộc chuyên ngành bạn đang theo học, không thể thay đổi.
                          </p>
                        </div>

                        {/* Mentor */}
                        <div ref={mentorRef} className="relative">
                          <label className={labelClass}>
                            Giảng viên hướng dẫn <span className="text-red-500">*</span>
                          </label>
                          <div className="relative">
                            <input
                              value={
                                form.mentorId
                                  ? selectedMentor
                                    ? `${selectedMentor.academicTitle ? selectedMentor.academicTitle + ". " : ""}${selectedMentor.fullName}`
                                    : ""
                                  : mentorSearch
                              }
                              onChange={(e) => {
                                setMentorSearch(e.target.value);
                                setMentorOpen(true);
                                if (form.mentorId) set("mentorId", "");
                              }}
                              onFocus={() => setMentorOpen(true)}
                              placeholder="Nhập tên giảng viên để tìm kiếm..."
                              className={`${inputClass} pr-10`}
                            />
                            {form.mentorId ? (
                              <button
                                type="button"
                                onClick={() => {
                                  set("mentorId", "");
                                  setMentorSearch("");
                                }}
                                className="absolute -translate-y-1/2 right-3 top-1/2 text-slate-400 hover:text-red-500"
                              >
                                <span className="text-lg material-symbols-outlined">close</span>
                              </button>
                            ) : (
                              <span className="absolute text-lg -translate-y-1/2 pointer-events-none material-symbols-outlined text-slate-400 right-3 top-1/2">
                                search
                              </span>
                            )}
                          </div>
                          {mentorOpen && !form.mentorId && filteredMentors.length > 0 && (
                            <div className="absolute z-20 mt-1 w-full bg-white border border-slate-200 rounded-xl shadow-lg max-h-52 overflow-y-auto">
                              {filteredMentors.map((m) => {
                                const isFull = m.currentGroupCount >= m.maxGroups;
                                return (
                                  <button
                                    key={m.mentorId}
                                    type="button"
                                    disabled={isFull}
                                    onClick={() => {
                                      set("mentorId", m.mentorId);
                                      setMentorSearch("");
                                      setMentorOpen(false);
                                    }}
                                    className={`w-full text-left px-4 py-2.5 text-sm border-b border-slate-100 last:border-b-0 transition-colors ${
                                      isFull ? "opacity-50 cursor-not-allowed bg-slate-50" : "hover:bg-primary/5 cursor-pointer"
                                    }`}
                                  >
                                    <div className="font-medium text-slate-800">
                                      {m.academicTitle ? `${m.academicTitle}. ` : ""}
                                      {m.fullName}
                                    </div>
                                    <div className="text-xs text-slate-500">
                                      {m.email} · {m.currentGroupCount}/{m.maxGroups} nhóm {isFull ? "(Đã đầy)" : ""}
                                    </div>
                                  </button>
                                );
                              })}
                            </div>
                          )}
                          {mentorOpen && !form.mentorId && mentorSearch.trim() && filteredMentors.length === 0 && (
                            <div className="absolute z-20 mt-1 w-full bg-white border border-slate-200 rounded-xl shadow-lg p-3 text-sm text-center text-slate-500">
                              Không tìm thấy giảng viên
                            </div>
                          )}
                          {selectedMentor && (
                            <p className="mt-1.5 text-xs text-slate-500">
                              {selectedMentor.email} · Đang hướng dẫn {selectedMentor.currentGroupCount}/
                              {selectedMentor.maxGroups} nhóm
                            </p>
                          )}
                        </div>

                        <div>
                          <label className={labelClass}>
                            Tên đề tài (Tiếng Việt) <span className="text-red-500">*</span>
                          </label>
                          <input
                            value={form.nameVi}
                            onChange={(e) => set("nameVi", e.target.value)}
                            className={inputClass}
                            placeholder="Nhập tên đề tài đầy đủ bằng tiếng Việt..."
                          />
                        </div>
                        <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
                          <div>
                            <label className={labelClass}>Tên đề tài (Tiếng Anh)</label>
                            <input
                              value={form.nameEn}
                              onChange={(e) => set("nameEn", e.target.value)}
                              className={inputClass}
                              placeholder="Enter project name in English..."
                            />
                          </div>
                          <div>
                            <label className={labelClass}>
                              Tên viết tắt <span className="text-red-500">*</span>
                            </label>
                            <input
                              value={form.nameAbbr}
                              onChange={(e) => set("nameAbbr", e.target.value)}
                              maxLength={20}
                              className={inputClass}
                              placeholder="VD: QLDT, HTQL..."
                            />
                          </div>
                        </div>
                      </div>
                    )}

                    {step === 1 && (
                      <div className="space-y-5">
                        <div>
                          <label className={labelClass}>
                            Mô tả đề tài <span className="text-red-500">*</span>
                          </label>
                          <AutoResizeTextarea
                            rows={4}
                            value={form.description}
                            onChange={(e) => set("description", e.target.value)}
                            className={textareaClass}
                            placeholder="Mô tả tổng quan về đề tài..."
                          />
                        </div>
                        <div>
                          <label className={labelClass}>
                            Mục tiêu <span className="text-red-500">*</span>
                          </label>
                          <AutoResizeTextarea
                            rows={3}
                            value={form.objectives}
                            onChange={(e) => set("objectives", e.target.value)}
                            className={textareaClass}
                            placeholder="Mục tiêu cần đạt được..."
                          />
                        </div>
                        <div>
                          <label className={labelClass}>Phạm vi nghiên cứu</label>
                          <AutoResizeTextarea
                            rows={2}
                            value={form.scope}
                            onChange={(e) => set("scope", e.target.value)}
                            className={textareaClass}
                            placeholder="Giới hạn phạm vi nghiên cứu..."
                          />
                        </div>
                        <div>
                          <label className={labelClass}>Công nghệ sử dụng</label>
                          <input
                            value={form.technologies}
                            onChange={(e) => set("technologies", e.target.value)}
                            className={inputClass}
                            placeholder="React, ASP.NET Core, SQL Server..."
                          />
                        </div>
                        <div>
                          <label className={labelClass}>Kết quả dự kiến</label>
                          <AutoResizeTextarea
                            rows={2}
                            value={form.expectedResults}
                            onChange={(e) => set("expectedResults", e.target.value)}
                            className={textareaClass}
                            placeholder="Kết quả kỳ vọng sau khi hoàn thành..."
                          />
                        </div>
                      </div>
                    )}

                    {step === 2 && (
                      <div className="p-4 border bg-slate-50 rounded-xl border-slate-100">
                        <h4 className="text-sm font-bold text-slate-700 mb-3 flex items-center gap-1.5">
                          <span className="material-symbols-outlined text-[16px] text-primary">summarize</span>
                          Tóm tắt đề tài
                        </h4>
                        <div className="grid grid-cols-2 gap-2 text-xs">
                          <div>
                            <span className="text-slate-500">Chuyên ngành:</span>{" "}
                            <span className="font-medium text-slate-700">{majorName || "—"}</span>
                          </div>
                          <div>
                            <span className="text-slate-500">Giảng viên:</span>{" "}
                            <span className="font-medium text-slate-700">{selectedMentor?.fullName ?? "—"}</span>
                          </div>
                          <div>
                            <span className="text-slate-500">Tên:</span>{" "}
                            <span className="font-medium text-slate-700">{form.nameVi || "—"}</span>
                          </div>
                          <div>
                            <span className="text-slate-500">Viết tắt:</span>{" "}
                            <span className="font-medium text-slate-700">{form.nameAbbr || "—"}</span>
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
                    )}
                  </motion.div>
                </AnimatePresence>
              )}
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
            <div className="flex items-center justify-between px-8 py-5 border-t bg-slate-50 border-slate-200 shrink-0">
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
                  onClick={() => onCancel()}
                  className="px-5 py-2.5 rounded-lg border border-slate-300 bg-white text-slate-700 text-sm font-semibold shadow-sm hover:bg-slate-50 transition-all"
                >
                  Hủy bỏ
                </button>
                {step < STEPS.length - 1 ? (
                  <button
                    onClick={goNext}
                    disabled={loading || !canProceed()}
                    className="px-6 py-2.5 rounded-lg bg-primary hover:bg-primary/90 text-white text-sm font-semibold shadow-md transition-all flex items-center gap-1.5 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Tiếp theo
                    <span className="material-symbols-outlined text-[18px]">arrow_forward</span>
                  </button>
                ) : (
                  <button
                    onClick={handleSubmit}
                    disabled={submitting}
                    className="px-6 py-2.5 rounded-lg bg-primary hover:bg-primary/90 text-white text-sm font-semibold shadow-md transition-all flex items-center gap-2 disabled:opacity-50"
                  >
                    {submitting ? (
                      <>
                        <span className="border-2 rounded-full size-4 border-white/30 border-t-white animate-spin" />
                        Đang tạo...
                      </>
                    ) : (
                      <>
                        <span className="material-symbols-outlined text-[18px]">add_circle</span>
                        Tạo đề tài
                      </>
                    )}
                  </button>
                )}
              </div>
            </div>
          </>
        )}
      </motion.div>
    </motion.div>,
    document.body,
  );
}
