import { useState, useEffect, useCallback } from "react";
import type { ReactNode } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Header } from "@/components/layout";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { activityLogService } from "@/lib/activityLogs/activityLogService";
import type {
  ActivityLogItem,
  ActivityLogResponse,
  ActivityLogSummary,
  ErrorLogDetail,
  ErrorLogItem,
  ErrorLogResponse,
} from "@/types";

// ── Design tokens ─────────────────────────────────────────────────────────────
const ROLE_CHIP: Record<string, string> = {
  admin:             "bg-[#EDE9FE] text-[#5B21B6]",
  mentor:            "bg-[#D6E8FA] text-[#1D4ED8]",
  student:           "bg-[#DCFCE7] text-[#15803D]",
  "department-head": "bg-[#FEF9C3] text-[#A16207]",
  evaluator:         "bg-[#FCE7F3] text-[#9D174D]",
};

const ROLE_DOT: Record<string, string> = {
  "":                "#0C6EDB",
  admin:             "#5B21B6",
  mentor:            "#1D4ED8",
  student:           "#16A34A",
  "department-head": "#A16207",
  evaluator:         "#9D174D",
};

const ROLE_FILTERS = [
  { key: "",        label: "Tất cả" },
  { key: "admin",   label: "Admin" },
  { key: "mentor",  label: "Giảng viên" },
  { key: "student", label: "Sinh viên" },
];

const SEVERITY_FILTERS = [
  { key: "",         label: "Tất cả",  color: "#0C6EDB" },
  { key: "critical", label: "Critical", color: "#7C3AED" },
  { key: "error",    label: "Error",    color: "#DC2626" },
  { key: "warning",  label: "Warning",  color: "#D97706" },
];

const CLEAR_RANGES = [
  { value: 1,  label: "Cũ hơn 1 ngày" },
  { value: 4,  label: "Cũ hơn 4 ngày" },
  { value: 7,  label: "Cũ hơn 1 tuần (7 ngày)" },
  { value: 14, label: "Cũ hơn 2 tuần (14 ngày)" },
  { value: 28, label: "Cũ hơn 4 tuần (28 ngày)" },
  { value: 0,  label: "Toàn bộ nhật ký" },
] as const;

const PAGE_SIZE = 20;

function durationColor(ms: number): string {
  if (ms > 1000) return "text-[#DC2626]";
  if (ms > 200)  return "text-[#D97706]";
  return "text-[#6B7280]";
}

function stripeColor(status: "Success" | "Failure"): string {
  return status === "Success" ? "#16A34A" : "#DC2626";
}

function severityStripe(severity: string): string {
  const map: Record<string, string> = {
    critical: "#7C3AED",
    error:    "#DC2626",
    warning:  "#D97706",
    info:     "#0C6EDB",
  };
  return map[severity] ?? "#6B7280";
}

function severityBadgeClass(severity: string): string {
  const map: Record<string, string> = {
    critical: "bg-[#EDE9FE] text-[#7C3AED] border-[#DDD6FE]",
    error:    "bg-[#FEE2E2] text-[#DC2626] border-[#FCA5A5]",
    warning:  "bg-[#FEF3C7] text-[#D97706] border-[#FDE68A]",
    info:     "bg-[#D6E8FA] text-[#0C6EDB] border-[#93C5FD]",
  };
  return map[severity] ?? "bg-slate-100 text-slate-600 border-slate-200";
}

// ── Main Page ─────────────────────────────────────────────────────────────────
export function ActivityLogsPage() {
  const [activeTab, setActiveTab] = useState<"activity" | "errors">("activity");

  // Shared date filters
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate]     = useState("");

  // Activity tab
  const [activeRole,       setActiveRole]       = useState("");
  const [activeStatus,     setActiveStatus]     = useState("");
  const [search,           setSearch]           = useState("");
  const [debouncedSearch,  setDebouncedSearch]  = useState("");
  const [page,             setPage]             = useState(1);
  const [data,             setData]             = useState<ActivityLogResponse | null>(null);
  const [summary,          setSummary]          = useState<ActivityLogSummary | null>(null);
  const [loading,          setLoading]          = useState(false);
  const [isLive,           setIsLive]           = useState(true);

  // Error tab
  const [activeSeverity,       setActiveSeverity]       = useState("");
  const [errorSearch,          setErrorSearch]          = useState("");
  const [debouncedErrorSearch, setDebouncedErrorSearch] = useState("");
  const [errorPage,            setErrorPage]            = useState(1);
  const [errorData,            setErrorData]            = useState<ErrorLogResponse | null>(null);
  const [errorLoading,         setErrorLoading]         = useState(false);

  // Detail panel
  const [selectedLog,    setSelectedLog]    = useState<ActivityLogItem | null>(null);
  const [selectedError,  setSelectedError]  = useState<ErrorLogItem | null>(null);
  const [errorDetail,    setErrorDetail]    = useState<ErrorLogDetail | null>(null);
  const [loadingDetail,  setLoadingDetail]  = useState(false);

  // Clear modal
  const [showClearModal, setShowClearModal] = useState(false);
  const [clearRange,     setClearRange]     = useState<number | null>(null);
  const [clearing,       setClearing]       = useState(false);

  const { showError } = useSystemError();

  // Debounce
  useEffect(() => {
    const id = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(id);
  }, [search]);
  useEffect(() => {
    const id = setTimeout(() => setDebouncedErrorSearch(errorSearch), 400);
    return () => clearTimeout(id);
  }, [errorSearch]);

  // Reset page on filter change
  useEffect(() => { setPage(1); },      [activeRole, activeStatus, debouncedSearch, fromDate, toDate]);
  useEffect(() => { setErrorPage(1); }, [activeSeverity, debouncedErrorSearch, fromDate, toDate]);

  // Clear panel on tab switch
  useEffect(() => {
    setSelectedLog(null);
    setSelectedError(null);
    setErrorDetail(null);
  }, [activeTab]);

  const fetchActivity = useCallback(async () => {
    setLoading(true);
    try {
      const [logsResult, summaryResult] = await Promise.all([
        activityLogService.getLogs({
          role:     activeRole   || undefined,
          status:   activeStatus || undefined,
          search:   debouncedSearch || undefined,
          from:     fromDate || undefined,
          to:       toDate   || undefined,
          page,
          pageSize: PAGE_SIZE,
        }),
        activityLogService.getSummary(
          activeRole || undefined,
          fromDate   || undefined,
          toDate     || undefined,
        ),
      ]);
      setData(logsResult);
      setSummary(summaryResult);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Đã xảy ra lỗi khi tải nhật ký hoạt động.");
    } finally {
      setLoading(false);
    }
  }, [activeRole, activeStatus, debouncedSearch, fromDate, toDate, page, showError]);

  const fetchErrors = useCallback(async () => {
    setErrorLoading(true);
    try {
      const result = await activityLogService.getErrorLogs({
        severity: activeSeverity      || undefined,
        search:   debouncedErrorSearch || undefined,
        from:     fromDate || undefined,
        to:       toDate   || undefined,
        page:     errorPage,
        pageSize: PAGE_SIZE,
      });
      setErrorData(result);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Đã xảy ra lỗi khi tải nhật ký lỗi.");
    } finally {
      setErrorLoading(false);
    }
  }, [activeSeverity, debouncedErrorSearch, fromDate, toDate, errorPage, showError]);

  useEffect(() => { fetchActivity(); }, [fetchActivity]);
  useEffect(() => { if (activeTab === "errors") fetchErrors(); }, [activeTab, fetchErrors]);

  // Live polling every 30s
  useEffect(() => {
    if (!isLive) return;
    const id = setInterval(fetchActivity, 30_000);
    return () => clearInterval(id);
  }, [isLive, fetchActivity]);

  const handleViewErrorDetail = useCallback(async (id: string) => {
    setLoadingDetail(true);
    setErrorDetail(null);
    try {
      const detail = await activityLogService.getErrorLogDetail(id);
      setErrorDetail(detail);
    } catch {
      showError("Không thể tải chi tiết lỗi.");
    } finally {
      setLoadingDetail(false);
    }
  }, [showError]);

  const handleCrossRefToError = (correlationId: string) => {
    setSelectedLog(null);
    setActiveTab("errors");
    setErrorSearch(correlationId);
  };

  const handleCrossRefToActivity = (correlationId: string) => {
    setSelectedError(null);
    setErrorDetail(null);
    setActiveTab("activity");
    setSearch(correlationId);
  };

  const handleClear = async () => {
    if (clearRange === null) return;
    setClearing(true);
    try {
      const days = clearRange === 0 ? undefined : clearRange;
      if (activeTab === "activity") {
        await activityLogService.clearActivityLogs(days);
        await fetchActivity();
      } else {
        await activityLogService.clearErrorLogs(days);
        await fetchErrors();
      }
      setShowClearModal(false);
      setClearRange(null);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể xóa nhật ký.");
    } finally {
      setClearing(false);
    }
  };

  const openClearModal = () => { setClearRange(null); setShowClearModal(true); };

  const panelOpen = selectedLog !== null || selectedError !== null;

  return (
    <>
      <Header title="Nhật Ký Hệ Thống" showSearch={false} />

      <div className="flex flex-col flex-1 min-h-0 bg-[#EEF2F8]">
        {/* ── Vitals strip + tab bar ──────────────────────────────── */}
        <div className="shrink-0 px-4 pt-4 space-y-3">
          <VitalsStrip
            summary={summary}
            errorTotal={errorData?.totalCount}
            isLive={isLive}
            onToggleLive={() => setIsLive((v) => !v)}
          />

          <div className="flex gap-1 p-1 bg-white rounded-lg border border-[#D8DDE8] shadow-sm w-fit">
            <button
              onClick={() => setActiveTab("activity")}
              className={`flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md transition-all ${
                activeTab === "activity"
                  ? "bg-[#0C6EDB] text-white shadow-sm"
                  : "text-[#3D4552] hover:bg-[#EEF2F8]"
              }`}
            >
              <span className="material-symbols-outlined text-[16px]">history</span>
              Nhật ký hoạt động
            </button>
            <button
              onClick={() => setActiveTab("errors")}
              className={`flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md transition-all ${
                activeTab === "errors"
                  ? "bg-[#DC2626] text-white shadow-sm"
                  : "text-[#3D4552] hover:bg-[#EEF2F8]"
              }`}
            >
              <span className="material-symbols-outlined text-[16px]">bug_report</span>
              Nhật ký lỗi
            </button>
          </div>
        </div>

        {/* ── Three-zone body ─────────────────────────────────────── */}
        <div className="flex flex-1 min-h-0 mt-3">
          {/* Left filter rail */}
          <aside className="w-[220px] shrink-0 border-r border-[#D8DDE8] bg-white overflow-y-auto">
            {activeTab === "activity" ? (
              <ActivityFilterRail
                search={search}
                onSearch={setSearch}
                activeRole={activeRole}
                onRole={setActiveRole}
                activeStatus={activeStatus}
                onStatus={setActiveStatus}
                fromDate={fromDate}
                toDate={toDate}
                onFrom={setFromDate}
                onTo={setToDate}
                roleCounts={summary?.roleCounts}
                total={summary?.total ?? 0}
                successCount={summary?.success ?? 0}
                failureCount={summary?.failure ?? 0}
              />
            ) : (
              <ErrorFilterRail
                search={errorSearch}
                onSearch={setErrorSearch}
                activeSeverity={activeSeverity}
                onSeverity={setActiveSeverity}
                fromDate={fromDate}
                toDate={toDate}
                onFrom={setFromDate}
                onTo={setToDate}
              />
            )}
          </aside>

          {/* Center feed */}
          <main className="flex-1 flex flex-col min-h-0 overflow-y-auto">
            {activeTab === "activity" ? (
              <ActivityFeed
                data={data}
                loading={loading}
                selectedId={selectedLog?.id ?? null}
                onSelect={setSelectedLog}
                page={page}
                onPageChange={setPage}
                onRefresh={fetchActivity}
                onClear={openClearModal}
              />
            ) : (
              <ErrorStream
                data={errorData}
                loading={errorLoading}
                selectedId={selectedError?.id ?? null}
                onSelect={(err) => {
                  setSelectedError(err);
                  handleViewErrorDetail(err.id);
                }}
                page={errorPage}
                onPageChange={setErrorPage}
                onRefresh={fetchErrors}
                onClear={openClearModal}
              />
            )}
          </main>

          {/* Right detail panel */}
          <AnimatePresence>
            {panelOpen && (
              <motion.aside
                key="detail-panel"
                initial={{ width: 0, opacity: 0 }}
                animate={{ width: 400, opacity: 1 }}
                exit={{ width: 0, opacity: 0 }}
                transition={{ type: "tween", duration: 0.2 }}
                className="shrink-0 border-l border-[#D8DDE8] bg-white overflow-hidden"
              >
                <div className="w-[400px] h-full overflow-y-auto">
                  {selectedLog && (
                    <ActivityDetailPanel
                      log={selectedLog}
                      onClose={() => setSelectedLog(null)}
                      onCrossRef={handleCrossRefToError}
                    />
                  )}
                  {selectedError && (
                    <ErrorDetailPanel
                      log={selectedError}
                      detail={errorDetail}
                      loading={loadingDetail}
                      onClose={() => { setSelectedError(null); setErrorDetail(null); }}
                      onCrossRef={handleCrossRefToActivity}
                    />
                  )}
                </div>
              </motion.aside>
            )}
          </AnimatePresence>
        </div>
      </div>

      {/* ── Clear confirm modal ─────────────────────────────────── */}
      <AnimatePresence>
        {showClearModal && (
          <ClearConfirmModal
            tab={activeTab}
            range={clearRange}
            onRangeChange={setClearRange}
            clearing={clearing}
            onConfirm={handleClear}
            onClose={() => { setShowClearModal(false); setClearRange(null); }}
          />
        )}
      </AnimatePresence>
    </>
  );
}

// ── Vitals Strip ──────────────────────────────────────────────────────────────
function VitalsStrip({
  summary, errorTotal, isLive, onToggleLive,
}: {
  summary: ActivityLogSummary | null;
  errorTotal?: number;
  isLive: boolean;
  onToggleLive: () => void;
}) {
  const total   = summary?.total   ?? 0;
  const success = summary?.success ?? 0;
  const failure = summary?.failure ?? 0;
  const rate    = total > 0 ? ((success / total) * 100).toFixed(1) : "—";
  const errCnt  = errorTotal ?? 0;

  return (
    <div className="flex items-start gap-3">
      <div className="grid grid-cols-4 gap-3 flex-1">
        <VitalCard
          label="Tổng hành động"
          value={total.toLocaleString("vi-VN")}
          sub="24 giờ qua"
          stripe="#0C6EDB"
        />
        <VitalCard
          label="Thành công"
          value={success.toLocaleString("vi-VN")}
          sub={`${rate}% tỷ lệ`}
          stripe="#16A34A"
          valueColor="#16A34A"
        />
        <VitalCard
          label="Thất bại"
          value={failure.toLocaleString("vi-VN")}
          sub={total > 0 ? `${(100 - parseFloat(rate === "—" ? "0" : rate)).toFixed(1)}% tỷ lệ lỗi` : ""}
          stripe="#DC2626"
          valueColor={failure > 0 ? "#DC2626" : undefined}
        />
        <VitalCard
          label="Lỗi ứng dụng"
          value={String(errCnt)}
          sub="Error logs"
          stripe="#D97706"
          valueColor={errCnt > 0 ? "#D97706" : undefined}
        />
      </div>

      <button
        onClick={onToggleLive}
        className={`flex items-center gap-1.5 px-3 py-2 rounded-lg border text-xs font-semibold transition-all shrink-0 mt-1 ${
          isLive
            ? "bg-[#CCEEF6] border-[#A5D8E6] text-[#0891B2]"
            : "bg-white border-[#D8DDE8] text-[#6B7280]"
        }`}
      >
        <span
          className={`w-1.5 h-1.5 rounded-full shrink-0 ${isLive ? "bg-[#0891B2] animate-pulse" : "bg-[#9CA3AF]"}`}
        />
        {isLive ? "Live" : "Paused"}
      </button>
    </div>
  );
}

function VitalCard({ label, value, sub, stripe, valueColor }: {
  label: string; value: string; sub: string; stripe: string; valueColor?: string;
}) {
  return (
    <div className="flex bg-white rounded-md border border-[#E8EBF2] shadow-sm overflow-hidden">
      <div className="w-[3px] shrink-0" style={{ background: stripe }} />
      <div className="px-4 py-3 flex-1 min-w-0">
        <p className="text-[10px] font-semibold uppercase tracking-wider text-[#6B7280] mb-1">{label}</p>
        <p
          className="text-2xl font-bold tabular-nums leading-none"
          style={{ color: valueColor ?? "#0D1117" }}
        >
          {value}
        </p>
        {sub && <p className="text-[11px] text-[#6B7280] mt-1 truncate">{sub}</p>}
      </div>
    </div>
  );
}

// ── Filter Rail (Activity) ────────────────────────────────────────────────────
function ActivityFilterRail({
  search, onSearch,
  activeRole, onRole,
  activeStatus, onStatus,
  fromDate, toDate, onFrom, onTo,
  roleCounts, total, successCount, failureCount,
}: {
  search: string;        onSearch: (v: string) => void;
  activeRole: string;    onRole:   (v: string) => void;
  activeStatus: string;  onStatus: (v: string) => void;
  fromDate: string;      toDate: string;
  onFrom: (v: string) => void; onTo: (v: string) => void;
  roleCounts?: Record<string, number>;
  total: number; successCount: number; failureCount: number;
}) {
  return (
    <div className="p-3 space-y-4">
      <div className="relative">
        <span className="absolute left-2.5 top-1/2 -translate-y-1/2 material-symbols-outlined text-[#6B7280] text-[16px]">search</span>
        <input
          className="w-full pl-8 pr-3 py-2 text-[13px] bg-[#EEF2F8] border border-[#D8DDE8] rounded-md text-[#0D1117] placeholder-[#6B7280] focus:outline-none focus:ring-1 focus:ring-[#0C6EDB] focus:border-[#0C6EDB]"
          placeholder="Tìm kiếm…"
          value={search}
          onChange={(e) => onSearch(e.target.value)}
        />
      </div>

      <div>
        <p className="text-[10px] font-semibold uppercase tracking-wider text-[#6B7280] px-1 mb-2">Vai trò</p>
        <div className="space-y-0.5">
          {ROLE_FILTERS.map((r) => {
            const count = r.key === "" ? total : (roleCounts?.[r.key] ?? 0);
            const isActive = activeRole === r.key;
            return (
              <button
                key={r.key}
                onClick={() => onRole(r.key)}
                className={`w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-[12px] transition-all ${
                  isActive
                    ? "bg-[#D6E8FA] text-[#0C6EDB] font-medium"
                    : "text-[#3D4552] hover:bg-[#EEF2F8] hover:text-[#0D1117]"
                }`}
              >
                <span
                  className="w-[7px] h-[7px] rounded-full shrink-0"
                  style={{ background: ROLE_DOT[r.key] ?? "#6B7280" }}
                />
                <span className="flex-1 text-left">{r.label}</span>
                <span className="text-[11px] tabular-nums bg-[#EEF2F8] text-[#6B7280] px-1.5 py-0.5 rounded-full">
                  {count.toLocaleString()}
                </span>
              </button>
            );
          })}
        </div>
      </div>

      <hr className="border-[#D8DDE8]" />

      <div>
        <p className="text-[10px] font-semibold uppercase tracking-wider text-[#6B7280] px-1 mb-2">Trạng thái</p>
        <div className="space-y-0.5">
          {[
            { key: "Success", label: "Thành công", count: successCount, color: "#16A34A" },
            { key: "Failure", label: "Thất bại",   count: failureCount, color: "#DC2626" },
          ].map((s) => (
            <button
              key={s.key}
              onClick={() => onStatus(activeStatus === s.key ? "" : s.key)}
              className={`w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-[12px] transition-all ${
                activeStatus === s.key
                  ? "bg-[#D6E8FA] text-[#0C6EDB] font-medium"
                  : "text-[#3D4552] hover:bg-[#EEF2F8]"
              }`}
            >
              <span className="w-[7px] h-[7px] rounded-full shrink-0" style={{ background: s.color }} />
              <span className="flex-1 text-left">{s.label}</span>
              <span className="text-[11px] tabular-nums bg-[#EEF2F8] text-[#6B7280] px-1.5 py-0.5 rounded-full">
                {s.count.toLocaleString()}
              </span>
            </button>
          ))}
        </div>
      </div>

      <hr className="border-[#D8DDE8]" />
      <DateRangeFields fromDate={fromDate} toDate={toDate} onFrom={onFrom} onTo={onTo} />
    </div>
  );
}

// ── Filter Rail (Errors) ──────────────────────────────────────────────────────
function ErrorFilterRail({
  search, onSearch, activeSeverity, onSeverity,
  fromDate, toDate, onFrom, onTo,
}: {
  search: string;         onSearch:    (v: string) => void;
  activeSeverity: string; onSeverity:  (v: string) => void;
  fromDate: string;       toDate: string;
  onFrom: (v: string) => void; onTo: (v: string) => void;
}) {
  return (
    <div className="p-3 space-y-4">
      <div className="relative">
        <span className="absolute left-2.5 top-1/2 -translate-y-1/2 material-symbols-outlined text-[#6B7280] text-[16px]">search</span>
        <input
          className="w-full pl-8 pr-3 py-2 text-[13px] bg-[#EEF2F8] border border-[#D8DDE8] rounded-md text-[#0D1117] placeholder-[#6B7280] focus:outline-none focus:ring-1 focus:ring-[#0C6EDB] focus:border-[#0C6EDB]"
          placeholder="Tìm lỗi…"
          value={search}
          onChange={(e) => onSearch(e.target.value)}
        />
      </div>

      <div>
        <p className="text-[10px] font-semibold uppercase tracking-wider text-[#6B7280] px-1 mb-2">Mức độ</p>
        <div className="space-y-0.5">
          {SEVERITY_FILTERS.map((s) => (
            <button
              key={s.key}
              onClick={() => onSeverity(s.key)}
              className={`w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-[12px] transition-all ${
                activeSeverity === s.key
                  ? "bg-[#D6E8FA] text-[#0C6EDB] font-medium"
                  : "text-[#3D4552] hover:bg-[#EEF2F8]"
              }`}
            >
              <span className="w-[7px] h-[7px] rounded-full shrink-0" style={{ background: s.color }} />
              <span>{s.label}</span>
            </button>
          ))}
        </div>
      </div>

      <hr className="border-[#D8DDE8]" />
      <DateRangeFields fromDate={fromDate} toDate={toDate} onFrom={onFrom} onTo={onTo} />
    </div>
  );
}

function DateRangeFields({ fromDate, toDate, onFrom, onTo }: {
  fromDate: string; toDate: string;
  onFrom: (v: string) => void; onTo: (v: string) => void;
}) {
  return (
    <div className="space-y-2">
      <p className="text-[10px] font-semibold uppercase tracking-wider text-[#6B7280] px-1">Thời gian</p>
      <div>
        <label className="block text-[11px] font-medium text-[#3D4552] mb-1">Từ ngày</label>
        <input
          type="datetime-local"
          value={fromDate}
          onChange={(e) => onFrom(e.target.value)}
          className="w-full px-2 py-1.5 text-[11px] font-mono bg-[#EEF2F8] border border-[#D8DDE8] rounded-md text-[#0D1117] focus:outline-none focus:ring-1 focus:ring-[#0C6EDB]"
        />
      </div>
      <div>
        <label className="block text-[11px] font-medium text-[#3D4552] mb-1">Đến ngày</label>
        <input
          type="datetime-local"
          value={toDate}
          onChange={(e) => onTo(e.target.value)}
          className="w-full px-2 py-1.5 text-[11px] font-mono bg-[#EEF2F8] border border-[#D8DDE8] rounded-md text-[#0D1117] focus:outline-none focus:ring-1 focus:ring-[#0C6EDB]"
        />
      </div>
      {(fromDate || toDate) && (
        <button
          onClick={() => { onFrom(""); onTo(""); }}
          className="w-full py-1.5 text-[12px] text-[#DC2626] hover:bg-[#FEE2E2] rounded-md transition-colors"
        >
          Xóa lọc ngày
        </button>
      )}
    </div>
  );
}

// ── Activity Feed ─────────────────────────────────────────────────────────────
function ActivityFeed({
  data, loading, selectedId, onSelect, page, onPageChange, onRefresh, onClear,
}: {
  data: ActivityLogResponse | null;
  loading: boolean;
  selectedId: string | null;
  onSelect: (log: ActivityLogItem) => void;
  page: number;
  onPageChange: (p: number) => void;
  onRefresh: () => void;
  onClear: () => void;
}) {
  return (
    <div className="flex flex-col flex-1 p-3 gap-2">
      <div className="flex items-center justify-between shrink-0">
        <span className="text-[12px] text-[#6B7280] tabular-nums">
          {data ? `${data.totalCount.toLocaleString("vi-VN")} bản ghi` : "Đang tải…"}
        </span>
        <div className="flex items-center gap-2">
          <button
            onClick={onRefresh}
            disabled={loading}
            className="flex items-center gap-1 px-3 py-1.5 text-[12px] font-medium bg-white border border-[#D8DDE8] rounded-md text-[#3D4552] hover:bg-[#EEF2F8] transition-colors disabled:opacity-50"
          >
            <span className={`material-symbols-outlined text-[14px] ${loading ? "animate-spin" : ""}`}>refresh</span>
            Làm mới
          </button>
          <button
            onClick={onClear}
            className="flex items-center gap-1 px-3 py-1.5 text-[12px] font-medium bg-white border border-[#FCA5A5] rounded-md text-[#DC2626] hover:bg-[#FEE2E2] transition-colors"
          >
            <span className="material-symbols-outlined text-[14px]">delete_sweep</span>
            Xóa nhật ký
          </button>
        </div>
      </div>

      <div className="flex flex-col gap-[2px]">
        {loading && !data ? (
          <FeedLoader />
        ) : data && data.items.length > 0 ? (
          data.items.map((log) => (
            <ActivityFeedRow
              key={log.id}
              log={log}
              isSelected={selectedId === log.id}
              onClick={() => onSelect(log)}
            />
          ))
        ) : (
          <FeedEmpty message="Không tìm thấy bản ghi nào" icon="search_off" />
        )}
      </div>

      {data && data.totalCount > 0 && (
        <LogPagination
          page={page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          label="bản ghi"
          onPageChange={onPageChange}
        />
      )}
    </div>
  );
}

function ActivityFeedRow({ log, isSelected, onClick }: {
  log: ActivityLogItem; isSelected: boolean; onClick: () => void;
}) {
  return (
    <div
      onClick={onClick}
      className={`flex items-stretch bg-white border rounded-md overflow-hidden cursor-pointer transition-all ${
        isSelected
          ? "border-[#0C6EDB] shadow-sm ring-1 ring-[#0C6EDB]/20"
          : "border-[#E8EBF2] hover:border-[#D8DDE8] hover:shadow-sm"
      }`}
    >
      <div className="w-[3px] shrink-0" style={{ background: stripeColor(log.status) }} />

      <div className="flex flex-1 items-center gap-3 px-3 py-2.5 min-w-0">
        <span
          className={`text-[10px] font-bold px-1.5 py-0.5 rounded shrink-0 capitalize ${ROLE_CHIP[log.role] ?? "bg-[#EEF2F8] text-[#6B7280]"}`}
        >
          {log.role === "department-head" ? "Dept" : log.role}
        </span>
        <div className="min-w-0 flex-1">
          <p className="text-[13px] font-semibold text-[#0D1117] truncate">
            {log.actionName || log.actionCode}
          </p>
          <p className="text-[11px] text-[#6B7280] truncate">
            {log.userEmail ?? log.userName}
          </p>
        </div>
      </div>

      <div className="flex flex-col items-end justify-center gap-0.5 px-3 py-2.5 shrink-0">
        <span className="text-[11px] text-[#6B7280] font-mono tabular-nums">
          {formatTime(log.timestamp)}
        </span>
        <span className={`text-[11px] font-mono tabular-nums ${durationColor(log.durationMs)}`}>
          {log.durationMs.toLocaleString()} ms
        </span>
      </div>
    </div>
  );
}

// ── Error Stream ──────────────────────────────────────────────────────────────
function ErrorStream({
  data, loading, selectedId, onSelect, page, onPageChange, onRefresh, onClear,
}: {
  data: ErrorLogResponse | null;
  loading: boolean;
  selectedId: string | null;
  onSelect: (err: ErrorLogItem) => void;
  page: number;
  onPageChange: (p: number) => void;
  onRefresh: () => void;
  onClear: () => void;
}) {
  return (
    <div className="flex flex-col flex-1 p-3 gap-2">
      <div className="flex items-center justify-between shrink-0">
        <span className="text-[12px] text-[#6B7280] tabular-nums">
          {data ? `${data.totalCount.toLocaleString()} lỗi` : "Đang tải…"}
        </span>
        <div className="flex items-center gap-2">
          <button
            onClick={onRefresh}
            disabled={loading}
            className="flex items-center gap-1 px-3 py-1.5 text-[12px] font-medium bg-white border border-[#D8DDE8] rounded-md text-[#3D4552] hover:bg-[#EEF2F8] transition-colors disabled:opacity-50"
          >
            <span className={`material-symbols-outlined text-[14px] ${loading ? "animate-spin" : ""}`}>refresh</span>
            Làm mới
          </button>
          <button
            onClick={onClear}
            className="flex items-center gap-1 px-3 py-1.5 text-[12px] font-medium bg-white border border-[#FCA5A5] rounded-md text-[#DC2626] hover:bg-[#FEE2E2] transition-colors"
          >
            <span className="material-symbols-outlined text-[14px]">delete_sweep</span>
            Xóa nhật ký lỗi
          </button>
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        {loading && !data ? (
          <FeedLoader />
        ) : data && data.items.length > 0 ? (
          data.items.map((err) => (
            <ErrorCard
              key={err.id}
              log={err}
              isSelected={selectedId === err.id}
              onClick={() => onSelect(err)}
            />
          ))
        ) : (
          <FeedEmpty message="Không có lỗi nào" icon="check_circle" />
        )}
      </div>

      {data && data.totalCount > 0 && (
        <LogPagination
          page={page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          label="lỗi"
          onPageChange={onPageChange}
        />
      )}
    </div>
  );
}

function ErrorCard({ log, isSelected, onClick }: {
  log: ErrorLogItem; isSelected: boolean; onClick: () => void;
}) {
  const isCritical = log.severity === "critical";
  return (
    <div
      onClick={onClick}
      className={`border rounded-md overflow-hidden cursor-pointer transition-all ${
        isSelected
          ? "border-[#0C6EDB] shadow-sm ring-1 ring-[#0C6EDB]/20 bg-white"
          : isCritical
          ? "border-[#DDD6FE] bg-[#FAF5FF] hover:border-[#C4B5FD]"
          : "border-[#E8EBF2] bg-white hover:border-[#D8DDE8] hover:shadow-sm"
      }`}
    >
      <div className="flex items-stretch">
        <div className="w-[4px] shrink-0" style={{ background: severityStripe(log.severity) }} />
        <div className="flex-1 px-3.5 py-2.5 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <span className={`text-[10px] font-bold uppercase px-2 py-0.5 rounded border ${severityBadgeClass(log.severity)}`}>
              {log.severity}
            </span>
            <span className="text-[11px] text-[#6B7280]">{log.source}</span>
          </div>
          <p className="text-[12px] font-semibold font-mono text-[#0D1117]">{log.errorType}</p>
          <p className="text-[11px] text-[#3D4552] truncate mt-0.5">{log.errorMessage}</p>
        </div>
        <div
          className={`flex flex-col items-end justify-center px-3 py-2.5 border-l border-[#E8EBF2] shrink-0 ${
            isCritical ? "bg-[#F3E8FF]" : "bg-[#F5F7FB]"
          }`}
        >
          <span className="text-[11px] text-[#6B7280] font-mono tabular-nums">{formatTime(log.timestamp)}</span>
          <span className="material-symbols-outlined text-[#6B7280] text-[16px] mt-1">chevron_right</span>
        </div>
      </div>

      <div className="flex items-center gap-3 px-3.5 py-1.5 border-t border-[#E8EBF2] bg-[#F5F7FB]">
        <span className="text-[10px] font-mono text-[#6B7280]">
          <span className="font-semibold text-[#3D4552]">{log.requestMethod}</span>{" "}
          {log.requestPath}
        </span>
        {log.correlationId && (
          <span className="text-[10px] font-mono text-[#6B7280] ml-auto truncate max-w-[140px]">
            {log.correlationId.slice(0, 16)}…
          </span>
        )}
      </div>
    </div>
  );
}

// ── Detail Panels ─────────────────────────────────────────────────────────────
function ActivityDetailPanel({ log, onClose, onCrossRef }: {
  log: ActivityLogItem;
  onClose: () => void;
  onCrossRef: (correlationId: string) => void;
}) {
  const isFailure = log.status === "Failure";
  return (
    <div className="flex flex-col h-full">
      <div className="flex items-start justify-between p-5 border-b border-[#D8DDE8] shrink-0">
        <div>
          <p className="text-[14px] font-bold text-[#0D1117]">{log.actionName || log.actionCode}</p>
          <p className="text-[12px] text-[#6B7280] mt-0.5">Chi tiết hành động</p>
        </div>
        <button
          onClick={onClose}
          className="w-7 h-7 flex items-center justify-center rounded-md border border-[#D8DDE8] bg-[#EEF2F8] text-[#6B7280] hover:bg-[#D8DDE8] transition-colors shrink-0"
        >
          <span className="material-symbols-outlined text-[16px]">close</span>
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-5 space-y-4">
        <span
          className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-md text-[12px] font-semibold border ${
            isFailure
              ? "bg-[#FEE2E2] text-[#DC2626] border-[#FCA5A5]"
              : "bg-[#DCFCE7] text-[#16A34A] border-[#86EFAC]"
          }`}
        >
          <span className="material-symbols-outlined text-[14px]">{isFailure ? "cancel" : "check_circle"}</span>
          {isFailure ? "Thất bại" : "Thành công"}
        </span>

        <div className="space-y-3">
          <PanelField label="Người dùng">
            <span className="font-medium text-[#0D1117]">{log.userName}</span>
            <span className="block text-[11px] text-[#6B7280]">{log.userEmail ?? log.userId}</span>
          </PanelField>
          <PanelField label="Vai trò">
            <span
              className={`text-[11px] font-bold uppercase px-2 py-0.5 rounded capitalize ${ROLE_CHIP[log.role] ?? "bg-[#EEF2F8] text-[#6B7280]"}`}
            >
              {log.role}
            </span>
          </PanelField>
        </div>

        <hr className="border-[#E8EBF2]" />

        <div className="space-y-3">
          <PanelField label="Action Code">
            <code className="text-[12px] font-mono text-[#0C6EDB] bg-[#D6E8FA] px-2 py-0.5 rounded">
              {log.actionCode}
            </code>
          </PanelField>
          <PanelField label="Danh mục">
            <span className="text-[12px] text-[#0D1117]">{log.featureCategory || "—"}</span>
          </PanelField>
          <PanelField label="Thời gian xử lý">
            <span className={`text-[12px] font-mono tabular-nums font-semibold ${durationColor(log.durationMs)}`}>
              {log.durationMs.toLocaleString()} ms
            </span>
          </PanelField>
          <PanelField label="Timestamp">
            <span className="text-[12px] font-mono text-[#0D1117]">{log.timestamp}</span>
          </PanelField>
        </div>

        <hr className="border-[#E8EBF2]" />

        <div className="space-y-3">
          <PanelField label="API">
            <code className="text-[11px] font-mono text-[#3D4552] break-all">
              {log.requestMethod} {log.requestPath}
            </code>
          </PanelField>
          {log.entityType && (
            <PanelField label="Entity">
              <span className="text-[12px] font-mono text-[#0D1117]">
                {log.entityType}{log.entityId ? ` / ${log.entityId.slice(0, 8)}…` : ""}
              </span>
            </PanelField>
          )}
          {log.ipAddress && (
            <PanelField label="IP Address">
              <span className="text-[12px] font-mono text-[#0D1117]">{log.ipAddress}</span>
            </PanelField>
          )}
          {log.correlationId && (
            <PanelField label="Correlation ID">
              <span className="text-[11px] font-mono text-[#3D4552] break-all">{log.correlationId}</span>
            </PanelField>
          )}
        </div>

        {isFailure && log.correlationId && (
          <button
            onClick={() => onCrossRef(log.correlationId!)}
            className="w-full py-2.5 bg-[#FEE2E2] text-[#DC2626] border border-[#FCA5A5] rounded-md text-[12px] font-semibold hover:bg-[#FCA5A5]/30 transition-colors"
          >
            Xem Error Log tương ứng →
          </button>
        )}
      </div>
    </div>
  );
}

function ErrorDetailPanel({ log, detail, loading, onClose, onCrossRef }: {
  log: ErrorLogItem;
  detail: ErrorLogDetail | null;
  loading: boolean;
  onClose: () => void;
  onCrossRef: (correlationId: string) => void;
}) {
  return (
    <div className="flex flex-col h-full">
      <div className="flex items-start justify-between p-5 border-b border-[#D8DDE8] shrink-0">
        <div className="min-w-0 flex-1 mr-3">
          <div className="flex items-center gap-2 mb-1">
            <span className={`text-[10px] font-bold uppercase px-2 py-0.5 rounded border ${severityBadgeClass(log.severity)}`}>
              {log.severity}
            </span>
            <span className="text-[11px] text-[#6B7280]">{log.source}</span>
          </div>
          <p className="text-[13px] font-bold font-mono text-[#0D1117] truncate">{log.errorType}</p>
        </div>
        <button
          onClick={onClose}
          className="w-7 h-7 flex items-center justify-center rounded-md border border-[#D8DDE8] bg-[#EEF2F8] text-[#6B7280] hover:bg-[#D8DDE8] transition-colors shrink-0"
        >
          <span className="material-symbols-outlined text-[16px]">close</span>
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-5">
        {loading ? (
          <div className="flex items-center justify-center py-16 text-[#6B7280]">
            <span className="material-symbols-outlined animate-spin text-[24px] mr-2">progress_activity</span>
            Đang tải…
          </div>
        ) : detail ? (
          <div className="space-y-4">
            <div className="p-3 bg-[#FEE2E2] border border-[#FCA5A5] rounded-md">
              <p className="text-[12px] font-semibold text-[#DC2626] leading-relaxed">{detail.errorMessage}</p>
              <p className="text-[11px] font-mono text-[#DC2626]/70 mt-1">{detail.errorType}</p>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <PanelField label="Timestamp">
                <span className="text-[11px] font-mono text-[#0D1117]">{formatTimestamp(detail.timestamp)}</span>
              </PanelField>
              <PanelField label="Request">
                <code className="text-[10px] font-mono text-[#3D4552] break-all">
                  {detail.requestMethod} {detail.requestPath}
                </code>
              </PanelField>
              {detail.userName && (
                <PanelField label="Người dùng">
                  <span className="text-[12px] text-[#0D1117]">{detail.userName}</span>
                </PanelField>
              )}
              {detail.activeRole && (
                <PanelField label="Vai trò">
                  <span className="text-[12px] capitalize text-[#0D1117]">{detail.activeRole}</span>
                </PanelField>
              )}
            </div>

            {detail.correlationId && (
              <PanelField label="Correlation ID">
                <span className="text-[11px] font-mono text-[#3D4552] break-all">{detail.correlationId}</span>
              </PanelField>
            )}

            <hr className="border-[#E8EBF2]" />

            {detail.stackTrace && (
              <div>
                <p className="text-[10px] font-semibold uppercase tracking-wider text-[#6B7280] mb-2">Stack Trace</p>
                <pre className="bg-[#0D1117] text-[#E2E8F0] rounded-md p-3 text-[10px] font-mono leading-relaxed overflow-x-auto max-h-56 scrollbar-hide whitespace-pre-wrap break-all">
                  {detail.stackTrace}
                </pre>
              </div>
            )}

            {detail.innerExceptions.length > 0 && (
              <div>
                <p className="text-[10px] font-semibold uppercase tracking-wider text-[#6B7280] mb-2">
                  Inner Exceptions ({detail.innerExceptions.length})
                </p>
                <div className="space-y-2">
                  {detail.innerExceptions.map((ie, i) => (
                    <div key={i} className="border border-[#E8EBF2] rounded-md overflow-hidden">
                      <div className="px-3 py-2 bg-[#F5F7FB]">
                        <p className="text-[12px] font-medium text-[#DC2626]">{ie.message}</p>
                        <p className="text-[10px] font-mono text-[#6B7280]">{ie.type}</p>
                      </div>
                      {ie.stackTrace && (
                        <pre className="bg-[#0D1117] text-[#E2E8F0] px-3 py-2 text-[10px] font-mono leading-relaxed overflow-x-auto max-h-24 scrollbar-hide whitespace-pre-wrap break-all">
                          {ie.stackTrace}
                        </pre>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            {detail.correlationId && (
              <button
                onClick={() => onCrossRef(detail.correlationId!)}
                className="w-full py-2.5 bg-[#D6E8FA] text-[#0C6EDB] border border-[#93C5FD] rounded-md text-[12px] font-semibold hover:bg-[#93C5FD]/30 transition-colors"
              >
                Xem Activity Log tương ứng
              </button>
            )}
          </div>
        ) : (
          <p className="text-center py-8 text-[#6B7280] text-[13px]">Không thể tải chi tiết.</p>
        )}
      </div>
    </div>
  );
}

// ── Shared small components ───────────────────────────────────────────────────
function PanelField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <p className="text-[10px] font-semibold uppercase tracking-wider text-[#6B7280] mb-1">{label}</p>
      <div className="text-[12px] text-[#0D1117]">{children}</div>
    </div>
  );
}

function FeedLoader() {
  return (
    <div className="flex items-center justify-center py-20 text-[#6B7280]">
      <span className="material-symbols-outlined animate-spin text-[24px] mr-2">progress_activity</span>
      Đang tải…
    </div>
  );
}

function FeedEmpty({ message, icon }: { message: string; icon: string }) {
  return (
    <div className="flex flex-col items-center justify-center py-20 text-[#6B7280]">
      <span className="material-symbols-outlined text-[40px] mb-2 text-[#D8DDE8]">{icon}</span>
      {message}
    </div>
  );
}

function LogPagination({ page, totalPages, totalCount, label, onPageChange }: {
  page: number; totalPages: number; totalCount: number; label: string;
  onPageChange: (p: number) => void;
}) {
  return (
    <div className="flex items-center justify-between mt-2 pt-2 border-t border-[#E8EBF2] shrink-0">
      <span className="text-[12px] text-[#6B7280] tabular-nums">
        {totalCount.toLocaleString()} {label} · trang {page}/{totalPages}
      </span>
      <div className="flex gap-1">
        <button
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
          className="h-7 px-2 text-[12px] border border-[#D8DDE8] rounded bg-white text-[#3D4552] hover:bg-[#EEF2F8] disabled:opacity-40 transition-colors"
        >
          ‹
        </button>
        {getPageNumbers(page, totalPages).map((p, i) =>
          p === "..." ? (
            <span key={`dot-${i}`} className="h-7 px-1 flex items-center text-[#6B7280] text-[12px]">…</span>
          ) : (
            <button
              key={p}
              onClick={() => onPageChange(p as number)}
              className={`h-7 min-w-[28px] px-2 rounded text-[12px] tabular-nums transition-colors ${
                page === p
                  ? "bg-[#0C6EDB] text-white font-semibold"
                  : "border border-[#D8DDE8] bg-white text-[#3D4552] hover:bg-[#EEF2F8]"
              }`}
            >
              {p}
            </button>
          )
        )}
        <button
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
          className="h-7 px-2 text-[12px] border border-[#D8DDE8] rounded bg-white text-[#3D4552] hover:bg-[#EEF2F8] disabled:opacity-40 transition-colors"
        >
          ›
        </button>
      </div>
    </div>
  );
}

// ── Clear Confirm Modal ───────────────────────────────────────────────────────
function ClearConfirmModal({ tab, range, onRangeChange, clearing, onConfirm, onClose }: {
  tab: "activity" | "errors";
  range: number | null;
  onRangeChange: (v: number) => void;
  clearing: boolean;
  onConfirm: () => void;
  onClose: () => void;
}) {
  const title = tab === "activity" ? "Xóa nhật ký hoạt động" : "Xóa nhật ký lỗi";

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50"
      onClick={onClose}
    >
      <motion.div
        initial={{ scale: 0.96, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        exit={{ scale: 0.96, opacity: 0 }}
        transition={{ duration: 0.15 }}
        className="bg-white rounded-xl shadow-2xl w-full max-w-md"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-start gap-3 p-5 border-b border-[#E8EBF2]">
          <div className="w-10 h-10 rounded-full bg-[#FEE2E2] flex items-center justify-center shrink-0">
            <span className="material-symbols-outlined text-[#DC2626] text-[20px]">warning</span>
          </div>
          <div>
            <p className="text-[15px] font-bold text-[#0D1117]">{title}</p>
            <p className="text-[12px] text-[#6B7280] mt-0.5">
              Chọn khoảng thời gian để xóa. Thao tác này không thể hoàn tác.
            </p>
          </div>
          <button
            onClick={onClose}
            className="ml-auto w-7 h-7 flex items-center justify-center rounded-md border border-[#D8DDE8] bg-[#EEF2F8] text-[#6B7280] hover:bg-[#D8DDE8] transition-colors shrink-0"
          >
            <span className="material-symbols-outlined text-[16px]">close</span>
          </button>
        </div>

        {/* Range options */}
        <div className="p-5 space-y-2">
          {CLEAR_RANGES.map((r) => {
            const isAll = r.value === 0;
            const isSelected = range === r.value;
            return (
              <label
                key={r.value}
                className={`flex items-center gap-3 px-4 py-3 rounded-lg border cursor-pointer transition-all ${
                  isSelected
                    ? isAll
                      ? "border-[#DC2626] bg-[#FEE2E2]"
                      : "border-[#0C6EDB] bg-[#D6E8FA]"
                    : "border-[#E8EBF2] hover:border-[#D8DDE8] hover:bg-[#F5F7FB]"
                }`}
              >
                <input
                  type="radio"
                  name="clear-range"
                  value={r.value}
                  checked={isSelected}
                  onChange={() => onRangeChange(r.value)}
                  className="accent-[#0C6EDB]"
                />
                <span className={`text-[13px] font-medium ${
                  isSelected
                    ? isAll ? "text-[#DC2626]" : "text-[#0C6EDB]"
                    : "text-[#0D1117]"
                }`}>
                  {r.label}
                </span>
                {isAll && (
                  <span className="ml-auto text-[10px] font-bold uppercase px-2 py-0.5 rounded bg-[#FEE2E2] text-[#DC2626] border border-[#FCA5A5]">
                    Nguy hiểm
                  </span>
                )}
              </label>
            );
          })}
        </div>

        {/* Actions */}
        <div className="flex items-center justify-end gap-3 px-5 py-4 border-t border-[#E8EBF2] bg-[#F5F7FB] rounded-b-xl">
          <button
            onClick={onClose}
            disabled={clearing}
            className="px-4 py-2 text-[13px] font-medium border border-[#D8DDE8] rounded-lg text-[#3D4552] bg-white hover:bg-[#EEF2F8] transition-colors disabled:opacity-50"
          >
            Hủy bỏ
          </button>
          <button
            onClick={onConfirm}
            disabled={range === null || clearing}
            className="flex items-center gap-2 px-4 py-2 text-[13px] font-semibold rounded-lg bg-[#DC2626] text-white hover:bg-[#B91C1C] transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            {clearing ? (
              <span className="material-symbols-outlined text-[16px] animate-spin">progress_activity</span>
            ) : (
              <span className="material-symbols-outlined text-[16px]">delete_forever</span>
            )}
            {clearing ? "Đang xóa…" : "Xóa nhật ký"}
          </button>
        </div>
      </motion.div>
    </motion.div>
  );
}

// ── Utilities ─────────────────────────────────────────────────────────────────
function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString("vi-VN", {
    hour: "2-digit", minute: "2-digit", second: "2-digit",
  });
}

function formatTimestamp(iso: string): string {
  const d = new Date(iso);
  const diffMin = Math.floor((Date.now() - d.getTime()) / 60_000);
  if (diffMin < 1)    return "Vừa xong";
  if (diffMin < 60)   return `${diffMin} phút trước`;
  if (diffMin < 1440) return `${Math.floor(diffMin / 60)} giờ trước`;
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
  for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) pages.push(i);
  if (current < total - 2) pages.push("...");
  pages.push(total);
  return pages;
}
