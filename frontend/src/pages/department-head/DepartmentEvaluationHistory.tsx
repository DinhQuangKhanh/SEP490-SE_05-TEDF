import { useEffect, useMemo, useState } from "react";
import { motion } from "framer-motion";
import { isSameMonth, isSameWeek, isYesterday, parseISO } from "date-fns";
import { projectService } from "@/lib";
import { groupProjects, reviewedAt, type DepartmentProject, type DepartmentProjectsResponse } from "@/types";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { fadeContainer as container, fadeItem as item, formatDate } from "@/lib/common/ui";
import { EvaluatorFilterBar } from "@/components/lecturer/EvaluatorFilterBar";
import { EvaluatorPagination } from "@/components/lecturer/EvaluatorPagination";
import { DepartmentReviewDetailModal } from "@/components/lecturer";

const PAGE_SIZE = 10;

const RESULT_DISPLAY: Record<string, { label: string; colors: string }> = {
  Approved: { label: "Đã duyệt", colors: "bg-green-50 text-green-600 border-green-200" },
  NeedsModification: { label: "Cần chỉnh sửa", colors: "bg-amber-50 text-amber-600 border-amber-200" },
  Rejected: { label: "Từ chối", colors: "bg-red-50 text-red-600 border-red-200" },
};

/** Maps a project StatusValue to the evaluation-result key used by filters/labels. */
function resultKey(statusValue: number): string {
  if (statusValue === 3) return "Approved";
  if (statusValue === 2) return "NeedsModification";
  if (statusValue === 4) return "Rejected";
  return "";
}

/** Latest evaluation timestamp for a project (most recent evaluator submission), fallback to submittedAt. */
function evaluatorNames(project: DepartmentProject): string {
  return project.evaluators.map((e) => e.evaluatorName).join(", ") || "—";
}

function matchesDateRange(iso: string | null, range: string): boolean {
  if (!range) return true;
  if (!iso) return false;
  const d = parseISO(iso);
  if (Number.isNaN(d.getTime())) return false;
  const now = new Date();
  if (range === "thisMonth") return isSameMonth(d, now);
  if (range === "thisWeek") return isSameWeek(d, now, { weekStartsOn: 1 });
  if (range === "yesterday") return isYesterday(d);
  return true;
}

function toCsvValue(value = ""): string {
  return /[",\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;
}

/**
 * Department-Head evaluation history: lists every finalized (reviewed) project in the DH's department,
 * with real stats/filters/search, CSV export, and a read-only per-evaluator checklist detail. Rendered by
 * LecturerHistoryPage when the active role is departmenthead.
 */
export function DepartmentEvaluationHistory() {
  const { showError } = useSystemError();
  const [data, setData] = useState<DepartmentProjectsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);

  const [search, setSearch] = useState("");
  const [dateRange, setDateRange] = useState("");
  const [result, setResult] = useState("");
  const [page, setPage] = useState(1);

  const [detailProject, setDetailProject] = useState<DepartmentProject | null>(null);

  useEffect(() => {
    setLoading(true);
    setLoadError(false);
    projectService
      .getDepartmentProjects()
      .then(setData)
      .catch((err) => {
        setLoadError(true);
        showError(err instanceof Error ? err.message : "Không thể tải lịch sử thẩm định.");
      })
      .finally(() => setLoading(false));
  }, [showError]);

  // Finalized projects only (Approved / NeedsModification / Rejected) — reuse the shared grouping.
  const finalized = useMemo(() => groupProjects(data).completed, [data]);

  const stats = useMemo(
    () => ({
      total: finalized.length,
      approved: finalized.filter((p) => p.statusValue === 3).length,
      needsMod: finalized.filter((p) => p.statusValue === 2).length,
      rejected: finalized.filter((p) => p.statusValue === 4).length,
    }),
    [finalized],
  );

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return finalized.filter((p) => {
      if (result && resultKey(p.statusValue) !== result) return false;
      if (q && !`${p.projectCode} ${p.nameVi}`.toLowerCase().includes(q)) return false;
      if (!matchesDateRange(reviewedAt(p), dateRange)) return false;
      return true;
    });
  }, [finalized, search, result, dateRange]);

  const totalCount = filtered.length;
  const totalPages = Math.ceil(totalCount / PAGE_SIZE);
  const pageItems = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const from = totalCount === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const to = Math.min(page * PAGE_SIZE, totalCount);

  function clearFilters() {
    setSearch("");
    setDateRange("");
    setResult("");
    setPage(1);
  }

  function exportCsv() {
    const header = ["Mã đề tài", "Tên đề tài", "Học kỳ", "Kết quả", "Người thẩm định", "Thời gian thẩm định"];
    const rows = filtered.map((p) => [
      p.projectCode,
      p.nameVi,
      p.semesterName,
      RESULT_DISPLAY[resultKey(p.statusValue)]?.label ?? p.status,
      evaluatorNames(p),
      formatDate(reviewedAt(p)),
    ]);
    const csv = [header, ...rows].map((r) => r.map((c) => toCsvValue(String(c))).join(",")).join("\r\n");
    // Prepend a UTF-8 BOM so Excel opens Vietnamese characters correctly
    // (fromCharCode avoids embedding a literal BOM in the source file).
    const blob = new Blob([String.fromCodePoint(0xfeff) + csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `lich-su-tham-dinh-${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  }

  /**
   * Table body: loading / load-failed / empty / the result rows.
   * Split out of the JSX so the four states read as guards instead of nested ternaries.
   */
  function renderRows() {
    if (loading) {
      return (
        <div className="flex items-center justify-center py-16 text-slate-400 gap-3">
          <span className="material-symbols-outlined animate-spin">progress_activity</span>
          <span className="text-sm">Đang tải...</span>
        </div>
      );
    }

    if (loadError) {
      return (
        <div className="flex flex-col items-center justify-center py-16 text-slate-400 gap-2">
          <span className="material-symbols-outlined text-4xl text-amber-400">report</span>
          <p className="text-sm font-medium">Không thể tải dữ liệu. Vui lòng thử lại.</p>
        </div>
      );
    }

    if (pageItems.length === 0) {
      return (
        <div className="flex flex-col items-center justify-center py-16 text-slate-400 gap-2">
          <span className="material-symbols-outlined text-4xl">history</span>
          <p className="text-sm font-medium">Không tìm thấy kết quả nào</p>
        </div>
      );
    }

    return (
      <table className="w-full text-left border-collapse">
        <thead>
          <tr className="bg-gray-50/80 border-b border-gray-100">
            {["Đề tài", "Học kỳ", "Kết quả", "Người thẩm định", "Thời gian", "Thao tác"].map((h) => (
              <th
                key={h}
                className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap last:text-right"
              >
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {pageItems.map((p) => {
            const resultInfo = RESULT_DISPLAY[resultKey(p.statusValue)];
            return (
              <tr key={p.projectId} className="hover:bg-blue-50/20 transition-colors">
                <td className="px-6 py-4">
                  <div className="flex flex-col">
                    <span className="text-slate-900 font-semibold text-sm line-clamp-1">{p.nameVi}</span>
                    <span className="text-xs text-slate-500 font-mono mt-1">{p.projectCode}</span>
                  </div>
                </td>
                <td className="px-6 py-4 text-sm text-slate-600 whitespace-nowrap">{p.semesterName}</td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span
                    className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border ${resultInfo?.colors ?? "bg-gray-100 text-gray-600 border-gray-200"}`}
                  >
                    {resultInfo?.label ?? p.status}
                  </span>
                </td>
                <td className="px-6 py-4 text-sm text-slate-600 max-w-xs">
                  <span className="line-clamp-2">{evaluatorNames(p)}</span>
                </td>
                <td className="px-6 py-4 text-sm text-slate-500 whitespace-nowrap">
                  {formatDate(reviewedAt(p))}
                </td>
                <td className="px-6 py-4 text-right whitespace-nowrap">
                  <button
                    type="button"
                    onClick={() => setDetailProject(p)}
                    className="inline-flex items-center justify-center h-8 px-4 bg-white border border-gray-200 text-slate-700 text-xs font-bold rounded-lg hover:bg-gray-50 hover:border-primary/50 hover:text-primary transition-all"
                  >
                    Chi tiết
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    );
  }

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <header className="bg-white border-b border-gray-200 px-8 py-6 shrink-0">
        <div className="w-full flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex flex-col gap-1">
            <h2 className="text-slate-900 text-2xl font-bold tracking-tight flex items-center gap-2">
              <span className="material-symbols-outlined text-primary">history</span>{" "}
              Lịch sử thẩm định
            </h2>
            <p className="text-slate-500 text-sm">Các đề tài đã thẩm định thuộc bộ môn của bạn.</p>
          </div>
          <div className="flex gap-3">
            <button
              type="button"
              onClick={exportCsv}
              disabled={totalCount === 0}
              className="flex items-center justify-center gap-2 h-10 px-4 rounded-lg border border-gray-200 bg-white text-slate-700 text-sm font-semibold hover:bg-gray-50 transition-colors disabled:opacity-50"
            >
              <span className="material-symbols-outlined text-[20px]">download</span>
              <span>Xuất Excel</span>
            </button>
          </div>
        </div>
      </header>

      {/* Content */}
      <div className="w-full p-6 md:p-8 flex flex-col gap-6 flex-1 min-h-0 overflow-y-auto">
        <motion.div variants={container} initial="hidden" animate="show" className="flex flex-col gap-6">
          {/* Stats */}
          <motion.div variants={item} className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <StatCard icon="assignment_turned_in" tone="primary" value={stats.total} label="Tổng đã thẩm định" loading={loading} />
            <StatCard icon="check_circle" tone="green" value={stats.approved} label="Đã duyệt" loading={loading} />
            <StatCard icon="edit_note" tone="amber" value={stats.needsMod} label="Cần chỉnh sửa" loading={loading} />
            <StatCard icon="cancel" tone="red" value={stats.rejected} label="Từ chối" loading={loading} />
          </motion.div>

          {/* Filters */}
          <EvaluatorFilterBar
            search={search}
            onSearch={(v) => {
              setSearch(v);
              setPage(1);
            }}
            searchPlaceholder="Tên đề tài, mã..."
            onClear={clearFilters}
            selects={[
              {
                label: "Thời gian",
                value: dateRange,
                onChange: (v) => {
                  setDateRange(v);
                  setPage(1);
                },
                colSpan: 2,
                options: [
                  { value: "", label: "Tất cả" },
                  { value: "thisMonth", label: "Tháng này" },
                  { value: "thisWeek", label: "Tuần này" },
                  { value: "yesterday", label: "Hôm qua" },
                ],
              },
              {
                label: "Kết quả",
                value: result,
                onChange: (v) => {
                  setResult(v);
                  setPage(1);
                },
                colSpan: 2,
                options: [
                  { value: "", label: "Tất cả" },
                  { value: "Approved", label: "Đã duyệt" },
                  { value: "NeedsModification", label: "Cần chỉnh sửa" },
                  { value: "Rejected", label: "Từ chối" },
                ],
              },
            ]}
          />

          {/* Table */}
          <motion.div
            variants={item}
            className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden flex flex-col flex-1"
          >
            <div className="overflow-x-auto">
              {renderRows()}
            </div>
            {!loading && !loadError && totalCount > 0 && (
              <div className="px-6 py-4 border-t border-gray-100 flex items-center justify-between bg-white shrink-0">
                <p className="text-xs text-slate-500 font-medium">
                  Hiển thị <span className="font-bold text-slate-900">{from}</span> đến{" "}
                  <span className="font-bold text-slate-900">{to}</span> trong tổng số{" "}
                  <span className="font-bold text-slate-900">{totalCount}</span> đề tài
                </p>
                <EvaluatorPagination page={page} totalPages={totalPages} onPage={setPage} />
              </div>
            )}
          </motion.div>
        </motion.div>
      </div>

      {/* Detail modal */}
      <DepartmentReviewDetailModal project={detailProject} onClose={() => setDetailProject(null)} />
    </div>
  );
}

function StatCard({
  icon,
  tone,
  value,
  label,
  loading,
}: Readonly<{
  icon: string;
  tone: "primary" | "green" | "amber" | "red";
  value: number;
  label: string;
  loading: boolean;
}>) {
  const toneClasses: Record<string, string> = {
    primary: "bg-primary/10 text-primary",
    green: "bg-green-50 text-green-600",
    amber: "bg-amber-50 text-amber-600",
    red: "bg-red-50 text-red-600",
  };
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-5 flex items-center gap-4">
      <div className={`size-12 rounded-xl flex items-center justify-center ${toneClasses[tone]}`}>
        <span className="material-symbols-outlined text-2xl">{icon}</span>
      </div>
      <div>
        <p className="text-2xl font-bold text-slate-900">{loading ? "—" : value}</p>
        <p className="text-xs text-slate-500 font-medium">{label}</p>
      </div>
    </div>
  );
}
