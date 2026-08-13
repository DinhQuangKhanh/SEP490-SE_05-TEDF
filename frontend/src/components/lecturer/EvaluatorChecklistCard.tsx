import { useMemo } from "react";
import type { ProjectChecklistResponse } from "@/types";

interface EvaluatorChecklistCardProps {
  evaluatorName: string;
  /** The evaluator's overall verdict ("Approved" | "NeedsModification" | "Rejected" | null). */
  verdict: string | null;
  /** The evaluator's overall feedback for the topic. */
  feedback: string | null;
  checklist: ProjectChecklistResponse;
}

const VERDICT_DISPLAY: Record<string, { label: string; colors: string }> = {
  Approved: { label: "Đã duyệt", colors: "bg-green-50 text-green-600 border-green-200" },
  NeedsModification: { label: "Cần chỉnh sửa", colors: "bg-amber-50 text-amber-600 border-amber-200" },
  Rejected: { label: "Từ chối", colors: "bg-red-50 text-red-600 border-red-200" },
};

/** Per-criterion pass/fail badge (extracted so there is no nested ternary in the markup). */
function criterionBadge(isPassed: boolean): { label: string; colors: string } {
  if (isPassed) return { label: "Đạt", colors: "bg-green-100 text-green-600" };
  return { label: "Không đạt", colors: "bg-amber-100 text-amber-600" };
}

/**
 * Read-only display of one evaluator's checklist result for a project (Department-Head review). Shows the
 * evaluator, overall verdict + feedback, the checklist version, per-criterion scores/pass state/comments,
 * and the passed-criteria total. Pure display — no inputs, no mutation.
 */
export function EvaluatorChecklistCard({
  evaluatorName,
  verdict,
  feedback,
  checklist,
}: Readonly<EvaluatorChecklistCardProps>) {
  const verdictInfo = verdict ? VERDICT_DISPLAY[verdict] : undefined;
  const meetsThreshold = checklist.passedCount >= checklist.requiredPassCount;

  const sortedItems = useMemo(
    () => [...checklist.items].sort((a, b) => a.order - b.order),
    [checklist.items],
  );

  return (
    <div className="rounded-xl border border-gray-200 bg-white">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-gray-100 px-4 py-3">
        <div className="flex items-center gap-2">
          <div className="flex size-8 items-center justify-center rounded-full bg-primary/10 shrink-0">
            <span className="material-symbols-outlined text-primary text-[16px]">person</span>
          </div>
          <div>
            <p className="text-sm font-bold text-slate-800">{evaluatorName}</p>
            {checklist.version != null && (
              <p className="text-[11px] text-slate-400">Checklist phiên bản v{checklist.version}</p>
            )}
          </div>
        </div>
        <div className="flex items-center gap-2">
          {verdictInfo && (
            <span
              className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-bold ${verdictInfo.colors}`}
            >
              {verdictInfo.label}
            </span>
          )}
          <span
            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-bold ${
              meetsThreshold ? "bg-green-100 text-green-600" : "bg-amber-100 text-amber-600"
            }`}
          >
            {checklist.passedCount}/{checklist.totalCriteria} đạt • cần ≥ {checklist.requiredPassCount}
          </span>
        </div>
      </div>

      {/* Overall feedback / note */}
      {(feedback || checklist.evaluatorNote) && (
        <div className="space-y-1 border-b border-gray-100 px-4 py-2 text-xs">
          {feedback && (
            <p className="text-slate-600">
              <span className="font-semibold text-slate-500">Phản hồi chung: </span>
              {feedback}
            </p>
          )}
          {checklist.evaluatorNote && (
            <p className="text-slate-600">
              <span className="font-semibold text-slate-500">Nhận xét tổng: </span>
              {checklist.evaluatorNote}
            </p>
          )}
        </div>
      )}

      {/* Criteria */}
      {sortedItems.length === 0 ? (
        <p className="px-4 py-4 text-center text-xs text-slate-400">Chưa có tiêu chí.</p>
      ) : (
        <div className="divide-y divide-gray-100">
          {sortedItems.map((item) => {
            const badge = criterionBadge(item.isPassed);
            return (
              <div key={item.criterionId} className="px-4 py-2.5">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-xs font-bold text-slate-400">{item.order}.</span>
                      <span className="text-sm font-semibold text-slate-800">{item.titleVi}</span>
                      {item.titleEn && <span className="text-xs text-slate-400">— {item.titleEn}</span>}
                    </div>
                    {item.description && <p className="mt-0.5 text-xs text-slate-500">{item.description}</p>}
                    {item.comment && (
                      <p className="mt-1 whitespace-pre-wrap rounded-lg bg-gray-50 px-2 py-1 text-xs text-slate-600">
                        {item.comment}
                      </p>
                    )}
                  </div>
                  <span className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-bold ${badge.colors}`}>
                    {badge.label}
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
