import { useEffect, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { checklistService } from "@/lib";
import type { DepartmentProject, ProjectChecklistResponse } from "@/types";
import { EvaluatorChecklistCard } from "./EvaluatorChecklistCard";

interface DepartmentReviewDetailModalProps {
  project: DepartmentProject | null;
  onClose: () => void;
}

/**
 * Department-Head read-only detail of a project's evaluation: project header + every submitted evaluator's
 * checklist (fetched per evaluator). Refetches whenever the project changes / the modal reopens, so stale
 * data from a previously viewed project is never shown. DH cannot edit anything here.
 */
export function DepartmentReviewDetailModal({ project, onClose }: Readonly<DepartmentReviewDetailModalProps>) {
  const [loading, setLoading] = useState(false);
  const [checklists, setChecklists] = useState<Record<string, ProjectChecklistResponse>>({});
  const [error, setError] = useState<string | null>(null);

  const submittedEvaluators = project?.evaluators.filter((e) => e.hasSubmitted) ?? [];

  useEffect(() => {
    if (!project) return;
    const submitted = project.evaluators.filter((e) => e.hasSubmitted);

    // Reset first so a reopen never briefly shows the previous project's checklists.
    setChecklists({});
    setError(null);
    if (submitted.length === 0) return;

    let cancelled = false;
    setLoading(true);
    Promise.all(
      submitted.map((e) =>
        checklistService
          .getEvaluatorChecklist(project.projectId, e.evaluatorId)
          .then((cl) => ({ id: e.evaluatorId, cl }))
          .catch(() => ({ id: e.evaluatorId, cl: null as ProjectChecklistResponse | null })),
      ),
    )
      .then((results) => {
        if (cancelled) return;
        const map: Record<string, ProjectChecklistResponse> = {};
        for (const r of results) if (r.cl) map[r.id] = r.cl;
        setChecklists(map);
      })
      .catch(() => {
        if (!cancelled) setError("Không thể tải checklist của đề tài. Vui lòng thử lại.");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [project]);

  const mentorName = project?.mentors[0]?.mentorName;

  function renderBody() {
    if (error) {
      return (
        <div className="flex flex-col items-center gap-2 py-12 text-center">
          <span className="material-symbols-outlined text-4xl text-amber-400">report</span>
          <p className="text-sm font-medium text-slate-600">{error}</p>
        </div>
      );
    }
    if (loading) {
      return (
        <div className="flex items-center justify-center gap-3 py-12 text-slate-400">
          <span className="material-symbols-outlined animate-spin">progress_activity</span>
          <span className="text-sm">Đang tải checklist...</span>
        </div>
      );
    }
    if (submittedEvaluators.length === 0) {
      return (
        <div className="flex flex-col items-center gap-2 py-12 text-center text-slate-400">
          <span className="material-symbols-outlined text-4xl">inbox</span>
          <p className="text-sm font-medium">Chưa có người thẩm định nào nộp kết quả.</p>
        </div>
      );
    }
    return (
      <div className="space-y-4">
        {submittedEvaluators.map((ev) => {
          const checklist = checklists[ev.evaluatorId];
          return checklist ? (
            <EvaluatorChecklistCard
              key={ev.evaluatorId}
              evaluatorName={ev.evaluatorName}
              verdict={ev.individualResult}
              feedback={ev.feedback}
              checklist={checklist}
            />
          ) : (
            <div
              key={ev.evaluatorId}
              className="rounded-xl border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-slate-500"
            >
              <span className="font-semibold text-slate-700">{ev.evaluatorName}</span> — chưa có checklist
              chi tiết cho lần thẩm định này.
            </div>
          );
        })}
      </div>
    );
  }

  return (
    <AnimatePresence>
      {project && (
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
            {/* Header */}
            <div className="flex items-start justify-between gap-3 border-b border-gray-200 px-6 py-4">
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-mono text-xs text-slate-400">{project.projectCode}</span>
                  <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-600">
                    {project.status}
                  </span>
                </div>
                <h3 className="mt-0.5 text-base font-bold text-slate-900">{project.nameVi}</h3>
                <div className="mt-1 flex flex-wrap items-center gap-x-4 gap-y-0.5 text-xs text-slate-500">
                  <span>Học kỳ: {project.semesterName}</span>
                  {mentorName && <span>Mentor: {mentorName}</span>}
                  <span>{project.majorName}</span>
                </div>
              </div>
              <button
                type="button"
                onClick={onClose}
                className="flex size-8 shrink-0 items-center justify-center rounded-lg text-slate-400 hover:bg-gray-100"
              >
                <span className="material-symbols-outlined text-[20px]">close</span>
              </button>
            </div>

            {/* Body */}
            <div className="flex-1 overflow-y-auto px-6 py-4">
              <h4 className="mb-3 text-xs font-bold uppercase text-slate-500">
                Checklist thẩm định của từng người ({submittedEvaluators.length})
              </h4>
              {renderBody()}
            </div>

            {/* Footer */}
            <div className="flex justify-end border-t border-gray-200 px-6 py-4">
              <button
                type="button"
                onClick={onClose}
                className="h-10 rounded-xl border border-gray-200 px-4 text-sm font-semibold text-slate-700 hover:bg-gray-50"
              >
                Đóng
              </button>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
