import React, { useState, useEffect, useCallback, useMemo } from "react";
import { motion } from "framer-motion";
import { Header } from "@/components/layout";
import { projectService, semesterService } from "@/lib";
import {
  DepartmentAuditLogFilters,
  DepartmentAuditLogItemDto,
  DepartmentAuditLogStatsDto,
  SemesterDto,
} from "@/types";

// ── Animation variants ─────────────────────────────────────────────────────
const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.05 } },
};
const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

// ── Action configuration ────────────────────────────────────────────────────
interface ActionConfig {
  label: string;
  icon: string;
  color: string;
  bg: string;
  border: string;
}

const ACTION_CONFIG: Record<string, ActionConfig> = {
  Submitted: {
    label: "Nộp đề tài",
    icon: "send",
    color: "text-blue-700",
    bg: "bg-blue-50",
    border: "border-blue-200",
  },
  Resubmitted: {
    label: "Nộp lại đề tài",
    icon: "replay",
    color: "text-indigo-700",
    bg: "bg-indigo-50",
    border: "border-indigo-200",
  },
  SubmittedToMentor: {
    label: "Nộp cho GVHD",
    icon: "forward_to_inbox",
    color: "text-cyan-700",
    bg: "bg-cyan-50",
    border: "border-cyan-200",
  },
  MentorApproved: {
    label: "GVHD duyệt",
    icon: "check_circle",
    color: "text-emerald-700",
    bg: "bg-emerald-50",
    border: "border-emerald-200",
  },
  MentorNeedsModification: {
    label: "GVHD yêu cầu chỉnh sửa",
    icon: "edit_note",
    color: "text-amber-700",
    bg: "bg-amber-50",
    border: "border-amber-200",
  },
  Approved: {
    label: "Hội đồng duyệt",
    icon: "verified",
    color: "text-green-700",
    bg: "bg-green-50",
    border: "border-green-200",
  },
  NeedsModification: {
    label: "Hội đồng yêu cầu chỉnh sửa",
    icon: "rate_review",
    color: "text-orange-700",
    bg: "bg-orange-50",
    border: "border-orange-200",
  },
  Rejected: {
    label: "Hội đồng từ chối",
    icon: "cancel",
    color: "text-red-700",
    bg: "bg-red-50",
    border: "border-red-200",
  },
  MentorAssigned: {
    label: "Phân công GVHD",
    icon: "assignment_ind",
    color: "text-purple-700",
    bg: "bg-purple-50",
    border: "border-purple-200",
  },
  EvaluatorAssigned: {
    label: "Phân công Evaluator",
    icon: "person_add",
    color: "text-indigo-700",
    bg: "bg-indigo-50",
    border: "border-indigo-200",
  },
  DocumentUploaded: {
    label: "Tải lên tài liệu",
    icon: "upload_file",
    color: "text-teal-700",
    bg: "bg-teal-50",
    border: "border-teal-200",
  },
  DocumentDeleted: {
    label: "Xóa tài liệu",
    icon: "delete",
    color: "text-rose-700",
    bg: "bg-rose-50",
    border: "border-rose-200",
  },
};

function getActionConfig(action: string): ActionConfig {
  return (
    ACTION_CONFIG[action] ?? {
      label: action,
      icon: "info",
      color: "text-slate-700",
      bg: "bg-slate-50",
      border: "border-slate-200",
    }
  );
}

// ── Action filter tabs ──────────────────────────────────────────────────────
const ACTION_FILTERS = [
  { key: "", label: "Tất cả", icon: "list" },
  { key: "Submitted,Resubmitted,SubmittedToMentor", label: "Nộp đề tài", icon: "send" },
  { key: "MentorApproved,Approved", label: "Duyệt", icon: "check_circle" },
  { key: "MentorNeedsModification,NeedsModification", label: "Yêu cầu sửa", icon: "edit_note" },
  { key: "Rejected", label: "Từ chối", icon: "cancel" },
  { key: "MentorAssigned,EvaluatorAssigned", label: "Phân công", icon: "assignment_ind" },
  { key: "DocumentUploaded,DocumentDeleted", label: "Tài liệu", icon: "description" },
];

// ── Project status labels (for the "trạng thái trước → sau" column) ─────────
const STATUS_LABELS: Record<string, string> = {
  Draft: "Nháp",
  PendingMentorReview: "Chờ GVHD duyệt",
  PendingEvaluation: "Chờ thẩm định",
  NeedsModification: "Cần chỉnh sửa",
  Approved: "Đã duyệt",
  Rejected: "Từ chối",
  InProgress: "Đang thực hiện",
  Completed: "Hoàn thành",
  Cancelled: "Đã hủy",
};

function formatStatus(status: string | null): string {
  if (!status) return "—";
  return STATUS_LABELS[status] ?? status;
}

const PAGE_SIZE = 10;

/** Server caps pageSize at 100; export walks the pages with that size. */
const EXPORT_PAGE_SIZE = 100;

/** Safety valve so an unfiltered export cannot pull an unbounded number of rows into the browser. */
const EXPORT_MAX_ROWS = 5000;

// ── Page Component ──────────────────────────────────────────────────────────
export function ProjectAuditLogsPage() {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [actionFilter, setActionFilter] = useState("");
  const [semesterId, setSemesterId] = useState<number | "">("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [page, setPage] = useState(1);
  const [expandedIds, setExpandedIds] = useState<string[]>([]);

  const [semesters, setSemesters] = useState<SemesterDto[]>([]);
  const [exporting, setExporting] = useState(false);

  const [logs, setLogs] = useState<DepartmentAuditLogItemDto[]>([]);
  const [stats, setStats] = useState<DepartmentAuditLogStatsDto>({
    total: 0,
    submitted: 0,
    approved: 0,
    revision: 0,
    rejected: 0,
  });
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const toggleExpand = (id: string) => {
    setExpandedIds((prev) => (prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]));
  };

  // Debounce search
  useEffect(() => {
    const id = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(id);
  }, [search]);

  // Semester dropdown options. A failure here only costs the filter, not the page.
  useEffect(() => {
    semesterService
      .getPublicSemesters()
      .then(setSemesters)
      .catch(() => setSemesters([]));
  }, []);

  // Reset page when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, actionFilter, semesterId, fromDate, toDate]);

  // The date inputs are local dates; widen them to cover the whole day before going to UTC,
  // otherwise "to = today" would drop everything logged after midnight.
  const filters = useMemo<DepartmentAuditLogFilters>(
    () => ({
      search: debouncedSearch || undefined,
      actions: actionFilter || undefined,
      semesterId: semesterId === "" ? undefined : semesterId,
      from: fromDate ? new Date(`${fromDate}T00:00:00`).toISOString() : undefined,
      to: toDate ? new Date(`${toDate}T23:59:59.999`).toISOString() : undefined,
    }),
    [debouncedSearch, actionFilter, semesterId, fromDate, toDate]
  );

  const hasActiveFilter = Boolean(debouncedSearch || actionFilter || semesterId !== "" || fromDate || toDate);

  const clearFilters = () => {
    setSearch("");
    setActionFilter("");
    setSemesterId("");
    setFromDate("");
    setToDate("");
  };

  // Search, filtering and paging are all resolved server-side against SQL Server.
  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await projectService.getDepartmentAuditLogs({ ...filters, page, pageSize: PAGE_SIZE });
      setLogs(data.items);
      setStats(data.stats);
      setTotalCount(data.totalCount);
      setTotalPages(Math.max(1, data.totalPages));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không tải được nhật ký thao tác.");
      setLogs([]);
    } finally {
      setLoading(false);
    }
  }, [filters, page]);

  useEffect(() => {
    void fetchLogs();
  }, [fetchLogs]);

  // Exports what the current filters select, not just the visible page, by walking the pages.
  const handleExport = useCallback(async () => {
    setExporting(true);
    setError(null);
    try {
      const rows: DepartmentAuditLogItemDto[] = [];
      let current = 1;
      let lastPage = 1;

      do {
        const data = await projectService.getDepartmentAuditLogs({
          ...filters,
          page: current,
          pageSize: EXPORT_PAGE_SIZE,
        });
        rows.push(...data.items);
        lastPage = Math.max(1, data.totalPages);
        current += 1;
      } while (current <= lastPage && rows.length < EXPORT_MAX_ROWS);

      downloadCsv(rows);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không xuất được nhật ký thao tác.");
    } finally {
      setExporting(false);
    }
  }, [filters]);

  const pagedLogs = logs;

  return (
    <>
      <Header title="Nhật Ký Thao Tác Đề Tài" showSearch={false} />

      <div className="flex-1 p-8 overflow-y-auto scrollbar-hide bg-slate-50">
        <motion.div variants={container} initial="hidden" animate="show" className="flex flex-col gap-6">
          {/* ── Error banner ──────────────────────────────────── */}
          {error && (
            <motion.div
              variants={item}
              className="flex items-center gap-3 px-4 py-3 border rounded-lg bg-red-50 border-red-200"
            >
              <span className="material-symbols-outlined text-red-600 text-[20px]">error</span>
              <div className="flex-1">
                <p className="text-sm font-semibold text-red-800">Không tải được dữ liệu</p>
                <p className="text-xs text-red-600">{error}</p>
              </div>
              <button
                type="button"
                onClick={() => void fetchLogs()}
                className="px-3 py-1.5 text-sm font-medium text-red-700 bg-white border border-red-200 rounded-md hover:bg-red-50"
              >
                Thử lại
              </button>
            </motion.div>
          )}

          {/* ── Summary Cards ────────────────────────────────── */}
          <motion.div variants={item} className="grid grid-cols-2 gap-4 lg:grid-cols-5">
            <StatCard label="Tổng thao tác" count={stats.total} icon="analytics" color="text-primary" bg="bg-primary/10" />
            <StatCard label="Nộp đề tài" count={stats.submitted} icon="send" color="text-blue-600" bg="bg-blue-50" />
            <StatCard
              label="Đã duyệt"
              count={stats.approved}
              icon="check_circle"
              color="text-emerald-600"
              bg="bg-emerald-50"
            />
            <StatCard
              label="Yêu cầu sửa"
              count={stats.revision}
              icon="edit_note"
              color="text-amber-600"
              bg="bg-amber-50"
            />
            <StatCard label="Từ chối" count={stats.rejected} icon="cancel" color="text-red-600" bg="bg-red-50" />
          </motion.div>

          {/* ── Filters ──────────────────────────────────────── */}
          <motion.div variants={item} className="flex flex-col gap-3">
            {/* Action filter tabs */}
            <div className="flex p-1 overflow-x-auto bg-white border rounded-lg shadow-sm border-slate-200 scrollbar-hide">
              {ACTION_FILTERS.map((tab) => (
                <button
                  key={tab.key}
                  type="button"
                  onClick={() => setActionFilter(tab.key)}
                  className={`flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md whitespace-nowrap transition-all ${actionFilter === tab.key
                      ? "bg-primary text-white shadow-sm"
                      : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                    }`}
                >
                  <span className="material-symbols-outlined text-[18px]">{tab.icon}</span>
                  {tab.label}
                </button>
              ))}
            </div>

            {/* Search */}
            <div className="relative w-full">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-400 text-[18px]">
                search
              </span>
              <input
                className="w-full py-2.5 pr-9 text-sm bg-white border rounded-lg pl-9 border-slate-200 shadow-sm focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary placeholder-slate-400 text-slate-700"
                placeholder="Tìm theo mã đề tài, tên đề tài, người thao tác..."
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
              {search && (
                <button
                  type="button"
                  onClick={() => setSearch("")}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                >
                  <span className="material-symbols-outlined text-[18px]">close</span>
                </button>
              )}
            </div>

            {/* Semester + date range + export */}
            <div className="flex flex-wrap items-end gap-3 p-3 bg-white border rounded-lg shadow-sm border-slate-200">
              <label className="flex flex-col gap-1">
                <span className="text-xs font-medium text-slate-500">Học kỳ</span>
                <select
                  className="py-2 pl-3 pr-8 text-sm bg-white border rounded-md border-slate-200 text-slate-700 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                  value={semesterId}
                  onChange={(e) => setSemesterId(e.target.value === "" ? "" : Number(e.target.value))}
                >
                  <option value="">Tất cả học kỳ</option>
                  {semesters.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name}
                    </option>
                  ))}
                </select>
              </label>

              <label className="flex flex-col gap-1">
                <span className="text-xs font-medium text-slate-500">Từ ngày</span>
                <input
                  type="date"
                  className="px-3 py-2 text-sm bg-white border rounded-md border-slate-200 text-slate-700 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                  value={fromDate}
                  max={toDate || undefined}
                  onChange={(e) => setFromDate(e.target.value)}
                />
              </label>

              <label className="flex flex-col gap-1">
                <span className="text-xs font-medium text-slate-500">Đến ngày</span>
                <input
                  type="date"
                  className="px-3 py-2 text-sm bg-white border rounded-md border-slate-200 text-slate-700 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                  value={toDate}
                  min={fromDate || undefined}
                  onChange={(e) => setToDate(e.target.value)}
                />
              </label>

              {hasActiveFilter && (
                <button
                  type="button"
                  onClick={clearFilters}
                  className="flex items-center gap-1.5 px-3 py-2 text-sm font-medium rounded-md text-slate-600 border border-slate-200 hover:bg-slate-50 transition-colors"
                >
                  <span className="material-symbols-outlined text-[18px]">filter_alt_off</span>
                  <span>Xóa bộ lọc</span>
                </button>
              )}

              <button
                type="button"
                onClick={() => void handleExport()}
                disabled={exporting || totalCount === 0}
                className="flex items-center gap-1.5 px-3 py-2 ml-auto text-sm font-medium text-white rounded-md bg-primary hover:opacity-90 transition-opacity disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <span className="material-symbols-outlined text-[18px]">download</span>
                {exporting ? "Đang xuất..." : "Xuất CSV"}
              </button>
            </div>
          </motion.div>

          {/* ── Table ─────────────────────────────────────────── */}
          <motion.div
            variants={item}
            className="flex flex-col flex-1 min-h-0 overflow-hidden bg-white rounded-xl border border-slate-200 shadow-sm"
          >
            <div className="flex-1 overflow-auto">
              <table className="w-full text-sm text-left text-slate-600">
                <thead className="sticky top-0 z-10 text-xs font-bold uppercase border-b bg-slate-50 text-slate-500 border-slate-200">
                  <tr>
                    <th className="px-6 py-4 w-40">Thời gian</th>
                    <th className="px-6 py-4 min-w-[250px]">Đề tài</th>
                    <th className="px-6 py-4 w-56">Hành động</th>
                    <th className="px-6 py-4 w-52">Trạng thái</th>
                    <th className="px-6 py-4 w-48">Người thực hiện</th>
                    <th className="px-6 py-4 w-16 text-center">Chi tiết</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {loading &&
                    Array.from({ length: 5 }, (_, i) => (
                      <tr key={`skeleton-${i}`} className="animate-pulse">
                        <td colSpan={6} className="px-6 py-4">
                          <div className="h-8 rounded bg-slate-100" />
                        </td>
                      </tr>
                    ))}

                  {!loading && pagedLogs.length === 0 && (
                    <tr>
                      <td colSpan={6} className="px-6 py-20 text-center text-slate-400">
                        <span className="material-symbols-outlined text-[40px] mb-2 block text-slate-300">
                          search_off
                        </span>
                        <span>
                          {debouncedSearch || actionFilter
                            ? "Không tìm thấy bản ghi nào"
                            : "Chưa có thao tác nào được ghi nhận"}
                        </span>
                      </td>
                    </tr>
                  )}

                  {!loading &&
                    pagedLogs.map((log) => {
                      const ac = getActionConfig(log.action);
                      const isExpanded = expandedIds.includes(log.id);

                      return (
                        <React.Fragment key={log.id}>
                          <tr
                            className={`transition-colors hover:bg-slate-50/80 group cursor-pointer ${isExpanded ? "bg-slate-50/50" : ""}`}
                            onClick={() => toggleExpand(log.id)}
                          >
                            {/* Timestamp */}
                            <td className="px-6 py-4">
                              <div className="text-xs text-slate-500">{formatTimestamp(log.timestamp)}</div>
                              <div className="text-[10px] text-slate-400 mt-0.5">
                                {new Date(log.timestamp).toLocaleTimeString("vi-VN", {
                                  hour: "2-digit",
                                  minute: "2-digit",
                                })}
                              </div>
                            </td>

                            {/* Project — clicking it narrows the trail to this project only */}
                            <td className="px-6 py-4">
                              <button
                                type="button"
                                title="Chỉ xem nhật ký của đề tài này"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  setSearch(log.projectCode);
                                }}
                                className="flex items-start gap-2.5 text-left w-full"
                              >
                                <div className="mt-0.5 bg-primary/10 text-primary rounded p-1.5 flex-shrink-0">
                                  <span className="material-symbols-outlined text-[16px]">description</span>
                                </div>
                                <div className="min-w-0">
                                  <p className="text-sm font-semibold text-slate-800 truncate hover:text-primary hover:underline transition-colors">
                                    {log.projectName}
                                  </p>
                                  <p className="text-xs font-mono text-slate-400 mt-0.5">{log.projectCode}</p>
                                </div>
                              </button>
                            </td>

                            {/* Action */}
                            <td className="px-6 py-4">
                              <span
                                className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border whitespace-nowrap ${ac.bg} ${ac.color} ${ac.border}`}
                              >
                                <span className="material-symbols-outlined text-[14px]">{ac.icon}</span>
                                {ac.label}
                              </span>
                            </td>

                            {/* Status transition — empty for actions that did not move the project */}
                            <td className="px-6 py-4">
                              {log.newStatus ? (
                                <div className="flex items-center gap-1.5 text-xs">
                                  <span className="px-2 py-0.5 rounded bg-slate-100 text-slate-600 whitespace-nowrap">
                                    {formatStatus(log.oldStatus)}
                                  </span>
                                  <span className="material-symbols-outlined text-[14px] text-slate-400">
                                    arrow_forward
                                  </span>
                                  <span className="px-2 py-0.5 font-medium rounded bg-primary/10 text-primary whitespace-nowrap">
                                    {formatStatus(log.newStatus)}
                                  </span>
                                </div>
                              ) : (
                                <span className="text-slate-300">—</span>
                              )}
                            </td>

                            {/* Performer */}
                            <td className="px-6 py-4">
                              <div className="flex items-center gap-2">
                                <div className="flex items-center justify-center w-7 h-7 text-[10px] font-bold rounded-full bg-primary/10 text-primary shrink-0">
                                  {getInitials(log.performedByName)}
                                </div>
                                <span className="text-sm text-slate-700 font-medium truncate">
                                  {log.performedByName ?? "Hệ thống"}
                                </span>
                              </div>
                            </td>

                            {/* Expand */}
                            <td className="px-6 py-4 text-center">
                              {log.metadata ? (
                                <span
                                  className={`material-symbols-outlined text-[20px] text-slate-400 transition-transform ${isExpanded ? "rotate-180" : ""
                                    }`}
                                >
                                  expand_more
                                </span>
                              ) : (
                                <span className="text-slate-300">—</span>
                              )}
                            </td>
                          </tr>

                          {/* Expanded Details Row */}
                          {isExpanded && log.metadata && (
                            <tr>
                              <td colSpan={6} className="p-0 border-b border-slate-100">
                                <motion.div
                                  initial={{ height: 0, opacity: 0 }}
                                  animate={{ height: "auto", opacity: 1 }}
                                  exit={{ height: 0, opacity: 0 }}
                                  className="overflow-hidden bg-slate-50/60"
                                >
                                  <div className="px-6 py-4 border-l-2 border-primary ml-2 my-2 bg-white rounded-r-lg shadow-sm">
                                    <div className="flex items-start gap-2">
                                      <span className="material-symbols-outlined text-primary text-[18px] mt-0.5 shrink-0">
                                        info
                                      </span>
                                      <div className="space-y-1.5 text-sm">
                                        {Object.entries(log.metadata).map(([key, value]) => (
                                          <div key={key} className="flex items-baseline gap-2">
                                            <span className="text-xs font-medium text-slate-500 min-w-[120px]">
                                              {formatMetadataKey(key)}:
                                            </span>
                                            <span className="text-slate-800 font-medium">{String(value)}</span>
                                          </div>
                                        ))}
                                      </div>
                                    </div>
                                  </div>
                                </motion.div>
                              </td>
                            </tr>
                          )}
                        </React.Fragment>
                      );
                    })}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between p-4 bg-white border-t border-slate-200 shrink-0">
                <span className="text-sm text-slate-500">
                  Hiển thị{" "}
                  <span className="font-medium text-slate-900">{(page - 1) * PAGE_SIZE + 1}</span> đến{" "}
                  <span className="font-medium text-slate-900">{Math.min(page * PAGE_SIZE, totalCount)}</span> trong số{" "}
                  <span className="font-medium text-slate-900">{totalCount}</span> bản ghi
                </span>
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    disabled={page <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    className="px-3 py-1.5 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-md hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Trước
                  </button>
                  {getPageNumbers(page, totalPages).map((p, i, arr) =>
                    p === "..." ? (
                      <span key={`dots-after-${arr[i - 1]}`} className="px-2 py-1 text-slate-400">
                        ...
                      </span>
                    ) : (
                      <button
                        key={p}
                        type="button"
                        onClick={() => setPage(p as number)}
                        className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${p === page ? "bg-primary text-white" : "text-slate-600 border border-slate-200 hover:bg-slate-50"
                          }`}
                      >
                        {p}
                      </button>
                    )
                  )}
                  <button
                    type="button"
                    disabled={page >= totalPages}
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    className="px-3 py-1.5 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-md hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Sau
                  </button>
                </div>
              </div>
            )}
          </motion.div>
        </motion.div>
      </div>
    </>
  );
}

// ── Helper Components ───────────────────────────────────────────────────────
function StatCard({
  label,
  count,
  icon,
  color,
  bg,
}: Readonly<{
  label: string;
  count: number;
  icon: string;
  color: string;
  bg: string;
}>) {
  return (
    <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-4 flex items-center gap-4">
      <div className={`${bg} p-2.5 rounded-lg`}>
        <span className={`material-symbols-outlined ${color} text-[22px]`}>{icon}</span>
      </div>
      <div>
        <p className="text-xl font-bold text-slate-800">{count}</p>
        <p className="text-xs text-slate-500">{label}</p>
      </div>
    </div>
  );
}

// ── Utilities ───────────────────────────────────────────────────────────────
function getInitials(name: string | null): string {
  if (!name) return "—";
  return name
    .split(" ")
    .filter(Boolean)
    .map((w) => w[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
}

function formatTimestamp(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

/**
 * Labels for the metadata keys the API renders. The backend resolves user ids to names and drops
 * opaque ids, so these are the only keys that can reach here — an unmapped key falls back to itself.
 */
function formatMetadataKey(key: string): string {
  const map: Record<string, string> = {
    submissionNumber: "Lần nộp",
    feedback: "Phản hồi",
    comment: "Ghi chú",
    reason: "Lý do",
    mentorName: "Giảng viên hướng dẫn",
    evaluatorName: "Người thẩm định",
    evaluatorOrder: "Thứ tự thẩm định",
    assignedByName: "Người phân công",
    deletedByName: "Người xóa",
    documentType: "Loại tài liệu",
    fileName: "Tên file",
  };
  return map[key] ?? key;
}

/** Flattens rendered metadata into one readable cell, e.g. "Phản hồi: cần bổ sung mục tiêu". */
function formatMetadata(metadata: Record<string, unknown> | null): string {
  if (!metadata) return "";
  return Object.entries(metadata)
    .map(([key, value]) => `${formatMetadataKey(key)}: ${String(value)}`)
    .join("; ");
}

// ── CSV export ──────────────────────────────────────────────────────────────
const CSV_HEADERS = [
  "Thời gian",
  "Mã đề tài",
  "Tên đề tài",
  "Hành động",
  "Trạng thái trước",
  "Trạng thái sau",
  "Lần nộp",
  "Người thực hiện",
  "Chi tiết",
];

function escapeCsvCell(value: string): string {
  return `"${value.replace(/"/g, '""')}"`;
}

function downloadCsv(rows: DepartmentAuditLogItemDto[]): void {
  const lines = rows.map((log) =>
    [
      new Date(log.timestamp).toLocaleString("vi-VN"),
      log.projectCode,
      log.projectName,
      getActionConfig(log.action).label,
      formatStatus(log.oldStatus),
      formatStatus(log.newStatus),
      log.submissionNumber?.toString() ?? "",
      log.performedByName ?? "Hệ thống",
      formatMetadata(log.metadata),
    ]
      .map(escapeCsvCell)
      .join(",")
  );

  // BOM so Excel opens the Vietnamese text as UTF-8 instead of mojibake.
  const csv = `\uFEFF${[CSV_HEADERS.map(escapeCsvCell).join(","), ...lines].join("\r\n")}`;
  const url = URL.createObjectURL(new Blob([csv], { type: "text/csv;charset=utf-8;" }));

  const link = document.createElement("a");
  link.href = url;
  link.download = `nhat-ky-thao-tac-${new Date().toISOString().slice(0, 10)}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}

function getPageNumbers(current: number, total: number): (number | "...")[] {
  if (total <= 5) return Array.from({ length: total }, (_, i) => i + 1);
  const pages: (number | "...")[] = [1];
  if (current > 3) pages.push("...");
  for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
    pages.push(i);
  }
  if (current < total - 2) pages.push("...");
  pages.push(total);
  return pages;
}
