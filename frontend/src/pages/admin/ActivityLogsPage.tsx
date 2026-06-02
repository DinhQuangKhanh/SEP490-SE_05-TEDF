import { useState, useEffect, useCallback } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Header } from "@/components/layout";
import { useSystemError } from "@/contexts/SystemErrorContext";
import {
  activityLogService,
  type GroupedActivityLogItem,
  type GroupedActivityLogResponse,
  type ErrorDetailItem,
  type SeveritySummary,
  type ErrorLogDetail,
} from "@/lib/activityLogService";

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.05 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

// ── Constants ──────────────────────────────────────────────
const roleTabs = [
  { key: "", label: "Tất cả", icon: "groups" },
  { key: "admin", label: "Admin", icon: "admin_panel_settings" },
  { key: "mentor", label: "Mentor", icon: "school" },
  { key: "evaluator", label: "Evaluator", icon: "fact_check" },
  { key: "student", label: "Sinh viên", icon: "person" },
];

const severityOptions = [
  { key: "", label: "Tất cả" },
  { key: "info", label: "Info" },
  { key: "warning", label: "Warning" },
  { key: "error", label: "Error" },
  { key: "critical", label: "Critical" },
];

const severityConfig: Record<string, { bg: string; text: string; border: string; icon: string; cardBg: string }> = {
  info: { bg: "bg-blue-50", text: "text-blue-700", border: "border-blue-200", icon: "info", cardBg: "bg-blue-50/80" },
  warning: { bg: "bg-amber-50", text: "text-amber-700", border: "border-amber-200", icon: "warning", cardBg: "bg-amber-50/80" },
  error: { bg: "bg-red-50", text: "text-red-700", border: "border-red-200", icon: "error", cardBg: "bg-red-50/80" },
  critical: { bg: "bg-red-100", text: "text-red-900", border: "border-red-300", icon: "emergency", cardBg: "bg-red-100/80" },
};

const roleColors: Record<string, string> = {
  admin: "bg-slate-800 text-slate-100 border-slate-700",
  mentor: "bg-purple-50 text-purple-700 border-purple-100",
  evaluator: "bg-orange-50 text-orange-700 border-orange-100",
  student: "bg-slate-100 text-slate-700 border-slate-200",
};

const PAGE_SIZE = 20;

// ── Component ──────────────────────────────────────────────
export function ActivityLogsPage() {
  const [activeRole, setActiveRole] = useState("");
  const [severity, setSeverity] = useState("");
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [page, setPage] = useState(1);
  const [data, setData] = useState<GroupedActivityLogResponse | null>(null);
  const [severitySummary, setSeveritySummary] = useState<SeveritySummary | null>(null);
  const [loading, setLoading] = useState(false);

  // Modal state
  const [selectedLog, setSelectedLog] = useState<GroupedActivityLogItem | null>(null);
  const [errorDetails, setErrorDetails] = useState<ErrorDetailItem[]>([]);
  const [loadingErrors, setLoadingErrors] = useState(false);
  // Error log detail panel state
  const [errorLogDetail, setErrorLogDetail] = useState<ErrorLogDetail | null>(null);
  const [loadingErrorLog, setLoadingErrorLog] = useState(false);
  const { showError } = useSystemError();

  // Debounce search input
  useEffect(() => {
    const id = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(id);
  }, [search]);

  // Fetch data
  const fetchLogs = useCallback(async () => {
    setLoading(true);
    try {
      const [result, summary] = await Promise.all([
        activityLogService.getGroupedLogs({
          role: activeRole || undefined,
          severity: severity || undefined,
          search: debouncedSearch || undefined,
          from: fromDate || undefined,
          to: toDate || undefined,
          page,
          pageSize: PAGE_SIZE,
        }),
        activityLogService.getSeveritySummary(
          activeRole || undefined,
          fromDate || undefined,
          toDate || undefined,
        ),
      ]);
      setData(result);
      setSeveritySummary(summary);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Đã xảy ra lỗi khi tải dữ liệu nhật ký.");
    } finally {
      setLoading(false);
    }
  }, [activeRole, severity, debouncedSearch, fromDate, toDate, page]);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  // Reset page when filters change
  useEffect(() => {
    setPage(1);
  }, [activeRole, severity, debouncedSearch, fromDate, toDate]);

  // Fetch error details when modal opens
  useEffect(() => {
    if (!selectedLog) return;
    const hasErrors = selectedLog.severityCounts.error + selectedLog.severityCounts.critical > 0;
    if (!hasErrors) {
      setErrorDetails([]);
      return;
    }
    setLoadingErrors(true);
    activityLogService
      .getErrorDetails(selectedLog.userId, selectedLog.action, fromDate || undefined, toDate || undefined)
      .then((res) => setErrorDetails(res.errors))
      .catch(() => setErrorDetails([]))
      .finally(() => setLoadingErrors(false));
  }, [selectedLog, fromDate, toDate]);

  const totalPages = data?.totalPages ?? 1;
  const ss = severitySummary;

  return (
    <>
      <Header title="Nhật Ký Hoạt Động" showSearch={false} />

      <div className="flex-1 overflow-y-auto p-8 scrollbar-hide bg-slate-50">
        <motion.div variants={container} initial="hidden" animate="show" className="flex flex-col h-full">
          {/* ── Severity Summary Cards ──────────────────────── */}
          <motion.div variants={item} className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-6">
            <SeverityCard
              label="Tổng"
              count={ss?.total ?? 0}
              icon="analytics"
              iconColor="text-primary"
              iconBg="bg-primary/10"
              active={severity === ""}
              onClick={() => setSeverity("")}
            />
            {(["info", "warning", "error", "critical"] as const).map((sev) => {
              const cfg = severityConfig[sev];
              const count = ss?.[sev] ?? 0;
              return (
                <SeverityCard
                  key={sev}
                  label={sev.charAt(0).toUpperCase() + sev.slice(1)}
                  count={count}
                  icon={cfg.icon}
                  iconColor={cfg.text}
                  iconBg={cfg.cardBg}
                  active={severity === sev}
                  onClick={() => setSeverity(severity === sev ? "" : sev)}
                />
              );
            })}
          </motion.div>

          {/* ── Filters ─────────────────────────────────────── */}
          <motion.div variants={item} className="flex flex-col gap-4 mb-6">
            {/* Row 1: Role tabs + Search + Refresh */}
            <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
              <div className="flex bg-white p-1 rounded-lg border border-slate-200 shadow-sm overflow-x-auto scrollbar-hide">
                {roleTabs.map((tab) => (
                  <button
                    key={tab.key}
                    onClick={() => setActiveRole(tab.key)}
                    className={`flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md whitespace-nowrap transition-all ${
                      activeRole === tab.key
                        ? "bg-primary text-white shadow-sm"
                        : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                    }`}
                  >
                    <span className="material-symbols-outlined text-[18px]">{tab.icon}</span>
                    {tab.label}
                  </button>
                ))}
              </div>

              <div className="flex items-center gap-3 w-full lg:w-auto">
                <div className="relative flex-1 lg:w-80">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-400 text-[18px]">
                    search
                  </span>
                  <input
                    className="w-full pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary bg-white placeholder-slate-400 text-slate-700"
                    placeholder="Tìm theo tên, email, hành động..."
                    type="text"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                  />
                </div>
                <button
                  onClick={fetchLogs}
                  className="flex items-center gap-2 px-4 py-2 bg-white border border-slate-200 text-slate-700 text-sm font-medium rounded-md hover:bg-slate-50 hover:text-primary transition-colors whitespace-nowrap"
                >
                  <span className="material-symbols-outlined text-[20px]">refresh</span>
                  <span className="hidden sm:inline">Làm mới</span>
                </button>
              </div>
            </div>

            {/* Row 2: Date range */}
            <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3">
              <div className="flex items-center gap-2">
                <span className="text-sm text-slate-500 whitespace-nowrap">Từ ngày:</span>
                <input
                  type="datetime-local"
                  value={fromDate}
                  onChange={(e) => setFromDate(e.target.value)}
                  className="px-3 py-2 text-sm border border-slate-200 rounded-md bg-white text-slate-700 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                />
              </div>

              <div className="flex items-center gap-2">
                <span className="text-sm text-slate-500 whitespace-nowrap">Đến ngày:</span>
                <input
                  type="datetime-local"
                  value={toDate}
                  onChange={(e) => setToDate(e.target.value)}
                  className="px-3 py-2 text-sm border border-slate-200 rounded-md bg-white text-slate-700 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                />
              </div>

              {(fromDate || toDate) && (
                <button
                  onClick={() => {
                    setFromDate("");
                    setToDate("");
                  }}
                  className="flex items-center gap-1 px-3 py-2 text-sm text-slate-500 hover:text-red-600 transition-colors"
                >
                  <span className="material-symbols-outlined text-[16px]">close</span>
                  Xóa lọc
                </button>
              )}
            </div>
          </motion.div>

          {/* ── Table ─────────────────────────────────────────── */}
          <motion.div
            variants={item}
            className="bento-card rounded-md overflow-hidden bg-white flex flex-col flex-1 min-h-0"
          >
            <div className="overflow-auto flex-1">
              <table className="w-full text-left text-sm text-slate-600">
                <thead className="bg-slate-50 text-xs uppercase font-bold text-slate-500 border-b border-slate-200 sticky top-0 z-10">
                  <tr>
                    <th className="px-6 py-4">Người dùng</th>
                    <th className="px-6 py-4">Vai trò</th>
                    <th className="px-6 py-4">Hành động</th>
                    <th className="px-6 py-4">Mức độ</th>
                    <th className="px-6 py-4">Số lần</th>
                    <th className="px-6 py-4">Thời gian gần nhất</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {loading && !data ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-20 text-center text-slate-400">
                        <span className="material-symbols-outlined animate-spin text-[28px] mb-2 block">
                          progress_activity
                        </span>
                        Đang tải dữ liệu...
                      </td>
                    </tr>
                  ) : data && data.items.length > 0 ? (
                    data.items.map((log, idx) => (
                      <GroupedLogRow key={`${log.userId}-${log.action}-${idx}`} log={log} onClick={() => setSelectedLog(log)} />
                    ))
                  ) : (
                    <tr>
                      <td colSpan={6} className="px-6 py-20 text-center text-slate-400">
                        <span className="material-symbols-outlined text-[40px] mb-2 block text-slate-300">
                          search_off
                        </span>
                        Không tìm thấy bản ghi nào
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {data && data.totalPages > 1 && (
              <div className="p-4 border-t border-slate-200 flex items-center justify-between bg-white shrink-0">
                <span className="text-sm text-slate-500 hidden sm:inline">
                  Trang{" "}
                  <span className="font-medium text-slate-900">{page}</span>
                  {" / "}
                  <span className="font-medium text-slate-900">{totalPages}</span>
                  {" "}({data.totalGroups.toLocaleString()} nhóm)
                </span>
                <div className="flex gap-1 w-full sm:w-auto justify-center sm:justify-end">
                  <button
                    disabled={page <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    className="px-3 py-1 border border-slate-200 rounded hover:bg-slate-50 text-slate-600 text-sm disabled:opacity-50 transition-colors"
                  >
                    Trước
                  </button>
                  {getPageNumbers(page, totalPages).map((p, i) =>
                    p === "..." ? (
                      <span key={`dot-${i}`} className="px-2 py-1 text-slate-400 hidden sm:inline">
                        ...
                      </span>
                    ) : (
                      <button
                        key={p}
                        onClick={() => setPage(p as number)}
                        className={`px-3 py-1 rounded text-sm transition-colors ${
                          page === p
                            ? "bg-primary text-white hover:bg-primary-light"
                            : "border border-slate-200 hover:bg-slate-50 text-slate-600"
                        }`}
                      >
                        {p}
                      </button>
                    ),
                  )}
                  <button
                    disabled={page >= totalPages}
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    className="px-3 py-1 border border-slate-200 rounded hover:bg-slate-50 text-slate-600 text-sm disabled:opacity-50 transition-colors"
                  >
                    Sau
                  </button>
                </div>
              </div>
            )}
          </motion.div>
        </motion.div>
      </div>

      {/* ── Detail Modal ──────────────────────────────────── */}
      <AnimatePresence>
        {selectedLog && (
          <DetailModal
            log={selectedLog}
            errorDetails={errorDetails}
            loadingErrors={loadingErrors}
            errorLogDetail={errorLogDetail}
            loadingErrorLog={loadingErrorLog}
            onViewErrorLog={async (errorLogId) => {
              setLoadingErrorLog(true);
              setErrorLogDetail(null);
              try {
                const detail = await activityLogService.getErrorLogDetail(errorLogId);
                setErrorLogDetail(detail);
              } catch {
                showError("Không thể tải chi tiết lỗi.");
              } finally {
                setLoadingErrorLog(false);
              }
            }}
            onCloseErrorLog={() => setErrorLogDetail(null)}
            onClose={() => {
              setSelectedLog(null);
              setErrorDetails([]);
              setErrorLogDetail(null);
            }}
          />
        )}
      </AnimatePresence>
    </>
  );
}

// ── Helper Components ──────────────────────────────────────

function SeverityCard({
  label,
  count,
  icon,
  iconColor,
  iconBg,
  active,
  onClick,
}: {
  label: string;
  count: number;
  icon: string;
  iconColor: string;
  iconBg: string;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className={`bento-card p-4 rounded-md flex items-center gap-4 transition-all text-left ${
        active ? "ring-2 ring-primary ring-offset-1" : "hover:shadow-md"
      }`}
    >
      <div className={`${iconBg} p-2.5 rounded-lg`}>
        <span className={`material-symbols-outlined ${iconColor} text-[22px]`}>{icon}</span>
      </div>
      <div>
        <p className="text-xl font-bold text-slate-800">{count.toLocaleString()}</p>
        <p className="text-xs text-slate-500">{label}</p>
      </div>
    </button>
  );
}

function GroupedLogRow({ log, onClick }: { log: GroupedActivityLogItem; onClick: () => void }) {
  const roleColor = roleColors[log.activeRole] ?? roleColors.student;
  const sc = log.severityCounts;

  return (
    <motion.tr
      whileHover={{ backgroundColor: "rgb(248 250 252)" }}
      className="transition-colors group cursor-pointer"
      onClick={onClick}
    >
      {/* User */}
      <td className="px-6 py-4">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-primary/10 text-primary flex items-center justify-center text-xs font-bold shrink-0">
            {getInitials(log.userName)}
          </div>
          <div className="min-w-0">
            <p className="font-semibold text-slate-800 truncate">{log.userName}</p>
            <p className="text-xs text-slate-500 truncate">{log.userEmail ?? log.userId}</p>
          </div>
        </div>
      </td>
      {/* Role */}
      <td className="px-6 py-4">
        <span
          className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold border capitalize ${roleColor}`}
        >
          {log.activeRole}
        </span>
      </td>
      {/* Action */}
      <td className="px-6 py-4">
        <div>
          <p className="font-medium text-slate-800">{log.action}</p>
          {log.category && (
            <span className="inline-block mt-0.5 text-[10px] px-1.5 py-0.5 rounded bg-slate-100 text-slate-500 font-medium">
              {log.category}
            </span>
          )}
        </div>
      </td>
      {/* Severity mini badges */}
      <td className="px-6 py-4">
        <div className="flex items-center gap-1">
          {sc.info > 0 && <MiniSeverityBadge severity="info" count={sc.info} />}
          {sc.warning > 0 && <MiniSeverityBadge severity="warning" count={sc.warning} />}
          {sc.error > 0 && <MiniSeverityBadge severity="error" count={sc.error} />}
          {sc.critical > 0 && <MiniSeverityBadge severity="critical" count={sc.critical} />}
        </div>
      </td>
      {/* Count */}
      <td className="px-6 py-4">
        <span className="inline-flex items-center justify-center min-w-[2rem] px-2 py-0.5 rounded-full text-xs font-bold bg-primary/10 text-primary">
          {log.totalCount}
        </span>
      </td>
      {/* Latest Timestamp */}
      <td className="px-6 py-4 whitespace-nowrap text-xs text-slate-500">
        {formatTimestamp(log.latestTimestamp)}
      </td>
    </motion.tr>
  );
}

function MiniSeverityBadge({ severity, count }: { severity: string; count: number }) {
  const cfg = severityConfig[severity];
  if (!cfg) return null;
  return (
    <span className={`inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded text-[10px] font-bold ${cfg.bg} ${cfg.text} ${cfg.border} border`}>
      <span className="material-symbols-outlined text-[12px]">{cfg.icon}</span>
      {count}
    </span>
  );
}

function DetailModal({
  log,
  errorDetails,
  loadingErrors,
  errorLogDetail,
  loadingErrorLog,
  onViewErrorLog,
  onCloseErrorLog,
  onClose,
}: {
  log: GroupedActivityLogItem;
  errorDetails: ErrorDetailItem[];
  loadingErrors: boolean;
  errorLogDetail: ErrorLogDetail | null;
  loadingErrorLog: boolean;
  onViewErrorLog: (errorLogId: string) => void;
  onCloseErrorLog: () => void;
  onClose: () => void;
}) {
  const sc = log.severityCounts;
  const hasErrors = sc.error + sc.critical > 0;

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4"
      onClick={onClose}
    >
      <motion.div
        initial={{ scale: 0.95, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        exit={{ scale: 0.95, opacity: 0 }}
        className="bg-white rounded-xl shadow-2xl w-full max-w-3xl max-h-[85vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-slate-200">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-full bg-primary/10 text-primary flex items-center justify-center text-sm font-bold">
              {getInitials(log.userName)}
            </div>
            <div>
              <p className="font-bold text-slate-800">{log.userName}</p>
              <p className="text-sm text-slate-500">{log.action}</p>
              {log.category && (
                <span className="inline-block mt-0.5 text-[10px] px-1.5 py-0.5 rounded bg-slate-100 text-slate-500 font-medium">
                  {log.category}
                </span>
              )}
            </div>
          </div>
          <button onClick={onClose} className="p-1 hover:bg-slate-100 rounded-lg transition-colors">
            <span className="material-symbols-outlined text-slate-500">close</span>
          </button>
        </div>

        {/* Body */}
        <div className="p-6 space-y-6">
          {/* Summary row */}
          <div className="flex items-center gap-3">
            <span className="text-sm text-slate-500">Tổng số lần:</span>
            <span className="font-bold text-slate-800 text-lg">{log.totalCount}</span>
            <span className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold border capitalize ${roleColors[log.activeRole] ?? roleColors.student}`}>
              {log.activeRole}
            </span>
          </div>

          {/* Severity breakdown */}
          <div>
            <p className="text-sm font-semibold text-slate-700 mb-3">Phân bố mức độ</p>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
              {(["info", "warning", "error", "critical"] as const).map((sev) => {
                const cfg = severityConfig[sev];
                const count = sc[sev];
                return (
                  <div
                    key={sev}
                    className={`flex items-center gap-2 px-3 py-2 rounded-lg border ${cfg.bg} ${cfg.border}`}
                  >
                    <span className={`material-symbols-outlined text-[18px] ${cfg.text}`}>{cfg.icon}</span>
                    <div>
                      <p className={`text-lg font-bold ${cfg.text}`}>{count}</p>
                      <p className={`text-xs capitalize ${cfg.text}`}>{sev}</p>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Error details */}
          {hasErrors && (
            <div>
              <p className="text-sm font-semibold text-slate-700 mb-3">Chi tiết lỗi</p>
              {loadingErrors ? (
                <div className="flex items-center justify-center py-8 text-slate-400">
                  <span className="material-symbols-outlined animate-spin text-[24px] mr-2">progress_activity</span>
                  Đang tải...
                </div>
              ) : errorDetails.length > 0 ? (
                <div className="overflow-x-auto border border-slate-200 rounded-lg">
                  <table className="w-full text-sm text-left text-slate-600">
                    <thead className="bg-slate-50 text-xs uppercase font-bold text-slate-500 border-b border-slate-200">
                      <tr>
                        <th className="px-4 py-3">Thông báo lỗi</th>
                        <th className="px-4 py-3">Loại lỗi</th>
                        <th className="px-4 py-3">Số lần</th>
                        <th className="px-4 py-3">Lần gần nhất</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {errorDetails.map((err, i) => (
                        <tr key={i} className="hover:bg-slate-50 transition-colors">
                          <td className="px-4 py-3 max-w-[250px] truncate text-red-700 font-medium" title={err.message}>
                            {err.message}
                          </td>
                          <td className="px-4 py-3 whitespace-nowrap">
                            {err.errorType ? (
                              <span className="text-xs bg-slate-50 text-slate-600 px-2 py-1 rounded border border-slate-200 font-mono">
                                {err.errorType}
                              </span>
                            ) : (
                              <span className="text-xs text-slate-400">—</span>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            <span className="inline-flex items-center justify-center min-w-[1.5rem] px-1.5 py-0.5 rounded-full text-xs font-bold bg-red-100 text-red-700">
                              {err.count}
                            </span>
                          </td>
                          <td className="px-4 py-3 whitespace-nowrap text-xs text-slate-500">
                            {formatTimestamp(err.latestAt)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p className="text-sm text-slate-400 py-4 text-center">Không có chi tiết lỗi</p>
              )}
            </div>
          )}

          {/* Error Log Detail Panel */}
          {errorLogDetail && (
            <ErrorLogDetailPanel detail={errorLogDetail} onClose={onCloseErrorLog} />
          )}
          {loadingErrorLog && !errorLogDetail && (
            <div className="flex items-center justify-center py-6 text-slate-400">
              <span className="material-symbols-outlined animate-spin text-[24px] mr-2">progress_activity</span>
              Đang tải chi tiết lỗi...
            </div>
          )}
        </div>
      </motion.div>
    </motion.div>
  );
}

function ErrorLogDetailPanel({ detail, onClose }: { detail: ErrorLogDetail; onClose: () => void }) {
  return (
    <div className="border border-red-200 rounded-lg bg-red-50/30 overflow-hidden">
      <div className="flex items-center justify-between px-4 py-3 bg-red-50 border-b border-red-200">
        <div className="flex items-center gap-2">
          <span className="material-symbols-outlined text-red-600 text-[18px]">bug_report</span>
          <span className="text-sm font-semibold text-red-800">Chi tiết lỗi hệ thống</span>
        </div>
        <button onClick={onClose} className="p-0.5 hover:bg-red-100 rounded transition-colors">
          <span className="material-symbols-outlined text-red-500 text-[18px]">close</span>
        </button>
      </div>
      <div className="p-4 space-y-4">
        {/* Meta info */}
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div>
            <span className="text-slate-500">API Path:</span>
            <span className="ml-2 font-mono text-xs bg-slate-100 px-2 py-0.5 rounded text-slate-700">
              {detail.requestMethod} {detail.requestPath}
            </span>
          </div>
          <div>
            <span className="text-slate-500">Severity:</span>
            <span className={`ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-bold ${severityConfig[detail.severity]?.bg ?? ""} ${severityConfig[detail.severity]?.text ?? ""}`}>
              {detail.severity}
            </span>
          </div>
          {detail.routePath && (
            <div>
              <span className="text-slate-500">Route:</span>
              <span className="ml-2 text-xs text-slate-700">{detail.routePath}</span>
            </div>
          )}
          {detail.correlationId && (
            <div>
              <span className="text-slate-500">Correlation ID:</span>
              <span className="ml-2 font-mono text-xs text-slate-600">{detail.correlationId}</span>
            </div>
          )}
          <div>
            <span className="text-slate-500">Thời gian:</span>
            <span className="ml-2 text-xs text-slate-700">{formatTimestamp(detail.timestamp)}</span>
          </div>
        </div>

        {/* Error message */}
        <div>
          <p className="text-xs font-semibold text-slate-600 mb-1">Lỗi:</p>
          <div className="bg-white border border-red-200 rounded p-3">
            <p className="text-sm text-red-800 font-medium">{detail.errorMessage}</p>
            <p className="text-xs text-slate-500 mt-1 font-mono">{detail.errorType}</p>
          </div>
        </div>

        {/* Stack trace */}
        {detail.stackTrace && (
          <div>
            <p className="text-xs font-semibold text-slate-600 mb-1">Stack Trace:</p>
            <pre className="bg-slate-900 text-green-400 text-xs p-4 rounded-lg overflow-x-auto max-h-64 scrollbar-hide font-mono leading-relaxed">
              {detail.stackTrace}
            </pre>
          </div>
        )}

        {/* Inner exceptions */}
        {detail.innerExceptions.length > 0 && (
          <div>
            <p className="text-xs font-semibold text-slate-600 mb-1">Inner Exceptions ({detail.innerExceptions.length}):</p>
            <div className="space-y-2">
              {detail.innerExceptions.map((ie, i) => (
                <div key={i} className="bg-white border border-slate-200 rounded p-3">
                  <p className="text-sm text-red-700 font-medium">{ie.message}</p>
                  <p className="text-xs text-slate-500 font-mono">{ie.type}</p>
                  {ie.stackTrace && (
                    <pre className="mt-2 bg-slate-900 text-green-400 text-xs p-3 rounded overflow-x-auto max-h-32 scrollbar-hide font-mono">
                      {ie.stackTrace}
                    </pre>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// ── Utilities ───────────────────────────────────────────────

function getInitials(name: string): string {
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
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();
  const diffMin = Math.floor(diffMs / 60000);

  if (diffMin < 1) return "Vừa xong";
  if (diffMin < 60) return `${diffMin} phút trước`;
  if (diffMin < 1440) return `${Math.floor(diffMin / 60)} giờ trước`;
  if (diffMin < 10080) return `${Math.floor(diffMin / 1440)} ngày trước`;

  return (
    d.toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" }) +
    " " +
    d.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })
  );
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
