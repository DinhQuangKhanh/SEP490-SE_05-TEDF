import { useEffect, useState } from "react";
import { Modal } from "@/components/common/Modal";
import { majorService, userService } from "@/lib";
import type { CreateUserRequest, MajorOption } from "@/types";

interface Props {
  onClose: () => void;
  onCreated: () => void;
}

const ROLE_OPTIONS = [
  { value: "Student", label: "Sinh viên" },
  { value: "Mentor", label: "Mentor" },
  { value: "Evaluator", label: "Evaluator" },
  { value: "DepartmentHead", label: "Trưởng bộ môn" },
];

const inputClass =
  "w-full px-3 py-2 text-sm border rounded-lg border-slate-300 focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition";
const labelClass = "block mb-1 text-sm font-semibold text-slate-700";

const emptyForm: CreateUserRequest = {
  role: "Student",
  email: "",
  fullName: "",
  code: "",
  phone: "",
  academicTitle: "",
  majorId: undefined,
};

export function AddUserModal({ onClose, onCreated }: Readonly<Props>) {
  const [form, setForm] = useState<CreateUserRequest>({ ...emptyForm });
  const [majors, setMajors] = useState<MajorOption[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    majorService.getMajors().then(setMajors).catch(() => {
      /* majors are optional for the form; ignore load failure */
    });
  }, []);

  const isStudent = form.role === "Student";
  const set = <K extends keyof CreateUserRequest>(key: K, value: CreateUserRequest[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const handleSubmit = async () => {
    setError(null);
    if (!form.fullName.trim() || !form.email.trim() || !form.code.trim()) {
      setError("Vui lòng nhập họ tên, email và mã số.");
      return;
    }
    setSubmitting(true);
    try {
      await userService.createUser({
        role: form.role,
        email: form.email.trim(),
        fullName: form.fullName.trim(),
        code: form.code.trim(),
        phone: form.phone?.trim() || undefined,
        academicTitle: isStudent ? undefined : form.academicTitle?.trim() || undefined,
        majorId: isStudent ? form.majorId : undefined,
      });
      onCreated();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Tạo người dùng thất bại.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal onClose={onClose} contentClassName="w-full max-w-lg bg-white shadow-2xl rounded-xl overflow-hidden">
      <div className="flex items-center justify-between px-6 py-4 bg-primary">
        <div className="flex items-center gap-2 text-white">
          <span className="material-symbols-outlined">person_add</span>
          <h3 className="text-lg font-bold">Thêm người dùng</h3>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="rounded-lg size-8 flex items-center justify-center text-white/80 hover:bg-white/10"
          aria-label="Đóng"
        >
          <span className="material-symbols-outlined text-[20px]">close</span>
        </button>
      </div>

      <div className="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
        <div>
          <label className={labelClass} htmlFor="add-user-role">Vai trò</label>
          <select id="add-user-role" className={inputClass} value={form.role} onChange={(e) => set("role", e.target.value)}>
            {ROLE_OPTIONS.map((r) => (
              <option key={r.value} value={r.value}>{r.label}</option>
            ))}
          </select>
        </div>

        <div>
          <label className={labelClass} htmlFor="add-user-fullname">Họ và tên</label>
          <input id="add-user-fullname" className={inputClass} value={form.fullName} onChange={(e) => set("fullName", e.target.value)} placeholder="Nguyễn Văn A" />
        </div>

        <div>
          <label className={labelClass} htmlFor="add-user-email">Email (@fpt.edu.vn)</label>
          <input id="add-user-email" className={inputClass} type="email" value={form.email} onChange={(e) => set("email", e.target.value)} placeholder="a@fpt.edu.vn" />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className={labelClass} htmlFor="add-user-code">{isStudent ? "Mã số sinh viên" : "Mã giảng viên"}</label>
            <input id="add-user-code" className={inputClass} value={form.code} onChange={(e) => set("code", e.target.value)} placeholder={isStudent ? "SE150001" : "GV0001"} />
          </div>
          <div>
            <label className={labelClass} htmlFor="add-user-phone">Số điện thoại</label>
            <input id="add-user-phone" className={inputClass} value={form.phone ?? ""} onChange={(e) => set("phone", e.target.value)} placeholder="09xxxxxxxx" />
          </div>
        </div>

        {isStudent ? (
          <div>
            <label className={labelClass} htmlFor="add-user-major">Ngành</label>
            <select
              id="add-user-major"
              className={inputClass}
              value={form.majorId ?? ""}
              onChange={(e) => set("majorId", e.target.value ? Number(e.target.value) : undefined)}
            >
              <option value="">— Chọn ngành —</option>
              {majors.map((m) => (
                <option key={m.id} value={m.id}>{m.name}</option>
              ))}
            </select>
          </div>
        ) : (
          <div>
            <label className={labelClass} htmlFor="add-user-academic-title">Học hàm / học vị</label>
            <input id="add-user-academic-title" className={inputClass} value={form.academicTitle ?? ""} onChange={(e) => set("academicTitle", e.target.value)} placeholder="ThS / TS / PGS.TS" />
          </div>
        )}

        {error && (
          <div className="px-3 py-2 text-sm text-red-700 border rounded-lg border-red-200 bg-red-50">{error}</div>
        )}
      </div>

      <div className="flex justify-end gap-3 px-6 py-4 border-t border-slate-100 bg-slate-50">
        <button type="button" onClick={onClose} className="px-4 py-2 text-sm font-semibold rounded-lg text-slate-600 hover:bg-slate-100">Hủy</button>
        <button
          type="button"
          onClick={handleSubmit}
          disabled={submitting}
          className="px-5 py-2 text-sm font-semibold text-white rounded-lg bg-primary hover:bg-primary-light disabled:opacity-50"
        >
          {submitting ? "Đang tạo..." : "Tạo người dùng"}
        </button>
      </div>
    </Modal>
  );
}
