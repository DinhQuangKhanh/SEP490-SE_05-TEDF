import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { Link, useNavigate } from "react-router-dom";
import { Header } from "@/components/layout";
import { SemesterTimeline } from "@/components/shared/SemesterTimeline";
import { useAuth } from "@/contexts/AuthContext";
import { useSystemError } from "@/contexts/SystemErrorContext";
import {
  departmentHeadService,
  type DepartmentHeadDashboardData,
  type RecentActivity,
} from "@/lib/departmentHeadService";

// ── Helpers ──────────────────────────────────────────────────────────────────

function relativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return "Vừa xong";
  if (mins < 60) return `${mins} phút trước`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs} giờ trước`;
  const days = Math.floor(hrs / 24);
  if (days < 7) return `${days} ngày trước`;
  return new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

function activityIcon(type: string): { icon: string; color: string; bg: string } {
  switch (type) {
    case "submitted":
      return { icon: "rate_review", color: "text-emerald-600", bg: "bg-emerald-50" };
    case "assigned":
      return { icon: "person_add", color: "text-blue-600", bg: "bg-blue-50" };
    case "decided":
      return { icon: "gavel", color: "text-amber-600", bg: "bg-amber-50" };
    default:
      return { icon: "info", color: "text-slate-600", bg: "bg-slate-50" };
  }
}

function activityLabel(a: RecentActivity): string {
  switch (a.activityType) {
    case "submitted":
      return `${a.actorName} đã nộp kết quả thẩm định`;
    case "assigned":
      return `${a.actorName} được phân công thẩm định`;
    case "decided":
      return `Đã đưa ra quyết định cuối cùng`;
    default:
      return a.activityType;
  }
}

// ── Animation ────────────────────────────────────────────────────────────────

const container = { hidden: {}, show: { transition: { staggerChildren: 0.06 } } };
const item = { hidden: { opacity: 0, y: 16 }, show: { opacity: 1, y: 0, transition: { type: "spring", damping: 20 } } };

// ── Component ────────────────────────────────────────────────────────────────

export function DepartmentHeadDashboardPage() {
  const [data, setData] = useState<DepartmentHeadDashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const { user } = useAuth();
  const { showError } = useSystemError();
  const navigate = useNavigate();

  useEffect(() => {
    departmentHeadService
      .getDashboard()
      .then(setData)
      .catch((err) => showError(err instanceof Error ? err.message : "Không thể tải dữ liệu dashboard."))
      .finally(() => setLoading(false));
  }, []);

  const headName = data?.headName || user?.name || "Chủ nhiệm bộ môn";
  const stats = data?.stats;
  const semester = data?.semesterProgress;
  const evalProgress = data?.evaluationProgress;

  const statCards = [
    { label: "Đề tài bộ môn", value: stats?.totalProjects ?? 0, icon: "topic", gradient: "from-blue-500 to-blue-600" },
    { label: "Chờ phân công", value: stats?.pendingAssignment ?? 0, icon: "pending_actions", gradient: "from-amber-500 to-orange-500" },
    { label: "Đang thẩm định", value: stats?.inEvaluation ?? 0, icon: "rate_review", gradient: "from-indigo-500 to-indigo-600" },
    { label: "Cần quyết định", value: stats?.needsFinalDecision ?? 0, icon: "gavel", gradient: "from-rose-500 to-rose-600" },
    { label: "Hoàn thành", value: stats?.completed ?? 0, icon: "check_circle", gradient: "from-emerald-500 to-emerald-600" },
  ];

  // Donut chart
  const totalEval = (evalProgress?.approved ?? 0) + (evalProgress?.rejected ?? 0) + (evalProgress?.needsModification ?? 0) + (evalProgress?.pending ?? 0);
  const segments = totalEval > 0
    ? [
        { label: "Đã duyệt", value: evalProgress?.approved ?? 0, color: "#10b981" },
        { label: "Từ chối", value: evalProgress?.rejected ?? 0, color: "#f43f5e" },
        { label: "Cần chỉnh sửa", value: evalProgress?.needsModification ?? 0, color: "#f59e0b" },
        { label: "Đang chờ", value: evalProgress?.pending ?? 0, color: "#94a3b8" },
      ]
    : [];

  const donutGradient = (() => {
    if (totalEval === 0) return "conic-gradient(#e2e8f0 0deg 360deg)";
    let cumulative = 0;
    const stops = segments.map((s) => {
      const start = cumulative;
      cumulative += (s.value / totalEval) * 360;
      return `${s.color} ${start}deg ${cumulative}deg`;
    });
    return `conic-gradient(${stops.join(", ")})`;
  })();

  return (
    <div className="flex flex-col h-full">
      <Header
        title="Tổng quan"
        subtitle={data?.departmentName ? `Bộ môn ${data.departmentName}` : "Quản lý và phân công thẩm định đề tài"}
        role="department-head"
        showSearch={false}
      />

      <div className="flex-1 overflow-y-auto">
        <motion.div
          variants={container}
          initial="hidden"
          animate="show"
          className="px-8 py-6 space-y-6"
        >
          {/* Welcome */}
          <motion.div variants={item} className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-extrabold text-slate-900">
                Xin chào, <span className="text-primary">{headName}</span>
              </h1>
              <p className="text-slate-500 mt-1">
                {semester ? `Học kỳ ${semester.semesterName}` : "Quản lý và phân công thẩm định đề tài trong bộ môn"}
                {stats ? ` · ${stats.totalMentors} giảng viên · ${stats.totalEvaluators} thẩm định viên` : ""}
              </p>
            </div>
          </motion.div>

          {/* Stats Cards */}
          {loading ? (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="bg-white p-5 rounded-xl border border-slate-100 animate-pulse h-32">
                  <div className="h-4 bg-slate-200 rounded w-20 mb-3" />
                  <div className="h-8 bg-slate-200 rounded w-12" />
                </div>
              ))}
            </div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
              {statCards.map((stat, idx) => (
                <motion.div
                  key={stat.label}
                  variants={item}
                  whileHover={{ y: -2, boxShadow: "0 8px 25px -5px rgba(0,0,0,0.1)" }}
                  className="bg-white p-5 rounded-xl border border-slate-100 shadow-sm flex items-start justify-between h-32 relative overflow-hidden group cursor-pointer"
                  onClick={() => idx > 0 ? navigate("/department-head/assign") : undefined}
                >
                  <div className="z-10">
                    <p className="text-slate-500 text-sm font-medium">{stat.label}</p>
                    <h3 className="text-3xl font-bold text-slate-800 mt-2">{String(stat.value).padStart(2, "0")}</h3>
                  </div>
                  <div className={`bg-gradient-to-br ${stat.gradient} text-white p-2.5 rounded-xl shadow-lg`}>
                    <span className="material-symbols-outlined text-[22px]">{stat.icon}</span>
                  </div>
                  <div className="absolute -right-6 -bottom-6 bg-gradient-to-br from-blue-50 to-transparent size-28 rounded-full opacity-40 group-hover:scale-125 transition-transform duration-300" />
                </motion.div>
              ))}
            </div>
          )}

          {/* Semester Timeline + Quick Actions — same row */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
            {/* Semester Timeline */}
            {semester && semester.phases.length > 0 ? (
              <motion.div variants={item} className="lg:col-span-2 bg-white rounded-xl border border-slate-100 shadow-sm p-6">
                <div className="flex items-center gap-2 mb-2">
                  <span className="material-symbols-outlined text-primary">timeline</span>
                  <h3 className="text-slate-800 font-bold text-lg">Tiến trình học kỳ</h3>
                  <span className="text-xs text-slate-400 ml-auto">{semester.semesterName}</span>
                </div>
                <SemesterTimeline phases={semester.phases} />
              </motion.div>
            ) : (
              <div className="lg:col-span-2" />
            )}

            {/* Quick Actions — stacked in 1 column */}
            <motion.div variants={item} className="space-y-3">
              {[
                {
                  title: "Phân công thẩm định",
                  desc: "Phân công evaluator cho đề tài",
                  icon: "assignment_ind",
                  gradient: "from-blue-500 to-blue-600",
                  path: "/department-head/assign",
                },
                {
                  title: "Kho đề tài bộ môn",
                  desc: "Xem tất cả đề tài trong bộ môn",
                  icon: "topic",
                  gradient: "from-indigo-500 to-indigo-600",
                  path: "/department-head/assign",
                },
                {
                  title: "Cài đặt",
                  desc: "Quản lý cài đặt bộ môn",
                  icon: "settings",
                  gradient: "from-slate-500 to-slate-600",
                  path: "/department-head/settings",
                },
              ].map((card) => (
                <motion.div
                  key={card.title}
                  whileHover={{ y: -2, boxShadow: "0 8px 25px -5px rgba(0,0,0,0.1)" }}
                  onClick={() => navigate(card.path)}
                  className="bg-white rounded-xl border border-slate-100 shadow-sm p-4 cursor-pointer group flex items-center gap-4 hover:border-primary/30 transition-all"
                >
                  <div className={`bg-gradient-to-br ${card.gradient} text-white p-2.5 rounded-xl shadow-md group-hover:scale-105 transition-transform`}>
                    <span className="material-symbols-outlined text-[22px]">{card.icon}</span>
                  </div>
                  <div>
                    <p className="font-bold text-slate-800 text-sm group-hover:text-primary transition-colors">{card.title}</p>
                    <p className="text-xs text-slate-500">{card.desc}</p>
                  </div>
                </motion.div>
              ))}
            </motion.div>
          </div>

          {/* Middle row: Donut chart + Quick alerts */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Donut */}
            <motion.div variants={item} className="bg-white rounded-xl border border-slate-100 shadow-sm p-6">
              <h3 className="text-slate-800 font-bold text-lg mb-5 flex items-center gap-2">
                <span className="material-symbols-outlined text-primary">pie_chart</span>
                Tổng quan thẩm định
              </h3>
              <div className="flex items-center gap-6">
                <div
                  className="size-36 rounded-full relative shrink-0"
                  style={{ background: donutGradient }}
                >
                  <div className="absolute inset-3 bg-white rounded-full flex flex-col items-center justify-center">
                    <span className="text-2xl font-bold text-slate-800">{totalEval}</span>
                    <span className="text-[10px] text-slate-500">đề tài</span>
                  </div>
                </div>
                <div className="space-y-2.5 flex-1">
                  {segments.map((s) => (
                    <div key={s.label} className="flex items-center gap-2 text-sm">
                      <div className="size-3 rounded-full shrink-0" style={{ backgroundColor: s.color }} />
                      <span className="text-slate-600 flex-1">{s.label}</span>
                      <span className="font-bold text-slate-800">{s.value}</span>
                    </div>
                  ))}
                </div>
              </div>
            </motion.div>

            {/* Quick alerts */}
            <motion.div variants={item} className="lg:col-span-2 space-y-4">
              <h3 className="text-slate-800 font-bold text-lg flex items-center gap-2">
                <span className="material-symbols-outlined text-primary">notifications_active</span>
                Cần xử lý
              </h3>

              {(stats?.needsFinalDecision ?? 0) > 0 && (
                <div className="p-4 bg-amber-50 border border-amber-200 rounded-xl flex items-center gap-4">
                  <div className="size-10 bg-amber-100 rounded-xl flex items-center justify-center shrink-0">
                    <span className="material-symbols-outlined text-amber-600">warning</span>
                  </div>
                  <div className="flex-1">
                    <p className="font-semibold text-amber-800">
                      {stats?.needsFinalDecision} đề tài cần quyết định cuối cùng
                    </p>
                    <p className="text-sm text-amber-600">Kết quả thẩm định của 2 evaluator không thống nhất</p>
                  </div>
                  <Link
                    to="/department-head/assign"
                    className="flex items-center gap-2 px-4 py-2.5 bg-amber-500 text-white text-sm font-medium rounded-lg hover:bg-amber-600 transition-colors shrink-0"
                  >
                    <span className="material-symbols-outlined text-[18px]">gavel</span>
                    Xem ngay
                  </Link>
                </div>
              )}

              {(stats?.pendingAssignment ?? 0) > 0 && (
                <div className="p-4 bg-blue-50 border border-blue-200 rounded-xl flex items-center gap-4">
                  <div className="size-10 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
                    <span className="material-symbols-outlined text-blue-600">assignment_ind</span>
                  </div>
                  <div className="flex-1">
                    <p className="font-semibold text-blue-800">
                      {stats?.pendingAssignment} đề tài chờ phân công evaluator
                    </p>
                    <p className="text-sm text-blue-600">Vui lòng phân công 2 evaluator cho mỗi đề tài</p>
                  </div>
                  <Link
                    to="/department-head/assign"
                    className="flex items-center gap-2 px-4 py-2.5 bg-primary text-white text-sm font-medium rounded-lg hover:bg-primary/90 transition-colors shrink-0"
                  >
                    <span className="material-symbols-outlined text-[18px]">person_add</span>
                    Phân công
                  </Link>
                </div>
              )}

              {(stats?.needsFinalDecision ?? 0) === 0 && (stats?.pendingAssignment ?? 0) === 0 && !loading && (
                <div className="p-6 bg-emerald-50 border border-emerald-200 rounded-xl flex items-center gap-4">
                  <div className="size-10 bg-emerald-100 rounded-xl flex items-center justify-center shrink-0">
                    <span className="material-symbols-outlined text-emerald-600">check_circle</span>
                  </div>
                  <div>
                    <p className="font-semibold text-emerald-800">Tất cả đã được xử lý</p>
                    <p className="text-sm text-emerald-600">Không có đề tài nào cần phân công hoặc quyết định</p>
                  </div>
                </div>
              )}
            </motion.div>
          </div>

          {/* Recent Activities */}
          <motion.div variants={item} className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="px-6 py-5 border-b border-slate-100 flex items-center justify-between">
              <h3 className="text-slate-800 font-bold text-lg flex items-center gap-2">
                <span className="material-symbols-outlined text-primary">history</span>
                Hoạt động gần đây
              </h3>
              <button
                onClick={() => navigate("/department-head/assign")}
                className="text-primary text-sm font-medium hover:underline flex items-center gap-1"
              >
                Xem tất cả <span className="material-symbols-outlined text-[16px]">arrow_forward</span>
              </button>
            </div>
            {loading ? (
              <div className="p-6 space-y-4">
                {Array.from({ length: 4 }).map((_, i) => (
                  <div key={i} className="flex items-center gap-4 animate-pulse">
                    <div className="size-10 bg-slate-200 rounded-xl" />
                    <div className="flex-1">
                      <div className="h-4 bg-slate-200 rounded w-60 mb-2" />
                      <div className="h-3 bg-slate-200 rounded w-32" />
                    </div>
                    <div className="h-4 bg-slate-200 rounded w-20" />
                  </div>
                ))}
              </div>
            ) : data && data.recentActivities.length > 0 ? (
              <div className="divide-y divide-slate-50">
                {data.recentActivities.map((act, idx) => {
                  const ai = activityIcon(act.activityType);
                  return (
                    <div key={idx} className="px-6 py-4 flex items-center gap-4 hover:bg-slate-50/50 transition-colors">
                      <div className={`size-10 ${ai.bg} rounded-xl flex items-center justify-center shrink-0`}>
                        <span className={`material-symbols-outlined text-[20px] ${ai.color}`}>{ai.icon}</span>
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-slate-800 truncate">{activityLabel(act)}</p>
                        <p className="text-xs text-slate-500 truncate">{act.projectCode} · {act.projectName}</p>
                      </div>
                      <span className="text-xs text-slate-400 shrink-0">{relativeTime(act.occurredAt)}</span>
                    </div>
                  );
                })}
              </div>
            ) : (
              <div className="py-12 text-center">
                <span className="material-symbols-outlined text-slate-300 text-[48px]">inbox</span>
                <p className="text-slate-500 mt-2">Chưa có hoạt động nào</p>
              </div>
            )}
          </motion.div>

        </motion.div>
      </div>
    </div>
  );
}
