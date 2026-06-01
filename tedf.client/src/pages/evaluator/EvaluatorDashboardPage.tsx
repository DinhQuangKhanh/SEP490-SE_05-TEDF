import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/contexts/AuthContext";
import { apiClient } from "@/lib/apiClient";
import { useSystemError } from "@/contexts/SystemErrorContext";

// ── DTOs ─────────────────────────────────────────────────────────────────────

interface EvaluatorStatsDto {
  totalAssigned: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
  needsModificationCount: number;
  reviewedCount: number;
  avgReviewDays: number | null;
}

interface PendingEvaluationDto {
  assignmentId: string;
  projectId: string;
  projectCode: string;
  projectNameVi: string;
  majorName: string;
  studentName: string;
  studentAvatar: string | null;
  assignedAt: string;
  daysElapsed: number;
  isUrgent: boolean;
}

interface RecentReviewedDto {
  projectId: string;
  projectNameVi: string;
  result: string;
  evaluatedAt: string;
}

interface EvaluatorDashboardDto {
  stats: EvaluatorStatsDto;
  pendingEvaluations: PendingEvaluationDto[];
  recentReviewed: RecentReviewedDto[];
}

// ── Animation variants ────────────────────────────────────────────────────────

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.07 } },
};

const fadeUp = {
  hidden: { opacity: 0, y: 18 },
  show: { opacity: 1, y: 0, transition: { duration: 0.35 } },
};

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

function relativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const days = Math.floor(diff / 86400000);
  if (days === 0) return "Hôm nay";
  if (days === 1) return "Hôm qua";
  return `${days} ngày trước`;
}

const RESULT_CONFIG: Record<string, { label: string; color: string; bar: string; badge: string }> = {
  Approved: {
    label: "Đã duyệt",
    color: "text-green-700",
    bar: "bg-green-500",
    badge: "bg-green-50 text-green-700 border-green-200",
  },
  NeedsModification: {
    label: "Cần chỉnh sửa",
    color: "text-amber-700",
    bar: "bg-amber-400",
    badge: "bg-amber-50 text-amber-700 border-amber-200",
  },
  Rejected: {
    label: "Từ chối",
    color: "text-red-700",
    bar: "bg-red-500",
    badge: "bg-red-50 text-red-700 border-red-200",
  },
};

function ResultBadge({ result }: { result: string }) {
  const cfg = RESULT_CONFIG[result];
  if (!cfg) return <span className="text-xs text-slate-500">{result}</span>;
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold border ${cfg.badge}`}>
      {cfg.label}
    </span>
  );
}

// ── Skeleton ──────────────────────────────────────────────────────────────────

function SkeletonBlock({ className }: { className?: string }) {
  return <div className={`bg-slate-100 rounded-xl animate-pulse ${className}`} />;
}

function DashboardSkeleton() {
  return (
    <div className="p-8 flex flex-col gap-8">
      <div className="flex flex-col gap-1.5">
        <SkeletonBlock className="h-8 w-56" />
        <SkeletonBlock className="h-4 w-72 mt-1" />
      </div>
      {/* Stat cards */}
      <div className="grid grid-cols-2 xl:grid-cols-4 gap-4">
        {[...Array(4)].map((_, i) => <SkeletonBlock key={i} className="h-28" />)}
      </div>
      {/* Row 2 */}
      <div className="grid grid-cols-1 xl:grid-cols-12 gap-6">
        <SkeletonBlock className="xl:col-span-8 h-80" />
        <SkeletonBlock className="xl:col-span-4 h-80" />
      </div>
      {/* Row 3 */}
      <div className="grid grid-cols-1 xl:grid-cols-12 gap-6">
        <SkeletonBlock className="xl:col-span-7 h-48" />
        <SkeletonBlock className="xl:col-span-5 h-48" />
      </div>
    </div>
  );
}

// ── Main Component ────────────────────────────────────────────────────────────

export function EvaluatorDashboardPage() {
  const { user } = useAuth();
  const { showError } = useSystemError();
  const navigate = useNavigate();
  const [data, setData] = useState<EvaluatorDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    apiClient
      .get<EvaluatorDashboardDto>("/api/evaluator/dashboard")
      .then(setData)
      .catch((err) => showError(err.message))
      .finally(() => setLoading(false));
  }, [showError]);

  if (loading) return <DashboardSkeleton />;

  const stats = data?.stats;
  const pending = data?.pendingEvaluations ?? [];
  const recentReviewed = data?.recentReviewed ?? [];

  const totalAssigned = stats?.totalAssigned ?? 0;
  const reviewedCount = stats?.reviewedCount ?? 0;
  const pendingCount = stats?.pendingCount ?? 0;
  const approvedCount = stats?.approvedCount ?? 0;
  const needsModCount = stats?.needsModificationCount ?? 0;
  const rejectedCount = stats?.rejectedCount ?? 0;
  const avgDays = stats?.avgReviewDays ?? null;

  const reviewedPct = totalAssigned > 0 ? Math.round((reviewedCount / totalAssigned) * 100) : 0;
  const urgentItems = pending.filter((p) => p.isUrgent);
  const firstPending = pending[0];

  // Result distribution (within reviewed only)
  const reviewedTotal = approvedCount + needsModCount + rejectedCount || 1;
  const resultBars = [
    { key: "Approved", label: "Đã duyệt", count: approvedCount, pct: Math.round((approvedCount / reviewedTotal) * 100) },
    { key: "NeedsModification", label: "Cần chỉnh sửa", count: needsModCount, pct: Math.round((needsModCount / reviewedTotal) * 100) },
    { key: "Rejected", label: "Từ chối", count: rejectedCount, pct: Math.round((rejectedCount / reviewedTotal) * 100) },
  ];

  return (
    <div className="p-8 flex flex-col gap-8 min-h-0">
      {/* Header */}
      <motion.header
        initial={{ opacity: 0, y: -16 }}
        animate={{ opacity: 1, y: 0 }}
        className="flex flex-col md:flex-row md:items-end justify-between gap-4"
      >
        <div>
          <h2 className="text-slate-900 text-2xl font-bold tracking-tight">
            Xin chào, {user?.name ?? "Giảng viên"}
          </h2>
          <p className="text-slate-500 text-sm mt-0.5">
            Bạn có{" "}
            <span className="text-primary font-bold">{pendingCount} đề tài</span>{" "}
            đang chờ thẩm định
            {urgentItems.length > 0 && (
              <span className="ml-1.5 inline-flex items-center gap-1 text-red-600 font-bold">
                <span className="size-1.5 rounded-full bg-red-500 animate-pulse inline-block" />
                {urgentItems.length} khẩn cấp
              </span>
            )}
          </p>
        </div>
        <button
          onClick={() =>
            firstPending
              ? navigate(`/evaluator/review/${firstPending.projectId}`)
              : navigate("/evaluator/projects")
          }
          className="flex items-center justify-center gap-2 h-10 px-5 rounded-lg bg-primary text-white text-sm font-semibold hover:bg-primary-dark transition-colors shadow-lg shadow-primary/20"
        >
          <span className="material-symbols-outlined text-[20px]">play_circle</span>
          <span>Bắt đầu thẩm định</span>
        </button>
      </motion.header>

      {/* ── ROW 1: Stat Cards ─────────────────────────────────────────────── */}
      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="grid grid-cols-2 xl:grid-cols-4 gap-4"
      >
        {/* Card 1 — Tổng giao */}
        <motion.div variants={fadeUp} className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">Tổng giao</span>
            <div className="p-1.5 rounded-lg bg-primary/10">
              <span className="material-symbols-outlined text-primary text-[20px]">assignment</span>
            </div>
          </div>
          <div className="flex items-end gap-2">
            <span className="text-3xl font-bold text-slate-900 leading-none">{totalAssigned}</span>
            <span className="text-xs text-slate-400 font-medium mb-0.5">đề tài</span>
          </div>
          <p className="text-xs text-slate-400 font-medium">học kỳ hiện tại</p>
        </motion.div>

        {/* Card 2 — Đang chờ */}
        <motion.div variants={fadeUp} className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">Đang chờ</span>
            <div className="p-1.5 rounded-lg bg-amber-50">
              <span className="material-symbols-outlined text-amber-600 text-[20px]">pending_actions</span>
            </div>
          </div>
          <div className="flex items-end gap-2">
            <span className="text-3xl font-bold text-amber-600 leading-none">{pendingCount}</span>
            <span className="text-xs text-slate-400 font-medium mb-0.5">đề tài</span>
          </div>
          {urgentItems.length > 0 ? (
            <p className="text-xs text-red-600 font-bold flex items-center gap-1">
              <span className="size-1.5 rounded-full bg-red-500 animate-pulse inline-block" />
              {urgentItems.length} khẩn cấp
            </p>
          ) : (
            <p className="text-xs text-slate-400 font-medium">không có đề tài khẩn cấp</p>
          )}
        </motion.div>

        {/* Card 3 — Đã thẩm định */}
        <motion.div variants={fadeUp} className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">Đã thẩm định</span>
            <div className="p-1.5 rounded-lg bg-green-50">
              <span className="material-symbols-outlined text-green-600 text-[20px]">verified</span>
            </div>
          </div>
          <div className="flex items-end gap-2">
            <span className="text-3xl font-bold text-green-600 leading-none">{reviewedCount}</span>
            <span className="text-xs text-slate-400 font-medium mb-0.5">/ {totalAssigned}</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="flex-1 h-1.5 bg-gray-100 rounded-full overflow-hidden">
              <div className="h-full bg-green-500 rounded-full" style={{ width: `${reviewedPct}%` }} />
            </div>
            <span className="text-xs font-bold text-green-600">{reviewedPct}%</span>
          </div>
        </motion.div>

        {/* Card 4 — Thời gian TB */}
        <motion.div variants={fadeUp} className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">Thời gian TB</span>
            <div className="p-1.5 rounded-lg bg-slate-100">
              <span className="material-symbols-outlined text-slate-600 text-[20px]">schedule</span>
            </div>
          </div>
          <div className="flex items-end gap-2">
            <span className="text-3xl font-bold text-slate-900 leading-none">
              {avgDays != null ? avgDays : "—"}
            </span>
            {avgDays != null && <span className="text-xs text-slate-400 font-medium mb-0.5">ngày</span>}
          </div>
          {avgDays != null ? (
            <p className={`text-xs font-bold ${avgDays <= 7 ? "text-green-600" : "text-amber-600"}`}>
              {avgDays <= 7 ? "Đúng hạn" : "Trên 7 ngày chuẩn"}
            </p>
          ) : (
            <p className="text-xs text-slate-400 font-medium">chưa có dữ liệu</p>
          )}
        </motion.div>
      </motion.div>

      {/* ── ROW 2: Pending Table + Performance ───────────────────────────── */}
      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="grid grid-cols-1 xl:grid-cols-12 gap-6"
      >
        {/* Pending Table */}
        <motion.div
          variants={fadeUp}
          className="xl:col-span-8 bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden flex flex-col"
        >
          <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="p-1.5 rounded-md bg-primary/10 text-primary">
                <span className="material-symbols-outlined text-xl">pending_actions</span>
              </div>
              <h3 className="text-slate-900 text-base font-bold">Cần xử lý ngay</h3>
              {pendingCount > 0 && (
                <span className="text-xs font-bold text-white bg-primary px-2 py-0.5 rounded-full">
                  {pendingCount}
                </span>
              )}
            </div>
            <div className="flex items-center gap-3">
              <span className="text-[11px] text-slate-400 font-medium">Top 5 khẩn cấp nhất</span>
              <Link
                to="/evaluator/projects"
                className="text-primary text-xs font-bold hover:text-primary-dark transition-colors"
              >
                Xem tất cả
              </Link>
            </div>
          </div>

          {/* Urgent banner */}
          {urgentItems.length > 0 && (
            <div className="px-6 py-2.5 bg-red-50 border-b border-red-100 flex items-center gap-2 text-red-700 text-xs font-semibold">
              <span className="material-symbols-outlined text-base">warning</span>
              Có {urgentItems.length} đề tài khẩn cấp cần xử lý ngay
            </div>
          )}

          {pending.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 text-slate-400 gap-2">
              <span className="material-symbols-outlined text-4xl">check_circle</span>
              <p className="text-sm font-medium">Không có đề tài nào đang chờ thẩm định</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-gray-50/80 border-b border-gray-100">
                    <th className="px-6 py-3 text-[11px] font-bold text-slate-500 uppercase tracking-wider">Đề tài</th>
                    <th className="px-6 py-3 text-[11px] font-bold text-slate-500 uppercase tracking-wider hidden sm:table-cell">Sinh viên</th>
                    <th className="px-6 py-3 text-[11px] font-bold text-slate-500 uppercase tracking-wider hidden md:table-cell">Chuyên ngành</th>
                    <th className="px-6 py-3 text-[11px] font-bold text-slate-500 uppercase tracking-wider hidden lg:table-cell">Ngày giao</th>
                    <th className="px-6 py-3 text-[11px] font-bold text-slate-500 uppercase tracking-wider">Trạng thái</th>
                    <th className="px-6 py-3 text-[11px] font-bold text-slate-500 uppercase tracking-wider text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {pending.map((project) => (
                    <motion.tr
                      key={project.assignmentId}
                      whileHover={{ backgroundColor: "rgb(249 250 251)" }}
                      className={`transition-colors ${project.isUrgent ? "bg-red-50/30" : ""}`}
                    >
                      <td className="px-6 py-3.5">
                        <div className="flex flex-col">
                          <span className="text-slate-900 font-semibold text-sm line-clamp-1">{project.projectNameVi}</span>
                          <span className="text-[11px] font-mono text-slate-400 mt-0.5">{project.projectCode}</span>
                        </div>
                      </td>
                      <td className="px-6 py-3.5 hidden sm:table-cell">
                        <div className="flex items-center gap-2.5">
                          {project.studentAvatar ? (
                            <div
                              className="size-7 rounded-full bg-gray-200 bg-cover ring-1 ring-gray-100 shrink-0"
                              style={{ backgroundImage: `url('${project.studentAvatar}')` }}
                            />
                          ) : (
                            <div className="size-7 rounded-full bg-primary/10 text-primary flex items-center justify-center text-xs font-bold ring-1 ring-gray-100 shrink-0">
                              {project.studentName.charAt(0)}
                            </div>
                          )}
                          <span className="text-slate-900 font-medium text-sm">{project.studentName || "—"}</span>
                        </div>
                      </td>
                      <td className="px-6 py-3.5 hidden md:table-cell">
                        <span className="inline-flex items-center px-2 py-0.5 rounded-md text-xs font-medium bg-gray-100 text-gray-700 border border-gray-200">
                          {project.majorName || "—"}
                        </span>
                      </td>
                      <td className="px-6 py-3.5 hidden lg:table-cell">
                        <span className="text-slate-500 text-sm">{formatDate(project.assignedAt)}</span>
                      </td>
                      <td className="px-6 py-3.5">
                        <div className="flex flex-col gap-1">
                          {project.isUrgent ? (
                            <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold border bg-red-50 text-red-600 border-red-100 w-fit">
                              <span className="size-1.5 rounded-full bg-red-500 animate-pulse" />
                              Khẩn cấp
                            </span>
                          ) : (
                            <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold border bg-gray-100 text-gray-600 border-gray-200 w-fit">
                              Đang chờ
                            </span>
                          )}
                          <span className={`text-[11px] font-medium ${project.daysElapsed > 5 ? "text-red-500" : "text-slate-400"}`}>
                            {project.daysElapsed === 0 ? "Hôm nay" : `${project.daysElapsed} ngày`}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-3.5 text-right">
                        <Link
                          to={`/evaluator/review/${project.projectId}`}
                          className={`inline-flex items-center justify-center h-7 px-3 text-xs font-bold rounded-lg transition-all ${
                            project.isUrgent
                              ? "bg-primary text-white hover:bg-primary-dark shadow-sm shadow-primary/20"
                              : "bg-white border border-gray-200 text-slate-700 hover:border-primary/50 hover:text-primary"
                          }`}
                        >
                          Thẩm định
                        </Link>
                      </td>
                    </motion.tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </motion.div>

        {/* Right column: Donut + Result Distribution */}
        <motion.div variants={fadeUp} className="xl:col-span-4 flex flex-col gap-5">
          {/* Donut */}
          <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 flex flex-col gap-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <div className="p-1.5 rounded-md bg-green-50 text-green-700">
                  <span className="material-symbols-outlined text-xl">monitoring</span>
                </div>
                <h3 className="text-slate-900 text-base font-bold">Tiến độ</h3>
              </div>
              <span className="text-xs font-bold text-slate-400 bg-gray-100 px-2 py-0.5 rounded-md">Học kỳ này</span>
            </div>

            <div className="flex items-center gap-5">
              {/* Donut SVG */}
              <div className="relative size-28 shrink-0">
                <svg className="size-full -rotate-90" viewBox="0 0 36 36">
                  <path
                    d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"
                    fill="none" stroke="#f1f5f9" strokeWidth="3.5"
                  />
                  <path
                    d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"
                    fill="none" stroke="var(--color-primary, #5564a0)" strokeWidth="3.5"
                    strokeDasharray={`${reviewedPct}, 100`} strokeLinecap="round"
                  />
                </svg>
                <div className="absolute inset-0 flex flex-col items-center justify-center text-center">
                  <span className="text-2xl font-bold text-slate-900">{reviewedPct}%</span>
                  <span className="text-[10px] text-slate-400 font-medium">hoàn thành</span>
                </div>
              </div>

              {/* Legend */}
              <div className="flex flex-col gap-2.5 flex-1">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1.5">
                    <div className="size-2 rounded-full bg-primary" />
                    <span className="text-xs text-slate-500">Đã thẩm định</span>
                  </div>
                  <span className="text-xs font-bold text-slate-900">{reviewedCount}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1.5">
                    <div className="size-2 rounded-full bg-slate-200" />
                    <span className="text-xs text-slate-500">Đang chờ</span>
                  </div>
                  <span className="text-xs font-bold text-slate-900">{pendingCount}</span>
                </div>
                <div className="pt-2 border-t border-gray-100 flex items-center justify-between">
                  <span className="text-[11px] text-slate-400">Thời gian TB</span>
                  <span className={`text-[11px] font-bold px-1.5 py-0.5 rounded-full ${
                    avgDays != null && avgDays <= 7
                      ? "text-green-700 bg-green-50"
                      : avgDays != null
                      ? "text-amber-700 bg-amber-50"
                      : "text-slate-500 bg-gray-100"
                  }`}>
                    {avgDays != null ? `${avgDays} ngày` : "—"}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Result Distribution */}
          <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 flex flex-col gap-4">
            <div className="flex items-center gap-2">
              <div className="p-1.5 rounded-md bg-slate-100 text-slate-600">
                <span className="material-symbols-outlined text-xl">bar_chart</span>
              </div>
              <h3 className="text-slate-900 text-base font-bold">Phân bổ kết quả</h3>
            </div>

            <div className="flex flex-col gap-3">
              {resultBars.map(({ key, label, count, pct }) => {
                const cfg = RESULT_CONFIG[key];
                return (
                  <div key={key} className="flex flex-col gap-1.5">
                    <div className="flex items-center justify-between text-xs">
                      <span className={`font-semibold ${cfg.color}`}>{label}</span>
                      <span className="font-bold text-slate-900">{count}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
                        <motion.div
                          initial={{ width: 0 }}
                          animate={{ width: `${pct}%` }}
                          transition={{ duration: 0.6, delay: 0.2 }}
                          className={`h-full rounded-full ${cfg.bar}`}
                        />
                      </div>
                      <span className="text-[11px] font-bold text-slate-500 w-7 text-right">{pct}%</span>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </motion.div>
      </motion.div>

      {/* ── ROW 3: Activity Feed + Quick Actions ──────────────────────────── */}
      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="grid grid-cols-1 xl:grid-cols-12 gap-6 pb-2"
      >
        {/* Activity Feed */}
        <motion.div
          variants={fadeUp}
          className="xl:col-span-7 bg-white rounded-2xl border border-gray-200 shadow-sm p-5 flex flex-col gap-4"
        >
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="p-1.5 rounded-md bg-amber-50 text-amber-700">
                <span className="material-symbols-outlined text-xl">history</span>
              </div>
              <h3 className="text-slate-900 text-base font-bold">Thẩm định gần đây</h3>
            </div>
            <Link
              to="/evaluator/history"
              className="text-primary text-xs font-bold hover:text-primary-dark transition-colors"
            >
              Xem tất cả
            </Link>
          </div>

          {recentReviewed.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-10 text-slate-400 gap-2">
              <span className="material-symbols-outlined text-3xl">inbox</span>
              <p className="text-sm font-medium">Chưa có thẩm định nào</p>
            </div>
          ) : (
            <div className="flex flex-col gap-2">
              {recentReviewed.map((r) => {
                const cfg = RESULT_CONFIG[r.result];
                return (
                  <motion.div
                    key={`${r.projectId}-${r.evaluatedAt}`}
                    whileHover={{ x: 3 }}
                    className="group flex items-center gap-4 p-3 rounded-xl hover:bg-gray-50 cursor-pointer transition-all border border-transparent hover:border-gray-100"
                  >
                    {/* Color bar */}
                    <div className={`w-1 h-10 rounded-full shrink-0 ${cfg?.bar ?? "bg-slate-300"}`} />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-semibold text-slate-900 line-clamp-1">{r.projectNameVi}</p>
                      <p className="text-xs text-slate-400 mt-0.5">{relativeTime(r.evaluatedAt)}</p>
                    </div>
                    <ResultBadge result={r.result} />
                  </motion.div>
                );
              })}
            </div>
          )}
        </motion.div>

        {/* Quick Actions */}
        <motion.div
          variants={fadeUp}
          className="xl:col-span-5 bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden"
        >
          <div className="px-5 py-4 border-b border-gray-100 flex items-center gap-2">
            <div className="p-1.5 rounded-md bg-primary/10 text-primary">
              <span className="material-symbols-outlined text-xl">bolt</span>
            </div>
            <h3 className="text-slate-900 text-base font-bold">Thao tác nhanh</h3>
          </div>
          <div className="divide-y divide-gray-100">
            <button
              onClick={() =>
                firstPending
                  ? navigate(`/evaluator/review/${firstPending.projectId}`)
                  : navigate("/evaluator/projects")
              }
              className="w-full flex items-center justify-between px-5 py-4 hover:bg-primary/5 group transition-colors text-left"
            >
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-primary/10 text-primary">
                  <span className="material-symbols-outlined text-[18px]">rate_review</span>
                </div>
                <div>
                  <p className="text-sm font-semibold text-slate-900">Bắt đầu thẩm định</p>
                  <p className="text-xs text-slate-400">
                    {firstPending ? `Tiếp theo: ${firstPending.projectCode}` : "Không có đề tài chờ"}
                  </p>
                </div>
              </div>
              <span className="material-symbols-outlined text-slate-300 group-hover:text-primary transition-colors text-xl">
                chevron_right
              </span>
            </button>

            <Link
              to="/evaluator/projects"
              className="w-full flex items-center justify-between px-5 py-4 hover:bg-gray-50 group transition-colors"
            >
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-slate-100 text-slate-600">
                  <span className="material-symbols-outlined text-[18px]">folder_open</span>
                </div>
                <div>
                  <p className="text-sm font-semibold text-slate-900">Tất cả đề tài được giao</p>
                  <p className="text-xs text-slate-400">{totalAssigned} đề tài trong học kỳ</p>
                </div>
              </div>
              <span className="material-symbols-outlined text-slate-300 group-hover:text-primary transition-colors text-xl">
                chevron_right
              </span>
            </Link>

            <Link
              to="/evaluator/history"
              className="w-full flex items-center justify-between px-5 py-4 hover:bg-gray-50 group transition-colors"
            >
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-slate-100 text-slate-600">
                  <span className="material-symbols-outlined text-[18px]">manage_history</span>
                </div>
                <div>
                  <p className="text-sm font-semibold text-slate-900">Lịch sử thẩm định</p>
                  <p className="text-xs text-slate-400">{reviewedCount} đề tài đã hoàn thành</p>
                </div>
              </div>
              <span className="material-symbols-outlined text-slate-300 group-hover:text-primary transition-colors text-xl">
                chevron_right
              </span>
            </Link>
          </div>
        </motion.div>
      </motion.div>
    </div>
  );
}
