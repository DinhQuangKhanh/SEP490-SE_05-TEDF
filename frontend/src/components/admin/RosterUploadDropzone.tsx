import { useRef } from "react";

interface RosterUploadDropzoneProps {
  /** Title shown when no file is selected (e.g. "Tải danh sách sinh viên"). */
  label: string;
  /** Helper text shown under the title. */
  hint: string;
  file: File | null;
  /** Called with the raw selected file. Parent validates extension and shows any error. */
  onSelect: (file: File) => void;
  onClear: () => void;
  /** Optional .csv template download link. */
  templateHref?: string;
  templateLabel?: string;
  disabled?: boolean;
}

/**
 * Drag-and-drop / click file picker for an eligibility-roster Excel/CSV.
 * Extracted from the (formerly duplicated) Create/Edit semester modal markup.
 */
export function RosterUploadDropzone({
  label,
  hint,
  file,
  onSelect,
  onClear,
  templateHref,
  templateLabel = "Tải file mẫu (.csv)",
  disabled = false,
}: Readonly<RosterUploadDropzoneProps>) {
  const inputRef = useRef<HTMLInputElement>(null);

  const pick = (f: File | undefined) => {
    if (!f || disabled) return;
    onSelect(f);
  };

  return (
    <div>
      <input
        ref={inputRef}
        type="file"
        accept=".csv,.xlsx,.xls"
        className="hidden"
        disabled={disabled}
        onChange={(e) => pick(e.target.files?.[0])}
      />
      <div
        role="button"
        tabIndex={disabled ? -1 : 0}
        aria-label={file ? "Tệp đã chọn, nhấn để xóa hoặc chọn lại" : label}
        className={`border-2 border-dashed rounded-xl p-6 transition-colors group ${
          disabled ? "opacity-60 cursor-not-allowed" : "cursor-pointer"
        } ${file ? "border-green-300 bg-green-50/30" : "border-slate-200 hover:border-primary/50"}`}
        onClick={() => !file && !disabled && inputRef.current?.click()}
        onKeyDown={(e) => { if ((e.key === "Enter" || e.key === " ") && !file && !disabled) inputRef.current?.click(); }}
        onDragOver={(e) => {
          e.preventDefault();
          e.stopPropagation();
        }}
        onDrop={(e) => {
          e.preventDefault();
          e.stopPropagation();
          pick(e.dataTransfer.files?.[0]);
        }}
      >
        <div className="flex flex-col items-center text-center">
          {file ? (
            <>
              <div className="flex items-center justify-center w-12 h-12 mb-3 text-green-600 bg-green-100 rounded-full">
                <span className="text-3xl material-symbols-outlined">check_circle</span>
              </div>
              <h4 className="mb-1 font-bold text-slate-800">Đã chọn tệp</h4>
              <p className="mb-1 text-xs font-medium text-slate-600">{file.name}</p>
              <p className="text-[10px] text-slate-400 mb-3">{(file.size / 1024).toFixed(1)} KB</p>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  onClear();
                  if (inputRef.current) inputRef.current.value = "";
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
              <h4 className="mb-1 font-bold text-slate-800">{label}</h4>
              <p className="px-4 mb-4 text-xs text-slate-500">{hint}</p>
              <button
                type="button"
                disabled={disabled}
                onClick={(e) => {
                  e.stopPropagation();
                  inputRef.current?.click();
                }}
                className="w-full px-4 py-2 text-sm font-semibold transition-colors bg-white border rounded-md border-slate-300 text-slate-700 hover:bg-slate-50 disabled:opacity-50"
              >
                Chọn tệp tin
              </button>
            </>
          )}
          {templateHref && (
            <a
              className="mt-3 text-[11px] text-primary hover:underline flex items-center gap-1"
              href={templateHref}
              download
            >
              <span className="material-symbols-outlined text-[14px]">download</span> {templateLabel}
            </a>
          )}
        </div>
      </div>
    </div>
  );
}
