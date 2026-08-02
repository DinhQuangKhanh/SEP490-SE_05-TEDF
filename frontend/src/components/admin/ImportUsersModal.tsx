import { useState } from "react";
import { Modal } from "@/components/common/Modal";
import { RosterUploadDropzone } from "@/components/admin/RosterUploadDropzone";
import { userService } from "@/lib";
import type { UserImportResponse } from "@/types";

interface Props {
  onClose: () => void;
  /** Called after a successful import so the parent can refresh the list. */
  onImported: () => void;
}

const ACCEPTED = new Set(["csv", "xlsx", "xls"]);

export function ImportUsersModal({ onClose, onImported }: Props) {
  const [file, setFile] = useState<File | null>(null);
  const [importing, setImporting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<UserImportResponse | null>(null);

  const selectFile = (f: File) => {
    const ext = f.name.split(".").pop()?.toLowerCase() ?? "";
    if (!ACCEPTED.has(ext)) {
      setError("Chỉ chấp nhận tệp .csv, .xlsx hoặc .xls.");
      return;
    }
    setError(null);
    setResult(null);
    setFile(f);
  };

  const handleDownloadTemplate = async () => {
    try {
      const blob = await userService.downloadImportTemplate();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "danh_sach_nguoi_dung_mau.xlsx";
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được file mẫu.");
    }
  };

  const handleImport = async () => {
    if (!file) return;
    setImporting(true);
    setError(null);
    try {
      const res = await userService.importUsers(file);
      setResult(res);
      if (res.successfullyImported > 0) onImported();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Nhập Excel thất bại.");
    } finally {
      setImporting(false);
    }
  };

  return (
    <Modal onClose={onClose} contentClassName="w-full max-w-lg bg-white shadow-2xl rounded-xl overflow-hidden">
      <div className="flex items-center justify-between px-6 py-4 bg-primary">
        <div className="flex items-center gap-2 text-white">
          <span className="material-symbols-outlined">upload_file</span>
          <h3 className="text-lg font-bold">Nhập người dùng từ Excel</h3>
        </div>
        <button onClick={onClose} className="rounded-lg size-8 flex items-center justify-center text-white/80 hover:bg-white/10" aria-label="Đóng">
          <span className="material-symbols-outlined text-[20px]">close</span>
        </button>
      </div>

      <div className="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
        <div className="flex items-start justify-between gap-3">
          <p className="text-sm text-slate-500">
            File cần các cột: <b>Vai trò</b> (Sinh viên/Mentor/Evaluator), <b>Email</b> (@fpt.edu.vn), <b>Họ tên</b>,
            <b> Mã số</b>, và tùy chọn Học hàm/học vị, Ngành, SĐT. Chỉ tạo được sinh viên và giảng viên.
          </p>
          <button
            type="button"
            onClick={handleDownloadTemplate}
            className="flex items-center gap-1 shrink-0 text-xs font-semibold text-primary hover:underline"
          >
            <span className="material-symbols-outlined text-[16px]">download</span>
            Tải file mẫu (.xlsx)
          </button>
        </div>

        <RosterUploadDropzone
          label="Tải danh sách người dùng"
          hint="Kéo thả hoặc chọn tệp .csv / .xlsx"
          file={file}
          onSelect={selectFile}
          onClear={() => { setFile(null); setResult(null); }}
          disabled={importing}
        />

        {error && (
          <div className="px-3 py-2 text-sm text-red-700 border rounded-lg border-red-200 bg-red-50">{error}</div>
        )}

        {result && (
          <div className="p-3 border rounded-lg border-slate-200 bg-slate-50">
            <p className="text-sm font-semibold text-slate-800">
              Đã tạo <span className="text-green-600">{result.successfullyImported}</span>/{result.totalProcessed} người dùng.
            </p>
            {result.issues.length > 0 && (
              <div className="mt-2 max-h-40 overflow-y-auto space-y-1">
                <p className="text-xs font-semibold text-amber-700">Các dòng bị bỏ qua ({result.issues.length}):</p>
                {result.issues.map((iss, i) => (
                  <p key={`${iss.code}-${i}`} className="text-xs text-slate-600">
                    <span className="font-medium">{iss.code}</span>: {iss.reason}
                  </p>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      <div className="flex justify-end gap-3 px-6 py-4 border-t border-slate-100 bg-slate-50">
        <button onClick={onClose} className="px-4 py-2 text-sm font-semibold rounded-lg text-slate-600 hover:bg-slate-100">
          {result ? "Đóng" : "Hủy"}
        </button>
        <button
          onClick={handleImport}
          disabled={!file || importing}
          className="px-5 py-2 text-sm font-semibold text-white rounded-lg bg-primary hover:bg-primary-light disabled:opacity-50"
        >
          {importing ? "Đang nhập..." : "Nhập"}
        </button>
      </div>
    </Modal>
  );
}
