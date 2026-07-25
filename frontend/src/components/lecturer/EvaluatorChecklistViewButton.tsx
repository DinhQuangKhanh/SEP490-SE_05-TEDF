import { useState } from "react";
import { checklistService } from "@/lib";
import { useSystemError } from "@/contexts/SystemErrorContext";
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
}: Readonly<EvaluatorChecklistViewButtonProps>) {
  const { showError } = useSystemError();
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [checklist, setChecklist] = useState<ProjectChecklistResponse | null>(null);

  async function handleOpen() {
    setOpen(true);
    setLoading(true);
    try {
      const data = await checklistService.getEvaluatorChecklist(projectId, evaluatorId);
      setChecklist(data);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể tải checklist.");
      setOpen(false);
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <button
        type="button"
        onClick={handleOpen}
        className={
          className ?? "inline-flex items-center gap-1 text-xs font-semibold text-primary hover:underline"
        }
        title={evaluatorName ? `Xem checklist của ${evaluatorName}` : "Xem checklist"}
      >
        <span className="material-symbols-outlined text-[16px]">checklist</span>
        <span>Xem checklist</span>
      </button>

      <EvaluationChecklistModal
        open={open}
        loading={loading}
        readOnly
        checklist={checklist}
        onClose={() => setOpen(false)}
        onSave={async () => {}}
      />
    </>
  );
}
