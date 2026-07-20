import { useEffect, useMemo, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import type { ChecklistScoreItemInput, ProjectChecklistResponse } from "@/types";

interface EvaluationChecklistModalProps {
  open: boolean;
  loading: boolean;
  readOnly?: boolean;
  checklist: ProjectChecklistResponse | null;
  onClose: () => void;
  /** Persists the per-criterion scores + comments + note. Should throw on failure. */
  onSave: (items: ChecklistScoreItemInput[], note: string) => Promise<void>;
}

/** Parses a raw score input; returns null when blank, NaN when invalid. */
function parseScore(raw: string): number | null {
  const trimmed = raw.trim();
  if (trimmed === "") return null;
  const value = Number(trimmed.replace(",", "."));
  return Number.isFinite(value) ? value : NaN;
}

export function EvaluationChecklistModal({
  open,
  loading,
  readOnly = false,
  checklist,
  onClose,
  onSave,
}: EvaluationChecklistModalProps) {
  const [scores, setScores] = useState<Record<string, string>>({});
  const [comments, setComments] = useState<Record<string, string>>({});
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Seed local state from the saved server state whenever the modal opens (preserves saved scores/comments).
  useEffect(() => {
    if (!open || !checklist) return;
    const nextScores: Record<string, string> = {};
    const nextComments: Record<string, string> = {};
    for (const item of checklist.items) {
      nextScores[item.criterionId] = item.score != null ? String(item.score) : "";
      nextComments[item.criterionId] = item.comment ?? "";
    }
    setScores(nextScores);
    setComments(nextComments);
    setNote(checklist.evaluatorNote ?? "");
    setError(null);
  }, [open, checklist]);

  const sortedItems = useMemo(
    () => (checklist ? [...checklist.items].sort((a, b) => a.order - b.order) : []),
    [checklist],
  );

  const required = checklist?.requiredPassCount ?? 0;
  const total = checklist?.totalCriteria ?? sortedItems.length;

  // Local display-only counts (the backend recomputes the authoritative values on save).
  const { scoredCount, passedCount } = useMemo(() => {
    let scored = 0;
    let passed = 0;
    for (const item of sortedItems) {
      const value = parseScore(scores[item.criterionId] ?? "");
      if (value != null && !Number.isNaN(value)) {
        scored += 1;
        if (value >= item.passScore) passed += 1;
      }
    }
    return { scoredCount: scored, passedCount: passed };
  }, [sortedItems, scores]);

  const meetsThreshold = required > 0 && passedCount >= required;

  function setScore(criterionId: string, value: string) {
    if (readOnly) return;
    setScores((prev) => ({ ...prev, [criterionId]: value }));
  }

  function setComment(criterionId: string, value: string) {
    if (readOnly) return;
    setComments((prev) => ({ ...prev, [criterionId]: value }));
  }

  function validate(): string | null {
    for (const item of sortedItems) {
      const value = parseScore(scores[item.criterionId] ?? "");
      if (value === null) continue;
      if (Number.isNaN(value)) return `Tiêu chí "${item.titleVi}": điểm không hợp lệ.`;
      if (value < 0) return `Tiêu chí "${item.titleVi}": điểm không được âm.`;
      if (value > item.maxScore)
        return `Tiêu chí "${item.titleVi}": điểm không được vượt quá ${item.maxScore}.`;
    }
    return null;
  }

  async function handleSave() {
    if (saving) return; // guard double-click
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const items: ChecklistScoreItemInput[] = sortedItems.map((item) => {
        const value = parseScore(scores[item.criterionId] ?? "");
        return {
          criterionId: item.criterionId,
          score: value === null || Number.isNaN(value) ? null : value,
          comment: (comments[item.criterionId] ?? "").trim() || null,
        };
      });
      await onSave(items, note.trim());
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu checklist. Vui lòng thử lại.");
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
            className="flex max-h-[90vh] w-full max-w-3xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl"
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

            {/* Progress bar */}
            {checklist?.hasActiveConfig && !loading && (
              <div className="flex flex-wrap items-center gap-x-6 gap-y-1 border-b border-gray-100 bg-gray-50/60 px-6 py-2 text-xs">
                <span className="text-slate-500">
                  Đã chấm: <span className="font-bold text-slate-700">{scoredCount}/{total}</span>
                </span>
                <span className="text-slate-500">
                  Đạt: <span className="font-bold text-green-600">{passedCount}</span>
                </span>
                <span className="text-slate-500">
                  Chưa đạt: <span className="font-bold text-amber-600">{scoredCount - passedCount}</span>
                </span>
                <span className="text-slate-500">
                  Cần đạt tối thiểu: <span className="font-bold text-slate-700">{required}</span>
                </span>
              </div>
            )}

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
              ) : sortedItems.length === 0 ? (
                <div className="flex flex-col items-center gap-2 py-16 text-center text-slate-400">
                  <span className="material-symbols-outlined text-4xl">inbox</span>
                  <p className="text-sm font-medium">Checklist chưa có tiêu chí nào.</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {sortedItems.map((item) => {
                    const value = parseScore(scores[item.criterionId] ?? "");
                    const hasScore = value != null && !Number.isNaN(value);
                    const isPassed = hasScore && value >= item.passScore;
                    return (
                      <div
                        key={item.criterionId}
                        className={`rounded-xl border p-3 transition-colors ${
                          !hasScore
                            ? "border-gray-200"
                            : isPassed
                              ? "border-green-200 bg-green-50/50"
                              : "border-amber-200 bg-amber-50/40"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="flex-1">
                            <div className="flex flex-wrap items-center gap-2">
                              <span className="text-xs font-bold text-slate-400">{item.order}.</span>
                              <span className="text-sm font-semibold text-slate-800">{item.titleVi}</span>
                              {item.titleEn && <span className="text-xs text-slate-400">— {item.titleEn}</span>}
                            </div>
                            {item.description && <p className="mt-0.5 text-xs text-slate-500">{item.description}</p>}
                            <p className="mt-1 text-[11px] text-slate-400">
                              Điểm tối đa: <span className="font-semibold text-slate-500">{item.maxScore}</span> • Điểm
                              đạt: <span className="font-semibold text-slate-500">{item.passScore}</span>
                            </p>
                          </div>
                          <span
                            className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-bold ${
                              !hasScore
                                ? "bg-gray-100 text-slate-500"
                                : isPassed
                                  ? "bg-green-100 text-green-600"
                                  : "bg-amber-100 text-amber-600"
                            }`}
                          >
                            {!hasScore ? "Chưa chấm" : isPassed ? "Đạt" : "Chưa đạt"}
                          </span>
                        </div>

                        <div className="mt-2 flex flex-col gap-2 sm:flex-row">
                          <div className="sm:w-40">
                            <label className="mb-1 block text-[11px] font-bold uppercase text-slate-400">Điểm</label>
                            <input
                              type="number"
                              min={0}
                              max={item.maxScore}
                              step="0.5"
                              value={scores[item.criterionId] ?? ""}
                              disabled={readOnly}
                              onChange={(e) => setScore(item.criterionId, e.target.value)}
                              placeholder={`0 - ${item.maxScore}`}
                              className="h-9 w-full rounded-lg border border-gray-200 bg-gray-50 px-3 text-sm outline-none focus:border-primary focus:bg-white disabled:opacity-70"
                            />
                          </div>
                          <div className="flex-1">
                            <label className="mb-1 block text-[11px] font-bold uppercase text-slate-400">
                              Nhận xét
                            </label>
                            <input
                              type="text"
                              value={comments[item.criterionId] ?? ""}
                              disabled={readOnly}
                              maxLength={2000}
                              onChange={(e) => setComment(item.criterionId, e.target.value)}
                              placeholder="Nhận xét cho tiêu chí này..."
                              className="h-9 w-full rounded-lg border border-gray-200 bg-gray-50 px-3 text-sm outline-none focus:border-primary focus:bg-white disabled:opacity-70"
                            />
                          </div>
                        </div>
                      </div>
                    );
                  })}

                  {!readOnly && (
                    <div className="pt-1">
                      <label className="mb-1 block text-xs font-bold uppercase text-slate-500">
                        Nhận xét tổng quát (tuỳ chọn)
                      </label>
                      <textarea
                        value={note}
                        onChange={(e) => setNote(e.target.value)}
                        rows={2}
                        maxLength={2000}
                        placeholder="Nhận xét chung cho lần thẩm định này..."
                        className="w-full resize-none rounded-xl border border-gray-200 bg-gray-50 px-3 py-2 text-sm outline-none focus:border-primary focus:bg-white focus:ring-2 focus:ring-primary/20"
                      />
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Footer */}
            {checklist?.hasActiveConfig && sortedItems.length > 0 && (
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
                      Cần đạt ít nhất {required} tiêu chí để được phép duyệt đề tài.
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
