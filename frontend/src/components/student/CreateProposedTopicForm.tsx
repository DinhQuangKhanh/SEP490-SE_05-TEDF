import { useEffect, useRef, useState } from "react";
import { AutoResizeTextarea } from "@/components/common/AutoResizeTextarea";
import { proposedTopicService } from "@/lib";
import { AvailableMentor, CreateProposedTopicRequest } from "@/types";

interface Props {
  groupId: string;
  onCreated: () => void;
  onCancel: () => void;
}

export function CreateProposedTopicForm({ groupId, onCreated, onCancel }: Props) {
  const [mentors, setMentors] = useState<AvailableMentor[]>([]);
  const [majorName, setMajorName] = useState("");
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({
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
  });

  useEffect(() => {
    // The student's program is fixed by the roster; the form only displays it (read-only).
    proposedTopicService
      .getAvailableMentors()
      .then((res) => {
        setMentors(res.mentors);
        setMajorName(res.majorName);
        setForm((f) => ({ ...f, majorId: res.majorId }));
      })
      .catch((err) => setError(err instanceof Error ? err.message : "Không thể tải danh sách giảng viên."))
      .finally(() => setLoading(false));
  }, []);

  // Mentor autocomplete state (must be before any early returns - Rules of Hooks)
  const [mentorSearch, setMentorSearch] = useState("");
  const [mentorOpen, setMentorOpen] = useState(false);
  const mentorRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (mentorRef.current && !mentorRef.current.contains(e.target as Node)) {
        setMentorOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const update = (key: string, value: string | number) => setForm((prev) => ({ ...prev, [key]: value }));

  const isValid =
    form.nameVi.trim() &&
    form.nameEn.trim() &&
    form.nameAbbr.trim() &&
    form.description.trim() &&
    form.objectives.trim() &&
    form.mentorId &&
    form.majorId > 0;

  const handleSubmit = async () => {
    if (!isValid) return;
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
      onCreated();
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
    const label = `${m.academicTitle ? m.academicTitle + ". " : ""}${m.fullName} ${m.email}`.toLowerCase();
    return label.includes(q);
  });

  if (loading) {
    return (
      <div className="flex items-center justify-center h-48">
        <div className="w-8 h-8 border-b-2 rounded-full animate-spin border-primary" />
      </div>
    );
  }

  return (
    <div className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm">
      <div className="p-5 border-b border-[#e9ecf1] flex items-center justify-between">
        <h3 className="font-bold text-[#101319] flex items-center gap-2">
          <span className="material-symbols-outlined text-primary">edit_note</span>
          Đề xuất đề tài mới
        </h3>
        <button onClick={onCancel} className="text-[#58698d] hover:text-red-500 transition-colors">
          <span className="material-symbols-outlined">close</span>
        </button>
      </div>

      <div className="p-6 space-y-5">
        {error && <div className="p-3 text-sm text-red-700 border border-red-200 rounded-lg bg-red-50">{error}</div>}

        {/* Major — read-only: a student may only propose within their own program */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Chuyên ngành</label>
          <input
            value={majorName}
            disabled
            readOnly
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm bg-gray-50 text-[#58698d] cursor-not-allowed outline-none"
          />
          <p className="mt-1 text-xs text-[#58698d]">Đề tài thuộc chuyên ngành bạn đang theo học, không thể thay đổi.</p>
        </div>

        {/* Name Vi */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">
            Tên đề tài (Tiếng Việt) <span className="text-red-500">*</span>
          </label>
          <input
            value={form.nameVi}
            onChange={(e) => update("nameVi", e.target.value)}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="Nhập tên đề tài tiếng Việt..."
          />
        </div>

        {/* Name En */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">
            Tên đề tài (Tiếng Anh) <span className="text-red-500">*</span>
          </label>
          <input
            value={form.nameEn}
            onChange={(e) => update("nameEn", e.target.value)}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="Enter topic name in English..."
          />
        </div>

        {/* Name Abbr */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">
            Tên viết tắt <span className="text-red-500">*</span>
          </label>
          <input
            value={form.nameAbbr}
            onChange={(e) => update("nameAbbr", e.target.value)}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="Ví dụ: QLDT, HTQL, ..."
            maxLength={20}
          />
        </div>

        {/* Description */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">
            Mô tả đề tài <span className="text-red-500">*</span>
          </label>
          <AutoResizeTextarea
            value={form.description}
            onChange={(e) => update("description", e.target.value)}
            rows={4}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="Mô tả tổng quan về đề tài..."
          />
        </div>

        {/* Objectives */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">
            Mục tiêu <span className="text-red-500">*</span>
          </label>
          <AutoResizeTextarea
            value={form.objectives}
            onChange={(e) => update("objectives", e.target.value)}
            rows={3}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="Mục tiêu cần đạt được..."
          />
        </div>

        {/* Scope */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Phạm vi nghiên cứu</label>
          <AutoResizeTextarea
            value={form.scope}
            onChange={(e) => update("scope", e.target.value)}
            rows={2}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="Giới hạn phạm vi nghiên cứu..."
          />
        </div>

        {/* Technologies */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Công nghệ sử dụng</label>
          <input
            value={form.technologies}
            onChange={(e) => update("technologies", e.target.value)}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="React, ASP.NET Core, SQL Server, ..."
          />
        </div>

        {/* Expected Results */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Kết quả dự kiến</label>
          <AutoResizeTextarea
            value={form.expectedResults}
            onChange={(e) => update("expectedResults", e.target.value)}
            rows={2}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="Kết quả kỳ vọng sau khi hoàn thành..."
          />
        </div>

        {/* Mentor Selection */}
        <div ref={mentorRef} className="relative">
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">
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
                if (form.mentorId) update("mentorId", "");
              }}
              onFocus={() => setMentorOpen(true)}
              placeholder="Nhập tên giảng viên để tìm kiếm..."
              className="w-full px-3 py-2.5 pr-8 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            />
            {form.mentorId ? (
              <button
                type="button"
                onClick={() => {
                  update("mentorId", "");
                  setMentorSearch("");
                }}
                className="absolute -translate-y-1/2 right-2 top-1/2 text-slate-400 hover:text-red-500"
              >
                <span className="text-lg material-symbols-outlined">close</span>
              </button>
            ) : (
              <span className="absolute text-lg -translate-y-1/2 pointer-events-none material-symbols-outlined text-slate-400 right-2 top-1/2">
                search
              </span>
            )}
          </div>
          {mentorOpen && filteredMentors.length > 0 && !form.mentorId && (
            <div className="absolute z-20 mt-1 w-full bg-white border border-[#e9ecf1] rounded-lg shadow-lg max-h-48 overflow-y-auto">
              {filteredMentors.map((m) => {
                const isFull = m.currentGroupCount >= m.maxGroups;
                return (
                  <button
                    key={m.mentorId}
                    type="button"
                    disabled={isFull}
                    onClick={() => {
                      update("mentorId", m.mentorId);
                      setMentorSearch("");
                      setMentorOpen(false);
                    }}
                    className={`w-full text-left px-3 py-2.5 text-sm border-b border-[#e9ecf1] last:border-b-0 transition-colors ${isFull ? "opacity-50 cursor-not-allowed bg-slate-50" : "hover:bg-primary/5 cursor-pointer"}`}
                  >
                    <div className="font-medium text-[#101319]">
                      {m.academicTitle ? `${m.academicTitle}. ` : ""}
                      {m.fullName}
                    </div>
                    <div className="text-xs text-[#58698d]">
                      {m.email} · {m.currentGroupCount}/{m.maxGroups} nhóm {isFull ? "(Đã đầy)" : ""}
                    </div>
                  </button>
                );
              })}
            </div>
          )}
          {mentorOpen && filteredMentors.length === 0 && mentorSearch.trim() && !form.mentorId && (
            <div className="absolute z-20 mt-1 w-full bg-white border border-[#e9ecf1] rounded-lg shadow-lg p-3 text-sm text-slate-500 text-center">
              Không tìm thấy giảng viên
            </div>
          )}
          {selectedMentor && (
            <p className="mt-1.5 text-xs text-[#58698d]">
              {selectedMentor.email} · Đang hướng dẫn {selectedMentor.currentGroupCount}/{selectedMentor.maxGroups} nhóm
            </p>
          )}
        </div>

        {/* Actions */}
        <div className="flex items-center gap-3 pt-3 border-t border-[#e9ecf1]">
          <button
            onClick={handleSubmit}
            disabled={!isValid || submitting}
            className="flex-1 py-2.5 bg-primary text-white rounded-lg text-sm font-bold hover:bg-primary-light transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {submitting ? (
              <>
                <div className="w-4 h-4 border-2 border-white rounded-full animate-spin border-t-transparent" />
                Đang tạo...
              </>
            ) : (
              <>
                <span className="text-lg material-symbols-outlined">add_circle</span>
                Tạo đề tài
              </>
            )}
          </button>
          <button
            onClick={onCancel}
            className="px-5 py-2.5 border border-[#e9ecf1] text-[#58698d] rounded-lg text-sm font-semibold hover:bg-gray-50 transition-colors"
          >
            Hủy
          </button>
        </div>
      </div>
    </div>
  );
}
