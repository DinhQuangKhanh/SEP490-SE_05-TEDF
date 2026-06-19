/**
 * Pagination button group shared by the evaluation queue & history tables
 * (LecturerModerationPage, LecturerHistoryPage). Renders prev / first-5 / ellipsis /
 * last / next. Returns null when there is only a single page.
 */
export function EvaluatorPagination({
  page,
  totalPages,
  onPage,
}: {
  page: number;
  totalPages: number;
  onPage: (page: number) => void;
}) {
  if (totalPages <= 1) return null;

  return (
    <div className="flex items-center gap-2">
      <button
        type="button"
        disabled={page <= 1}
        onClick={() => onPage(Math.max(1, page - 1))}
        className="size-8 flex items-center justify-center rounded-lg border border-gray-200 hover:bg-gray-50 text-slate-500 disabled:opacity-50 transition-all"
      >
        <span className="material-symbols-outlined text-sm">chevron_left</span>
      </button>

      {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => i + 1).map((i) => (
        <button
          key={i}
          type="button"
          onClick={() => onPage(i)}
          className={`size-8 flex items-center justify-center rounded-lg text-xs font-bold transition-all ${
            page === i
              ? "bg-primary text-white shadow-md shadow-primary/20"
              : "border border-gray-200 hover:bg-gray-50 text-slate-500"
          }`}
        >
          {i}
        </button>
      ))}

      {totalPages > 5 && (
        <>
          <span className="size-8 flex items-center justify-center rounded-lg border border-gray-200 text-slate-500 text-xs font-bold">
            ...
          </span>
          <button
            type="button"
            onClick={() => onPage(totalPages)}
            className={`size-8 flex items-center justify-center rounded-lg text-xs font-bold transition-all ${
              page === totalPages
                ? "bg-primary text-white shadow-md shadow-primary/20"
                : "border border-gray-200 hover:bg-gray-50 text-slate-500"
            }`}
          >
            {totalPages}
          </button>
        </>
      )}

      <button
        type="button"
        disabled={page >= totalPages}
        onClick={() => onPage(Math.min(totalPages, page + 1))}
        className="size-8 flex items-center justify-center rounded-lg border border-gray-200 hover:bg-gray-50 text-slate-500 disabled:opacity-50 transition-all"
      >
        <span className="material-symbols-outlined text-sm">chevron_right</span>
      </button>
    </div>
  );
}
