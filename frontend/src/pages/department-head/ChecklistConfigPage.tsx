import { useCallback, useEffect, useRef, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { checklistService } from "@/lib";
import { useSystemError } from "@/contexts/SystemErrorContext";
import type {
  ChecklistConfigDto,
  ChecklistConfigListResponse,
  ChecklistCriterionInput,
  ChecklistImportPreviewResponse,
  ChecklistSemesterOptionDto,
} from "@/types";

const STATUS_DISPLAY: Record<string, { label: string; colors: string }> = {
  Draft: { label: "Nháp", colors: "bg-slate-100 text-slate-600 border-slate-200" },
  Active: { label: "Đang áp dụng", colors: "bg-green-50 text-green-600 border-green-200" },
  Inactive: { label: "Ngừng áp dụng", colors: "bg-gray-100 text-gray-500 border-gray-200" },
};

function formatDate(iso: string | null): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleString("vi-VN", { dateStyle: "short", timeStyle: "short" });
}

export function ChecklistConfigPage() {
  const { showError } = useSystemError();
  const [data, setData] = useState<ChecklistConfigListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [filterSemester, setFilterSemester] = useState<string>("");
  const [busy, setBusy] = useState(false);

  const [editor, setEditor] = useState<{ open: boolean; config: ChecklistConfigDto | null }>({
    open: false,
    config: null,
  });
  const [importOpen, setImportOpen] = useState(false);
  const [confirm, setConfirm] = useState<{ kind: "activate" | "deactivate"; config: ChecklistConfigDto } | null>(null);
  const [copyFor, setCopyFor] = useState<ChecklistConfigDto | null>(null);

  const fetchData = useCallback(
    (semesterId?: number) => {
      setLoading(true);
      checklistService
        .getConfigs(semesterId)
        .then(setData)
        .catch((err) => showError(err.message))
        .finally(() => setLoading(false));
    },
    [showError],
  );

  useEffect(() => {
    fetchData(filterSemester ? Number(filterSemester) : undefined);
  }, [fetchData, filterSemester]);

  const refresh = useCallback(() => {
    fetchData(filterSemester ? Number(filterSemester) : undefined);
  }, [fetchData, filterSemester]);

  const semesters = data?.semesters ?? [];
  const configs = data?.configs ?? [];

  async function handleActivate(config: ChecklistConfigDto) {
    setBusy(true);
    try {
      await checklistService.activateConfig(config.id);
      setConfirm(null);
      refresh();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể kích hoạt checklist.");
    } finally {
      setBusy(false);
    }
  }

  async function handleDeactivate(config: ChecklistConfigDto) {
    setBusy(true);
    try {
      await checklistService.deactivateConfig(config.id);
      setConfirm(null);
      refresh();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể ngừng sử dụng checklist.");
    } finally {
      setBusy(false);
    }
  }

  /**
   * Config table body: loading / empty / the rows.
   * Split out of the JSX so the three states read as guards instead of nested ternaries.
   */
  function renderConfigs() {
    if (loading) {
      return (
        <div className="flex items-center justify-center gap-3 py-16 text-slate-400">
          <span className="material-symbols-outlined animate-spin">progress_activity</span>
          <span className="text-sm">Đang tải...</span>
        </div>
      );
    }

    if (configs.length === 0) {
      return (
        <div className="flex flex-col items-center justify-center gap-2 py-16 text-slate-400">
          <span className="material-symbols-outlined text-4xl">checklist</span>
          <p className="text-sm font-medium">Chưa có checklist nào</p>
        </div>
      );
    }

    return (
      <table className="w-full text-left border-collapse">
        <thead>
          <tr className="bg-gray-50/80 border-b border-gray-100">
            {["Học kỳ", "Phiên bản", "Trạng thái", "Số tiêu chí", "Cần đạt tối thiểu", "File nguồn", "Cập nhật", "Thao tác"].map(
              (h) => (
                <th
                  key={h}
                  className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap last:text-right"
                >
                  {h}
                </th>
              ),
            )}
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {configs.map((config) => {
            const status = STATUS_DISPLAY[config.status];
            return (
              <tr key={config.id} className="hover:bg-blue-50/20 transition-colors">
                <td className="px-6 py-4 text-sm font-semibold text-slate-800 whitespace-nowrap">
                  {config.semesterName}
                </td>
                <td className="px-6 py-4 text-sm text-slate-600">v{config.version}</td>
                <td className="px-6 py-4">
                  <span
                    className={`inline-flex items-center rounded-full border px-3 py-1 text-xs font-bold ${status?.colors ?? ""}`}
                  >
                    {status?.label ?? config.status}
                  </span>
                </td>
                <td className="px-6 py-4 text-sm text-slate-600">{config.criteriaCount}</td>
                <td className="px-6 py-4 text-sm text-slate-600">
                  {config.requiredPassCount}/{config.criteriaCount}
                </td>
                <td className="px-6 py-4 text-xs text-slate-500 max-w-[180px] truncate" title={config.sourceFileName ?? ""}>
                  {config.sourceFileName ?? "—"}
                </td>
                <td className="px-6 py-4 text-xs text-slate-500 whitespace-nowrap">
                  <div>{formatDate(config.updatedAt ?? config.createdAt)}</div>
                  {config.updatedByName && <div className="text-slate-400">bởi {config.updatedByName}</div>}
                </td>
                <td className="px-6 py-4 text-right whitespace-nowrap">
                  <div className="inline-flex items-center gap-1">
                    <button type="button"
                      onClick={() => setEditor({ open: true, config })}
                      className="px-2 py-1 text-xs font-semibold text-slate-600 rounded-lg hover:bg-gray-100"
                      title={config.status === "Draft" ? "Chỉnh sửa" : "Xem"}
                    >
                      {config.status === "Draft" ? "Sửa" : "Xem"}
                    </button>
                    <button type="button"
                      onClick={() => setCopyFor(config)}
                      className="px-2 py-1 text-xs font-semibold text-slate-600 rounded-lg hover:bg-gray-100"
                      title="Sao chép sang học kỳ khác"
                    >
                      Sao chép
                    </button>
                    {config.status !== "Active" && (
                      <button type="button"
                        onClick={() => setConfirm({ kind: "activate", config })}
                        className="px-2 py-1 text-xs font-semibold text-green-600 rounded-lg hover:bg-green-50"
                      >
                        Kích hoạt
                      </button>
                    )}
                    {config.status === "Active" && (
                      <button type="button"
                        onClick={() => setConfirm({ kind: "deactivate", config })}
                        className="px-2 py-1 text-xs font-semibold text-red-600 rounded-lg hover:bg-red-50"
                      >
                        Ngừng
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    );
  }

  return (
    <>
      {/* Header */}
      <header className="bg-primary px-8 py-6 shrink-0 shadow-lg z-10">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 w-full">
          <div className="flex flex-col gap-1">
            <h2 className="text-white text-2xl font-bold tracking-tight flex items-center gap-2">
              <span className="material-symbols-outlined">checklist</span>{" "}
              Checklist thẩm định đề tài
            </h2>
            <p className="text-blue-100/80 text-sm">
              Quản lý bộ tiêu chí thẩm định (import từ Excel) áp dụng cho từng học kỳ.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <button type="button"
              onClick={() => setEditor({ open: true, config: null })}
              className="flex items-center gap-2 h-11 px-4 rounded-xl bg-white/15 text-white font-semibold text-sm hover:bg-white/25"
            >
              <span className="material-symbols-outlined text-[20px]">edit_note</span>{" "}
              Tạo thủ công
            </button>
            <button type="button"
              onClick={() => setImportOpen(true)}
              className="flex items-center gap-2 h-11 px-5 rounded-xl bg-white text-primary font-semibold text-sm hover:bg-blue-50 shadow"
            >
              <span className="material-symbols-outlined text-[20px]">upload_file</span>{" "}
              Import Excel
            </button>
          </div>
        </div>
      </header>

      <div className="w-full p-6 md:p-8 flex flex-col gap-6 flex-1 min-h-0 overflow-y-auto">
        {/* Filter */}
        <div className="flex items-center gap-3">
          <label htmlFor="checklist-semester-filter" className="text-sm font-semibold text-slate-600">
            Học kỳ
          </label>
          <select
            id="checklist-semester-filter"
            value={filterSemester}
            onChange={(e) => setFilterSemester(e.target.value)}
            className="h-10 rounded-lg border border-gray-200 bg-white px-3 text-sm outline-none focus:border-primary"
          >
            <option value="">Tất cả</option>
            {semesters.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </div>

        {/* Table */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            {renderConfigs()}
          </div>
        </div>
      </div>

      {/* Import modal */}
      <ChecklistImportDialog
        open={importOpen}
        semesters={semesters}
        onClose={() => setImportOpen(false)}
        onImported={() => {
          setImportOpen(false);
          refresh();
        }}
      />

      {/* Editor modal (manual create / edit / view) */}
      <ChecklistConfigEditor
        open={editor.open}
        config={editor.config}
        semesters={semesters}
        onClose={() => setEditor({ open: false, config: null })}
        onSaved={() => {
          setEditor({ open: false, config: null });
          refresh();
        }}
      />

      {/* Copy modal */}
      <CopyChecklistDialog
        source={copyFor}
        semesters={semesters}
        onClose={() => setCopyFor(null)}
        onCopied={() => {
          setCopyFor(null);
          refresh();
        }}
      />

      {/* Activate / Deactivate confirm */}
      <ConfirmDialog
        open={!!confirm}
        busy={busy}
        title={confirm?.kind === "activate" ? "Kích hoạt checklist" : "Ngừng sử dụng checklist"}
        message={
          confirm?.kind === "activate"
            ? "Checklist này sẽ được áp dụng cho các đề tài thuộc học kỳ đã chọn. Các kết quả thẩm định đã lưu trước đó sẽ không bị thay đổi."
            : "Checklist này sẽ ngừng áp dụng cho học kỳ. Các kết quả thẩm định đã lưu trước đó sẽ không bị thay đổi."
        }
        confirmLabel={confirm?.kind === "activate" ? "Kích hoạt" : "Ngừng sử dụng"}
        danger={confirm?.kind === "deactivate"}
        onCancel={() => setConfirm(null)}
        onConfirm={() => {
          if (!confirm) return;
          if (confirm.kind === "activate") handleActivate(confirm.config);
          else handleDeactivate(confirm.config);
        }}
      />
    </>
  );
}

// ── Import dialog (choose file → preview → configure threshold → confirm) ────
function ChecklistImportDialog({
  open,
  semesters,
  onClose,
  onImported,
}: Readonly<{
  open: boolean;
  semesters: ChecklistSemesterOptionDto[];
  onClose: () => void;
  onImported: () => void;
}>) {
  const { showError } = useSystemError();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [semesterId, setSemesterId] = useState<string>("");
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<ChecklistImportPreviewResponse | null>(null);
  const [previewing, setPreviewing] = useState(false);
  const [requiredPassCount, setRequiredPassCount] = useState<number>(1);
  const [importing, setImporting] = useState(false);
  const [downloading, setDownloading] = useState(false);

  useEffect(() => {
    if (!open) return;
    setSemesterId("");
    setFile(null);
    setPreview(null);
    setRequiredPassCount(1);
    if (fileInputRef.current) fileInputRef.current.value = "";
  }, [open]);

  const validCount = preview?.criteriaCount ?? 0;

  async function handleDownloadTemplate() {
    if (downloading) return;
    setDownloading(true);
    try {
      const blob = await checklistService.downloadTemplate();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "checklist-tham-dinh-mau.xlsx";
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể tải file mẫu.");
    } finally {
      setDownloading(false);
    }
  }

  async function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const chosen = e.target.files?.[0] ?? null;
    setFile(chosen);
    setPreview(null);
    if (!chosen) return;

    setPreviewing(true);
    try {
      const result = await checklistService.previewImport(chosen);
      setPreview(result);
      // Default the threshold to ~70% of valid criteria, clamped to [1, count].
      if (result.isValid && result.criteriaCount > 0) {
        setRequiredPassCount(Math.min(result.criteriaCount, Math.max(1, Math.ceil(result.criteriaCount * 0.7))));
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể đọc file. Vui lòng thử lại.");
    } finally {
      setPreviewing(false);
    }
  }

  const thresholdValid = requiredPassCount >= 1 && requiredPassCount <= validCount;
  const canImport = !!semesterId && !!file && !!preview?.isValid && thresholdValid && !importing;

  async function handleImport() {
    if (!canImport || !file) return;
    setImporting(true);
    try {
      await checklistService.importConfig(Number(semesterId), requiredPassCount, file);
      onImported();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể import checklist.");
    } finally {
      setImporting(false);
    }
  }

  return (
    <AnimatePresence>
      {open && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          onClick={onClose}
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.96, y: 10 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.96, y: 10 }}
            onClick={(e) => e.stopPropagation()}
            className="flex max-h-[92vh] w-full max-w-3xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl"
          >
            <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
              <div>
                <h3 className="text-base font-bold text-slate-900">Import checklist từ Excel</h3>
                <p className="text-xs text-slate-500">
                  Chọn học kỳ và file .xlsx. Hệ thống kiểm tra dữ liệu trước khi tạo checklist (bản nháp).
                </p>
              </div>
              <button type="button" onClick={onClose} className="flex size-8 items-center justify-center rounded-lg text-slate-400 hover:bg-gray-100">
                <span className="material-symbols-outlined text-[20px]">close</span>
              </button>
            </div>

            <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
              <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                <div>
                  <label htmlFor="checklist-import-semester" className="mb-1 block text-xs font-bold uppercase text-slate-500">
                    Học kỳ áp dụng
                  </label>
                  <select
                    id="checklist-import-semester"
                    value={semesterId}
                    onChange={(e) => setSemesterId(e.target.value)}
                    className="h-10 w-full rounded-lg border border-gray-200 bg-white px-3 text-sm outline-none focus:border-primary"
                  >
                    <option value="">— Chọn học kỳ —</option>
                    {semesters.map((s) => (
                      <option key={s.id} value={s.id}>
                        {s.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="flex items-end">
                  <button type="button"
                    onClick={handleDownloadTemplate}
                    disabled={downloading}
                    className="flex h-10 items-center gap-2 rounded-lg border border-gray-200 px-4 text-sm font-semibold text-slate-600 hover:bg-gray-50 disabled:opacity-50"
                  >
                    <span className="material-symbols-outlined text-[18px]">download</span>{" "}
                    {downloading ? "Đang tải..." : "Tải file mẫu"}
                  </button>
                </div>
              </div>

              <div>
                <label htmlFor="checklist-import-file" className="mb-1 block text-xs font-bold uppercase text-slate-500">
                  File Excel (.xlsx)
                </label>
                <input
                  id="checklist-import-file"
                  ref={fileInputRef}
                  type="file"
                  accept=".xlsx"
                  onChange={handleFileChange}
                  className="block w-full text-sm text-slate-600 file:mr-3 file:rounded-lg file:border-0 file:bg-primary/10 file:px-4 file:py-2 file:text-sm file:font-semibold file:text-primary hover:file:bg-primary/20"
                />
              </div>

              {previewing && (
                <div className="flex items-center justify-center gap-3 py-8 text-slate-400">
                  <span className="material-symbols-outlined animate-spin">progress_activity</span>
                  <span className="text-sm">Đang đọc file...</span>
                </div>
              )}

              {preview && !previewing && (
                <div className="space-y-3">
                  {preview.errors.length > 0 && (
                    <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-700">
                      <p className="mb-1 font-bold">File có {preview.errors.length} lỗi cần sửa:</p>
                      <ul className="list-disc space-y-0.5 pl-4">
                        {preview.errors.map((err) => (
                          <li key={err}>{err}</li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {preview.rows.length > 0 && (
                    <div className="rounded-lg border border-gray-200 overflow-hidden">
                      <div className="max-h-64 overflow-y-auto">
                        <table className="w-full text-left text-xs">
                          <thead className="sticky top-0 bg-gray-50">
                            <tr className="border-b border-gray-100 text-[10px] uppercase text-slate-500">
                              <th className="px-3 py-2">#</th>
                              <th className="px-3 py-2">Tên tiêu chí</th>
                              <th className="px-3 py-2">Lỗi</th>
                            </tr>
                          </thead>
                          <tbody className="divide-y divide-gray-100">
                            {preview.rows.map((row) => (
                              <tr key={row.rowNumber} className={row.errors.length > 0 ? "bg-red-50/40" : ""}>
                                <td className="px-3 py-2 text-slate-400">{row.order}</td>
                                <td className="px-3 py-2">
                                  <div className="font-semibold text-slate-700">{row.titleVi || <span className="text-red-500">(trống)</span>}</div>
                                  {row.titleEn && <div className="text-slate-400">{row.titleEn}</div>}
                                </td>
                                <td className="px-3 py-2 text-red-600">{row.errors.join("; ")}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}

                  {preview.isValid && (
                    <div className="flex flex-wrap items-center gap-3 rounded-lg border border-green-200 bg-green-50 p-3">
                      <span className="text-sm font-semibold text-green-700">
                        Hợp lệ • {preview.criteriaCount} tiêu chí
                      </span>
                      <div className="flex items-center gap-2">
                        <label htmlFor="checklist-import-threshold" className="text-xs font-semibold text-slate-600">
                          Số tiêu chí tối thiểu cần đạt:
                        </label>
                        <input
                          id="checklist-import-threshold"
                          type="number"
                          min={1}
                          max={validCount}
                          value={requiredPassCount}
                          onChange={(e) => setRequiredPassCount(Number(e.target.value))}
                          className="h-8 w-20 rounded-lg border border-gray-200 px-2 text-sm outline-none focus:border-primary"
                        />
                        <span className="text-xs text-slate-500">/ {validCount}</span>
                      </div>
                      {!thresholdValid && (
                        <span className="text-xs font-semibold text-red-600">
                          Phải từ 1 đến {validCount}.
                        </span>
                      )}
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className="flex items-center justify-end gap-2 border-t border-gray-200 px-6 py-4">
              <button type="button" onClick={onClose} className="h-10 rounded-xl border border-gray-200 px-4 text-sm font-semibold text-slate-700 hover:bg-gray-50">
                Huỷ
              </button>
              <button type="button"
                onClick={handleImport}
                disabled={!canImport}
                className="flex h-10 items-center justify-center gap-2 rounded-xl bg-primary px-5 text-sm font-semibold text-white hover:bg-primary-dark disabled:opacity-50"
              >
                {importing ? (
                  <>
                    <span className="size-4 animate-spin rounded-full border-2 border-white border-t-transparent" />{" "}
                    Đang import...
                  </>
                ) : (
                  "Xác nhận tạo checklist"
                )}
              </button>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

// ── Editor (manual create / edit / view) ─────────────────────────────────────
type EditorRow = { key: string; titleVi: string; titleEn: string; description: string };

let rowKeySeq = 0;
const nextKey = () => `row-${rowKeySeq++}`;

function toRows(
  criteria: { titleVi: string; titleEn: string; description: string | null }[],
): EditorRow[] {
  return criteria.map((c) => ({
    key: nextKey(),
    titleVi: c.titleVi,
    titleEn: c.titleEn,
    description: c.description ?? "",
  }));
}

function ChecklistConfigEditor({
  open,
  config,
  semesters,
  onClose,
  onSaved,
}: Readonly<{
  open: boolean;
  config: ChecklistConfigDto | null;
  semesters: ChecklistSemesterOptionDto[];
  onClose: () => void;
  onSaved: () => void;
}>) {
  const { showError } = useSystemError();
  const isEdit = !!config;
  const readOnly = !!config && config.status !== "Draft";

  let editorTitle = "Tạo checklist thủ công";
  if (config) editorTitle = readOnly ? "Xem checklist" : "Chỉnh sửa checklist";

  const [semesterId, setSemesterId] = useState<string>("");
  const [rows, setRows] = useState<EditorRow[]>([]);
  const [requiredPassCount, setRequiredPassCount] = useState<number>(1);
  const [saving, setSaving] = useState(false);
  const [prefilling, setPrefilling] = useState(false);

  useEffect(() => {
    if (!open) return;
    if (config) {
      setSemesterId(String(config.semesterId));
      setRows(toRows(config.criteria));
      setRequiredPassCount(config.requiredPassCount);
    } else {
      // New checklist: pull the default criteria (with scores) from the backend (not hard-coded here).
      setSemesterId("");
      setRows([]);
      setRequiredPassCount(1);
      setPrefilling(true);
      checklistService
        .getDefaultCriteria()
        .then((defaults) => {
          setRows(toRows(defaults));
          setRequiredPassCount(Math.min(defaults.length, Math.max(1, Math.ceil(defaults.length * 0.7))));
        })
        .catch((err) => showError(err instanceof Error ? err.message : "Không thể tải tiêu chí mặc định."))
        .finally(() => setPrefilling(false));
    }
  }, [open, config, showError]);

  function updateRow(key: string, patch: Partial<EditorRow>) {
    setRows((prev) => prev.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  }

  function move(index: number, dir: -1 | 1) {
    setRows((prev) => {
      const next = [...prev];
      const target = index + dir;
      if (target < 0 || target >= next.length) return prev;
      [next[index], next[target]] = [next[target], next[index]];
      return next;
    });
  }

  function addRow() {
    setRows((prev) => [...prev, { key: nextKey(), titleVi: "", titleEn: "", description: "" }]);
  }

  function removeRow(key: string) {
    setRows((prev) => (prev.length <= 1 ? prev : prev.filter((r) => r.key !== key)));
  }

  function validate(): string | null {
    if (!isEdit && !semesterId) return "Vui lòng chọn học kỳ.";
    if (rows.length < 1) return "Checklist phải có ít nhất 1 tiêu chí.";
    for (const [i, r] of rows.entries()) {
      if (!r.titleVi.trim()) return `Tiêu chí ${i + 1}: tên (tiếng Việt) không được để trống.`;
    }
    if (requiredPassCount < 1 || requiredPassCount > rows.length)
      return `Số tiêu chí tối thiểu cần đạt phải từ 1 đến ${rows.length}.`;
    return null;
  }

  async function handleSave() {
    if (saving || readOnly) return;
    const validationError = validate();
    if (validationError) {
      showError(validationError);
      return;
    }
    const criteria: ChecklistCriterionInput[] = rows.map((r) => ({
      titleVi: r.titleVi.trim(),
      titleEn: r.titleEn.trim(),
      description: r.description.trim() || null,
    }));

    setSaving(true);
    try {
      if (config) {
        await checklistService.updateConfig(config.id, { criteria, requiredPassCount });
      } else {
        await checklistService.createConfig({ semesterId: Number(semesterId), criteria, requiredPassCount });
      }
      onSaved();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể lưu checklist.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <AnimatePresence>
      {open && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          onClick={onClose}
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.96, y: 10 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.96, y: 10 }}
            onClick={(e) => e.stopPropagation()}
            className="flex max-h-[92vh] w-full max-w-4xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl"
          >
            <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
              <div>
                <h3 className="text-base font-bold text-slate-900">{editorTitle}</h3>
                <p className="text-xs text-slate-500">
                  Cấu hình tiêu chí, điểm tối đa/điểm đạt và số tiêu chí tối thiểu cần đạt.
                </p>
              </div>
              <button type="button" onClick={onClose} className="flex size-8 items-center justify-center rounded-lg text-slate-400 hover:bg-gray-100">
                <span className="material-symbols-outlined text-[20px]">close</span>
              </button>
            </div>

            <div className="flex-1 overflow-y-auto px-6 py-4 space-y-3">
              {readOnly && (
                <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-700">
                  Checklist đã áp dụng không thể chỉnh sửa trực tiếp. Hãy dùng "Sao chép" để tạo phiên bản mới.
                </div>
              )}

              <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                {!isEdit && (
                  <div>
                    <label htmlFor="checklist-editor-semester" className="mb-1 block text-xs font-bold uppercase text-slate-500">
                      Học kỳ áp dụng
                    </label>
                    <select
                      id="checklist-editor-semester"
                      value={semesterId}
                      onChange={(e) => setSemesterId(e.target.value)}
                      className="h-10 w-full rounded-lg border border-gray-200 bg-white px-3 text-sm outline-none focus:border-primary"
                    >
                      <option value="">— Chọn học kỳ —</option>
                      {semesters.map((s) => (
                        <option key={s.id} value={s.id}>
                          {s.name}
                        </option>
                      ))}
                    </select>
                  </div>
                )}
                <div>
                  <label htmlFor="checklist-editor-threshold" className="mb-1 block text-xs font-bold uppercase text-slate-500">
                    Số tiêu chí tối thiểu cần đạt
                  </label>
                  <input
                    id="checklist-editor-threshold"
                    type="number"
                    min={1}
                    max={rows.length}
                    value={requiredPassCount}
                    disabled={readOnly}
                    onChange={(e) => setRequiredPassCount(Number(e.target.value))}
                    className="h-10 w-full rounded-lg border border-gray-200 bg-white px-3 text-sm outline-none focus:border-primary disabled:opacity-70"
                  />
                </div>
              </div>

              {prefilling ? (
                <div className="flex items-center justify-center gap-3 py-10 text-slate-400">
                  <span className="material-symbols-outlined animate-spin">progress_activity</span>
                  <span className="text-sm">Đang tải tiêu chí mặc định...</span>
                </div>
              ) : (
                <div className="space-y-2">
                  {rows.map((row, index) => (
                    <div key={row.key} className="rounded-xl border border-gray-200 p-3">
                      <div className="flex items-start gap-2">
                        <span className="mt-2 text-xs font-bold text-slate-400 w-5">{index + 1}.</span>
                        <div className="flex-1 space-y-2">
                          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                            <input
                              value={row.titleVi}
                              disabled={readOnly}
                              onChange={(e) => updateRow(row.key, { titleVi: e.target.value })}
                              placeholder="Tên tiêu chí (tiếng Việt)"
                              className="h-9 rounded-lg border border-gray-200 bg-gray-50 px-3 text-sm outline-none focus:border-primary focus:bg-white disabled:opacity-70"
                            />
                            <input
                              value={row.titleEn}
                              disabled={readOnly}
                              onChange={(e) => updateRow(row.key, { titleEn: e.target.value })}
                              placeholder="Tên tiêu chí (tiếng Anh)"
                              className="h-9 rounded-lg border border-gray-200 bg-gray-50 px-3 text-sm outline-none focus:border-primary focus:bg-white disabled:opacity-70"
                            />
                          </div>
                          <textarea
                            value={row.description}
                            disabled={readOnly}
                            onChange={(e) => updateRow(row.key, { description: e.target.value })}
                            placeholder="Mô tả / câu hỏi thẩm định"
                            rows={2}
                            className="w-full resize-none rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-sm outline-none focus:border-primary focus:bg-white disabled:opacity-70"
                          />
                        </div>
                        {!readOnly && (
                          <div className="flex flex-col gap-1">
                            <button type="button"
                              onClick={() => move(index, -1)}
                              disabled={index === 0}
                              className="flex size-7 items-center justify-center rounded-lg text-slate-400 hover:bg-gray-100 disabled:opacity-30"
                              title="Lên"
                            >
                              <span className="material-symbols-outlined text-[18px]">arrow_upward</span>
                            </button>
                            <button type="button"
                              onClick={() => move(index, 1)}
                              disabled={index === rows.length - 1}
                              className="flex size-7 items-center justify-center rounded-lg text-slate-400 hover:bg-gray-100 disabled:opacity-30"
                              title="Xuống"
                            >
                              <span className="material-symbols-outlined text-[18px]">arrow_downward</span>
                            </button>
                            <button type="button"
                              onClick={() => removeRow(row.key)}
                              disabled={rows.length <= 1}
                              className="flex size-7 items-center justify-center rounded-lg text-red-400 hover:bg-red-50 disabled:opacity-30"
                              title="Xoá"
                            >
                              <span className="material-symbols-outlined text-[18px]">delete</span>
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                  {!readOnly && (
                    <button type="button"
                      onClick={addRow}
                      className="flex w-full items-center justify-center gap-2 rounded-xl border border-dashed border-gray-300 py-3 text-sm font-semibold text-slate-500 hover:bg-gray-50"
                    >
                      <span className="material-symbols-outlined text-[18px]">add</span>{" "}
                      Thêm tiêu chí ({rows.length})
                    </button>
                  )}
                </div>
              )}
            </div>

            <div className="flex items-center justify-between border-t border-gray-200 px-6 py-4">
              <span className="text-sm font-semibold text-slate-600">{rows.length} tiêu chí</span>
              <div className="flex gap-2">
                <button type="button" onClick={onClose} className="h-10 rounded-xl border border-gray-200 px-4 text-sm font-semibold text-slate-700 hover:bg-gray-50">
                  {readOnly ? "Đóng" : "Huỷ"}
                </button>
                {!readOnly && (
                  <button type="button"
                    onClick={handleSave}
                    disabled={saving || prefilling}
                    className="flex h-10 items-center justify-center gap-2 rounded-xl bg-primary px-5 text-sm font-semibold text-white hover:bg-primary-dark disabled:opacity-50"
                  >
                    {saving ? (
                      <>
                        <span className="size-4 animate-spin rounded-full border-2 border-white border-t-transparent" />{" "}
                        Đang lưu...
                      </>
                    ) : (
                      "Lưu (bản nháp)"
                    )}
                  </button>
                )}
              </div>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

// ── Copy dialog ─────────────────────────────────────────────────────────────
function CopyChecklistDialog({
  source,
  semesters,
  onClose,
  onCopied,
}: Readonly<{
  source: ChecklistConfigDto | null;
  semesters: ChecklistSemesterOptionDto[];
  onClose: () => void;
  onCopied: () => void;
}>) {
  const { showError } = useSystemError();
  const [target, setTarget] = useState<string>("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setTarget("");
  }, [source]);

  async function handleCopy() {
    if (!source || busy) return;
    if (!target) {
      showError("Vui lòng chọn học kỳ đích.");
      return;
    }
    setBusy(true);
    try {
      await checklistService.copyConfig(source.id, { targetSemesterId: Number(target) });
      onCopied();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể sao chép checklist.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AnimatePresence>
      {source && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          onClick={onClose}
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.96 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.96 }}
            onClick={(e) => e.stopPropagation()}
            className="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl"
          >
            <h3 className="mb-1 text-base font-bold text-slate-900">Sao chép checklist</h3>
            <p className="mb-4 text-sm text-slate-500">
              Tạo bản nháp mới từ checklist "{source.semesterName} v{source.version}" cho học kỳ đích.
            </p>
            <select
              value={target}
              onChange={(e) => setTarget(e.target.value)}
              className="mb-4 h-10 w-full rounded-lg border border-gray-200 bg-white px-3 text-sm outline-none focus:border-primary"
            >
              <option value="">— Chọn học kỳ đích —</option>
              {semesters.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
            <div className="flex justify-end gap-2">
              <button type="button" onClick={onClose} className="h-10 rounded-xl border border-gray-200 px-4 text-sm font-semibold text-slate-700 hover:bg-gray-50">
                Huỷ
              </button>
              <button type="button"
                onClick={handleCopy}
                disabled={busy}
                className="h-10 rounded-xl bg-primary px-5 text-sm font-semibold text-white hover:bg-primary-dark disabled:opacity-50"
              >
                {busy ? "Đang sao chép..." : "Sao chép"}
              </button>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

// ── Confirm dialog ──────────────────────────────────────────────────────────
function ConfirmDialog({
  open,
  busy,
  title,
  message,
  confirmLabel,
  danger,
  onCancel,
  onConfirm,
}: Readonly<{
  open: boolean;
  busy: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  danger?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}>) {
  return (
    <AnimatePresence>
      {open && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          onClick={onCancel}
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.96 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.96 }}
            onClick={(e) => e.stopPropagation()}
            className="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl"
          >
            <h3 className="mb-2 text-base font-bold text-slate-900">{title}</h3>
            <p className="mb-5 text-sm text-slate-600">{message}</p>
            <div className="flex justify-end gap-2">
              <button type="button" onClick={onCancel} className="h-10 rounded-xl border border-gray-200 px-4 text-sm font-semibold text-slate-700 hover:bg-gray-50">
                Huỷ
              </button>
              <button type="button"
                onClick={onConfirm}
                disabled={busy}
                className={`h-10 rounded-xl px-5 text-sm font-semibold text-white disabled:opacity-50 ${
                  danger ? "bg-red-600 hover:bg-red-700" : "bg-primary hover:bg-primary-dark"
                }`}
              >
                {busy ? "Đang xử lý..." : confirmLabel}
              </button>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
