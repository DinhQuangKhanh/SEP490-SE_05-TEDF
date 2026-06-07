import { useState } from "react";
import { directTopicService, type CreateDirectTopicPayload } from "@/lib/directTopicService";

interface Props {
  projectId: string;
  initialData: Partial<CreateDirectTopicPayload>;
  onUpdated: () => void;
  onCancel: () => void;
}

export function EditDirectTopicForm({ projectId, initialData, onUpdated, onCancel }: Props) {
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({
    nameVi: initialData.nameVi || "",
    nameEn: initialData.nameEn || "",
    nameAbbr: initialData.nameAbbr || "",
    description: initialData.description || "",
    objectives: initialData.objectives || "",
    scope: initialData.scope || "",
    technologies: initialData.technologies || "",
    expectedResults: initialData.expectedResults || "",
    maxStudents: initialData.maxStudents || 5,
  });

  const update = (key: string, value: string | number) => setForm((prev) => ({ ...prev, [key]: value }));

  const isValid =
    form.nameVi.trim() &&
    form.nameEn.trim() &&
    form.nameAbbr.trim() &&
    form.description.trim() &&
    form.objectives.trim();

  const handleSubmit = async () => {
    if (!isValid) return;
    setSubmitting(true);
    setError(null);
    try {
      const payload: Partial<CreateDirectTopicPayload> = {
        nameVi: form.nameVi.trim(),
        nameEn: form.nameEn.trim(),
        nameAbbr: form.nameAbbr.trim(),
        description: form.description.trim(),
        objectives: form.objectives.trim(),
        scope: form.scope.trim() || undefined,
        technologies: form.technologies.trim() || undefined,
        expectedResults: form.expectedResults.trim() || undefined,
        maxStudents: form.maxStudents,
      };
      await directTopicService.updateTopic(projectId, payload);
      onUpdated();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Đã xảy ra lỗi.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm">
      <div className="p-5 border-b border-[#e9ecf1] flex items-center justify-between">
        <h3 className="font-bold text-[#101319] flex items-center gap-2">
          <span className="material-symbols-outlined text-primary">edit_note</span>
          Chỉnh sửa đề tài
        </h3>
        <button onClick={onCancel} className="text-[#58698d] hover:text-red-500 transition-colors">
          <span className="material-symbols-outlined">close</span>
        </button>
      </div>

      <div className="p-6 space-y-5">
        {error && <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">{error}</div>}

        {/* Tên đề tài Tiếng Việt */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Tên Đề Tài Tiếng Việt</label>
          <input
            type="text"
            value={form.nameVi}
            onChange={(e) => update("nameVi", e.target.value)}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="Nhập tên đề tài..."
          />
        </div>

        {/* Tên đề tài Tiếng Anh */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Tên Đề Tài Tiếng Anh</label>
          <input
            type="text"
            value={form.nameEn}
            onChange={(e) => update("nameEn", e.target.value)}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="Enter topic name..."
          />
        </div>

        {/* Tên Rút Gọn */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Tên Rút Gọn</label>
          <input
            type="text"
            value={form.nameAbbr}
            onChange={(e) => update("nameAbbr", e.target.value)}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="VD: QLCT"
            maxLength={20}
          />
        </div>

        {/* Mô Tả */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Mô Tả Đề Tài</label>
          <textarea
            value={form.description}
            onChange={(e) => update("description", e.target.value)}
            rows={3}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none"
            placeholder="Mô tả chi tiết về đề tài..."
          />
        </div>

        {/* Mục Tiêu */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Mục Tiêu</label>
          <textarea
            value={form.objectives}
            onChange={(e) => update("objectives", e.target.value)}
            rows={2}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none"
            placeholder="Mục tiêu chính của đề tài..."
          />
        </div>

        {/* Phạm Vi */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Phạm Vi (Tùy Chọn)</label>
          <textarea
            value={form.scope}
            onChange={(e) => update("scope", e.target.value)}
            rows={2}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none"
            placeholder="Phạm vi của đề tài..."
          />
        </div>

        {/* Công Nghệ */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Công Nghệ Sử Dụng (Tùy Chọn)</label>
          <input
            type="text"
            value={form.technologies}
            onChange={(e) => update("technologies", e.target.value)}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
            placeholder="VD: React, Node.js, MongoDB"
          />
        </div>

        {/* Kết Quả Dự Kiến */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Kết Quả Dự Kiến (Tùy Chọn)</label>
          <textarea
            value={form.expectedResults}
            onChange={(e) => update("expectedResults", e.target.value)}
            rows={2}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none"
            placeholder="Kết quả mong đợi sau khi hoàn thành..."
          />
        </div>

        {/* Số Sinh Viên Tối Đa */}
        <div>
          <label className="block text-sm font-semibold text-[#101319] mb-1.5">Số Sinh Viên Tối Đa</label>
          <input
            type="number"
            value={form.maxStudents}
            onChange={(e) => update("maxStudents", parseInt(e.target.value) || 5)}
            min={1}
            max={20}
            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
          />
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
                <div className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent" />
                Đang cập nhật...
              </>
            ) : (
              <>
                <span className="material-symbols-outlined text-lg">check</span>
                Cập Nhật
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
