import { useEffect, useMemo, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import type { ProjectChecklistResponse } from "@/types";

interface EvaluationChecklistModalProps {
  open: boolean;
  loading: boolean;
  readOnly?: boolean;
  checklist: ProjectChecklistResponse | null;
  onClose: () => void;
  /** Persists the passed criterion ids + note. Should throw on failure. */
  onSave: (passedCriterionIds: string[], note: string) => Promise<void>;
}

export function EvaluationChecklistModal({
  open,
  loading,
  readOnly = false,
  checklist,
  onClose,
  onSave,
}: EvaluationChecklistModalProps) {
  const [passed, setPassed] = useState<Set<string>>(new Set());
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Seed local state from the saved server state whenever the modal opens (preserves saved selections).
  useEffect(() => {
    if (!open || !checklist) return;
    setPassed(new Set(checklist.items.filter((i) => i.isPassed).map((i) => i.criterionId)));
    setNote(checklist.evaluatorNote ?? "");
    setError(null);
  }, [open, checklist]);

  const required = checklist?.requiredPassCount ?? 7;
  const total = checklist?.totalCriteria ?? checklist?.items.length ?? 10;
  const passedCount = passed.size;
  const meetsThreshold = passedCount >= required;

  const sortedItems = useMemo(
    () => (checklist ? [...checklist.items].sort((a, b) => a.order - b.order) : []),
    [checklist],
  );

  function toggle(criterionId: string) {
    if (readOnly) return;
    setPassed((prev) => {
      const next = new Set(prev);
      if (next.has(criterionId)) next.delete(criterionId);
      else next.add(criterionId);
      return next;
    });
  }

  async function handleSave() {
    if (saving) return; // guard double-click
    setSaving(true);
    setError(null);
    try {
      await onSave([...passed], note.trim());
      onClose();
    } catch {
      setError("Không thể lưu checklist. Vui lòng thử lại.");
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
            className="flex max-h-[90vh] w-full max-w-2xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl"
          >
            {/* Header */}
            <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
              <div className="flex items-center gap-2">
                <span className="material-symbols-outlined text-primary">checklist</span>
                <h3 className="text-base font-bold text-slate-900">Checklist thẩm định đề tài</h3>
              </div>
              <button
                onClick={onClose}
                className="flex size-8 items-center justify-center rounded-lg text-slate-400 hover:bg-gray-100"
              >
                <span className="material-symbols-outlined text-[20px]">close</span>
              </button>
            </div>

            {/* Body */}
            <div className="flex-1 overflow-y-auto px-6 py-4">
              {loading ? (
                <div className="flex items-center justify-center gap-3 py-16 text-slate-400">
                  <span className="material-symbols-outlined animate-spin">progress_activity</span>
                  <span className="text-sm">Đang tải checklist...</span>
                </div>
              ) : !checklist?.hasActiveConfig ? (
                <div className="flex flex-col items-center gap-2 py-16 text-center">
                  <span className="material-symbols-outlined text-4xl text-amber-400">report</span>
                  <p className="max-w-sm text-sm font-medium text-slate-600">
                    Học kỳ này chưa được cấu hình checklist thẩm định. Vui lòng liên hệ Trưởng bộ môn.
                  </p>
                </div>
              ) : (
                <div className="space-y-2">
                  {sortedItems.map((item) => {
                    const checked = passed.has(item.criterionId);
                    return (
                      <label
                        key={item.criterionId}
                        className={`flex cursor-pointer items-start gap-3 rounded-xl border p-3 transition-colors ${
                          checked ? "border-green-200 bg-green-50/60" : "border-gray-200 hover:bg-gray-50"
                        } ${readOnly ? "cursor-default" : ""}`}
                      >
                        <input
                          type="checkbox"
                          checked={checked}
                          disabled={readOnly}
                          onChange={() => toggle(item.criterionId)}
                          className="mt-0.5 size-4 accent-primary"
                        />
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-bold text-slate-400">{item.order}.</span>
                            <span className="text-sm font-semibold text-slate-800">{item.titleVi}</span>
                            {item.titleEn && (
                              <span className="text-xs text-slate-400">— {item.titleEn}</span>
                            )}
                          </div>
                          {item.description && (
                            <p className="mt-0.5 text-xs text-slate-500">{item.description}</p>
                          )}
                        </div>
                        <span
                          className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-bold ${
                            checked
                              ? "bg-green-100 text-green-600"
                              : "bg-gray-100 text-slate-500"
                          }`}
                        >
                          {checked ? "Đạt" : "Chưa đạt"}
                        </span>
                      </label>
                    );
                  })}

                  {!readOnly && (
                    <div className="pt-2">
                      <label className="mb-1 block text-xs font-bold uppercase text-slate-500">
                        Ghi chú (tuỳ chọn)
                      </label>
                      <textarea
                        value={note}
                        onChange={(e) => setNote(e.target.value)}
                        rows={2}
                        maxLength={2000}
                        placeholder="Ghi chú thẩm định cho checklist..."
                        className="w-full resize-none rounded-xl border border-gray-200 bg-gray-50 px-3 py-2 text-sm outline-none focus:border-primary focus:bg-white focus:ring-2 focus:ring-primary/20"
                      />
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Footer */}
            {checklist?.hasActiveConfig && (
              <div className="border-t border-gray-200 px-6 py-4">
                {error && (
                  <div className="mb-3 rounded-lg border border-red-200 bg-red-50 p-2 text-xs text-red-700">
                    {error}
                  </div>
                )}
                <div className="flex items-center justify-between gap-4">
                  <div className="text-sm">
                    <span className={`font-bold ${meetsThreshold ? "text-green-600" : "text-slate-700"}`}>
                      {passedCount}/{total} tiêu chí đạt
                    </span>
                    <p className={`text-xs ${meetsThreshold ? "text-green-600" : "text-amber-600"}`}>
                      Cần ít nhất {required} tiêu chí đạt để duyệt đề tài.
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={onClose}
                      className="h-10 rounded-xl border border-gray-200 px-4 text-sm font-semibold text-slate-700 hover:bg-gray-50"
                    >
                      {readOnly ? "Đóng" : "Huỷ"}
                    </button>
                    {!readOnly && (
                      <button
                        onClick={handleSave}
                        disabled={saving}
                        className="flex h-10 items-center justify-center gap-2 rounded-xl bg-primary px-5 text-sm font-semibold text-white shadow-lg shadow-primary/20 hover:bg-primary-dark disabled:opacity-50"
                      >
                        {saving ? (
                          <>
                            <span className="size-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                            Đang lưu...
                          </>
                        ) : (
                          "Lưu checklist"
                        )}
                      </button>
                    )}
                  </div>
                </div>
              </div>
            )}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
