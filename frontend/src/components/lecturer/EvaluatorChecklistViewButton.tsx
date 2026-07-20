import { useState } from "react";
import { checklistService } from "@/lib";
import type { ProjectChecklistResponse } from "@/types";
import { EvaluationChecklistModal } from "./EvaluationChecklistModal";

interface EvaluatorChecklistViewButtonProps {
  projectId: string;
  evaluatorId: string;
  evaluatorName?: string;
  className?: string;
}

/**
 * Department-Head read-only view of one evaluator's checklist for a project. Self-contained: owns its
 * own open state + fetch, so it can be dropped per evaluator on the needs-decision and history screens.
 */
export function EvaluatorChecklistViewButton({
  projectId,
  evaluatorId,
  evaluatorName,
  className,
}: EvaluatorChecklistViewButtonProps) {
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [checklist, setChecklist] = useState<ProjectChecklistResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleOpen() {
    setOpen(true);
    setLoading(true);
    setError(null);
    try {
      const data = await checklistService.getEvaluatorChecklist(projectId, evaluatorId);
      setChecklist(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tải checklist.");
      setChecklist(null);
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <button
        onClick={handleOpen}
        className={
          className ??
          "inline-flex items-center gap-1 text-xs font-semibold text-primary hover:underline"
        }
        title={evaluatorName ? `Xem checklist của ${evaluatorName}` : "Xem checklist"}
      >
        <span className="material-symbols-outlined text-[16px]">checklist</span>
        Xem checklist
      </button>

      {open && error && !loading ? (
        // Minimal error surface (the modal itself only renders when there is a checklist to show).
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          onClick={() => setOpen(false)}
        >
          <div
            className="w-full max-w-sm rounded-2xl bg-white p-6 text-center shadow-2xl"
            onClick={(e) => e.stopPropagation()}
          >
            <span className="material-symbols-outlined text-3xl text-amber-400">report</span>
            <p className="mt-2 text-sm font-medium text-slate-600">{error}</p>
            <button
              onClick={() => setOpen(false)}
              className="mt-4 h-9 rounded-xl border border-gray-200 px-4 text-sm font-semibold text-slate-700 hover:bg-gray-50"
            >
              Đóng
            </button>
          </div>
        </div>
      ) : (
        <EvaluationChecklistModal
          open={open && !error}
          loading={loading}
          readOnly
          checklist={checklist}
          onClose={() => setOpen(false)}
          onSave={async () => {}}
        />
      )}
    </>
  );
}
