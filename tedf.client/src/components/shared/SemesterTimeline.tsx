/**
 * Shared semester timeline component matching the admin/semesters design.
 * Works with the numeric phase format from dashboard APIs.
 */

// ── Types ────────────────────────────────────────────────────────────────────

interface Phase {
  name: string;
  type: number; // 0=Registration, 1=Evaluation, 2=Implementation, 3=Defense
  status: number; // 0=NotStarted, 1=InProgress, 2=Completed
  startDate: string;
  endDate: string;
}

interface SemesterTimelineProps {
  phases: Phase[];
  className?: string;
}

// ── Helpers ──────────────────────────────────────────────────────────────────

function phaseIcon(type: number): string {
  switch (type) {
    case 0:
      return "edit_note"; // Registration
    case 1:
      return "fact_check"; // Evaluation
    case 2:
      return "science"; // Implementation
    case 3:
      return "school"; // Defense
    default:
      return "event";
  }
}

function formatShortDate(iso: string): string {
  return new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit" });
}

// ── Component ────────────────────────────────────────────────────────────────

export function SemesterTimeline({ phases, className = "" }: SemesterTimelineProps) {
  if (phases.length === 0) return null;

  // Calculate progress percentage
  const total = phases.length;
  let pct = 0;
  if (total > 1) {
    const currentIndex = phases.findIndex((p) => p.status === 1); // InProgress
    if (currentIndex !== -1) {
      pct = (currentIndex / (total - 1)) * 100;
    } else {
      const completedCount = phases.filter((p) => p.status === 2).length;
      if (completedCount === total) pct = 100;
      else if (completedCount > 0) pct = Math.round(((completedCount - 1) / (total - 1)) * 100);
    }
  } else if (total === 1) {
    pct = phases[0].status === 2 ? 100 : 0;
  }

  const hasActive = phases.some((p) => p.status === 1);

  return (
    <div className={`relative px-2 pt-6 pb-2 ${className}`}>
      {/* Track background */}
      <div className="absolute left-0 w-full h-1 rounded-full top-[34px] bg-slate-100 z-0" />
      {/* Track progress */}
      <div
        className={`absolute top-[34px] left-0 h-1 rounded-full z-0 transition-all duration-1000 ease-in-out ${
          hasActive ? "bg-green-600 shadow-[0_0_8px_rgba(22,163,74,0.5)]" : "bg-green-600"
        }`}
        style={{ width: `${pct}%` }}
      />

      {/* Steps */}
      <div className="relative z-10 flex justify-between w-full">
        {phases.map((phase, idx) => {
          const isCompleted = phase.status === 2;
          const isCurrent = phase.status === 1;
          const isPending = phase.status === 0;

          const info = isCompleted
            ? `Hoàn tất ${formatShortDate(phase.endDate)}`
            : isCurrent
              ? "Đang diễn ra"
              : `Dự kiến ${formatShortDate(phase.startDate)}`;

          return (
            <div
              key={idx}
              className={`flex flex-col items-center gap-2 ${isPending ? "opacity-60" : ""}`}
            >
              {/* Icon circle */}
              <div className="relative flex items-center justify-center">
                {isCurrent && (
                  <div className="absolute inset-0 rounded-full bg-green-500/40 animate-ping" />
                )}
                <div
                  className={`relative z-10 rounded-full flex items-center justify-center ring-4 ring-white transition-all duration-300 ${
                    isCurrent
                      ? "w-10 h-10 bg-white border-[3px] border-green-600 text-green-600 shadow-[0_0_15px_rgba(22,163,74,0.3)]"
                      : isCompleted
                        ? "w-8 h-8 bg-gradient-to-br from-green-500 to-green-700 text-white shadow-md"
                        : "w-8 h-8 bg-slate-50 border-2 border-slate-200 text-slate-400"
                  }`}
                >
                  {isCompleted ? (
                    <span className="material-symbols-outlined text-[16px] font-bold">check</span>
                  ) : (
                    <span
                      className={`material-symbols-outlined ${isCurrent ? "text-[20px] font-bold" : "text-[16px]"}`}
                    >
                      {phaseIcon(phase.type)}
                    </span>
                  )}
                </div>
              </div>

              {/* Label + info — below the icon/bar */}
              <div className="text-center mt-1">
                <p
                  className={`text-xs font-bold uppercase ${
                    isCurrent ? "text-sm text-green-700" : isPending ? "text-slate-500" : "text-slate-700"
                  }`}
                >
                  {phase.name}
                </p>
                {isCurrent ? (
                  <p className="text-[10px] text-green-700 font-medium bg-green-50 px-2 py-0.5 rounded mt-1 border border-green-200">
                    {info}
                  </p>
                ) : (
                  <p className="text-[10px] text-slate-400 mt-0.5">{info}</p>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
