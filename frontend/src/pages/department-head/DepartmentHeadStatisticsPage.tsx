import { useState, useEffect, useMemo } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Header } from "@/components/layout";
import { SemesterTimeline } from "@/components/shared/SemesterTimeline";
import { useAuth } from "@/contexts/AuthContext";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { dashboardService, projectService } from "@/lib";
import type {
  DepartmentHeadDashboardData,
  DepartmentHeadStats,
  EvaluationProgress,
  SemesterProgressInfo,
} from "@/types";
import type { DepartmentProject, DepartmentProjectsResponse } from "@/types";

// ── Types ────────────────────────────────────────────────────────────────────

type TabKey = "topics" | "evaluations" | "semester";

interface TabDef {
  key: TabKey;
  label: string;
  icon: string;
}

const TABS: TabDef[] = [
  { key: "topics", label: "Thống kê đề tài", icon: "topic" },
  { key: "evaluations", label: "Thống kê thẩm định", icon: "fact_check" },
  { key: "semester", label: "Thống kê học kỳ", icon: "calendar_month" },
];

// ── Animations ───────────────────────────────────────────────────────────────

const container = {
  hidden: {},
  show: { transition: { staggerChildren: 0.06 } },
};
const item = {
  hidden: { opacity: 0, y: 16 },
  show: { opacity: 1, y: 0, transition: { type: "spring", damping: 20 } },
};
const tabContent = {
  initial: { opacity: 0, y: 12 },
  animate: { opacity: 1, y: 0, transition: { duration: 0.25 } },
  exit: { opacity: 0, y: -12, transition: { duration: 0.15 } },
};

// ── Helpers ──────────────────────────────────────────────────────────────────

function DonutChart({
  segments,
  total,
  centerLabel,
  size = 160,
}: {
  segments: { label: string; value: number; color: string }[];
  total: number;
  centerLabel?: string;
  size?: number;
}) {
  const gradient = (() => {
    if (total === 0) return "conic-gradient(#e2e8f0 0deg 360deg)";
    let cumulative = 0;
    const stops = segments.map((s) => {
      const start = cumulative;
      cumulative += (s.value / total) * 360;
      return `${s.color} ${start}deg ${cumulative}deg`;
    });
    return `conic-gradient(${stops.join(", ")})`;
  })();

  return (
    <div className="flex items-center gap-6">
      <div
        className="relative rounded-full shrink-0"
        style={{ width: size, height: size, background: gradient }}
      >
        <div className="absolute flex flex-col items-center justify-center bg-white rounded-full inset-3">
          <span className="text-2xl font-bold text-slate-800">{total}</span>
          <span className="text-[10px] text-slate-500">
            {centerLabel ?? "tổng"}
          </span>
        </div>
      </div>
      <div className="space-y-2.5 flex-1">
        {segments.map((s) => (
          <div key={s.label} className="flex items-center gap-2 text-sm">
            <div
              className="rounded-full size-3 shrink-0"
              style={{ backgroundColor: s.color }}
            />
            <span className="flex-1 text-slate-600">{s.label}</span>
            <span className="font-bold text-slate-800">{s.value}</span>
            {total > 0 && (
              <span className="text-xs text-slate-400 w-10 text-right">
                {Math.round((s.value / total) * 100)}%
              </span>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

function HorizontalBar({
  items,
}: {
  items: { label: string; value: number; color: string }[];
}) {
  const max = Math.max(...items.map((i) => i.value), 1);
  return (
    <div className="space-y-3">
      {items.map((it) => (
        <div key={it.label} className="flex items-center gap-3">
          <span className="text-sm text-slate-600 w-36 truncate shrink-0">
            {it.label}
          </span>
          <div className="flex-1 h-7 bg-slate-100 rounded-lg overflow-hidden relative">
            <motion.div
              initial={{ width: 0 }}
              animate={{ width: `${(it.value / max) * 100}%` }}
              transition={{ duration: 0.6, ease: "easeOut" }}
              className="h-full rounded-lg"
              style={{ backgroundColor: it.color }}
            />
          </div>
          <span className="text-sm font-bold text-slate-700 w-8 text-right">
            {it.value}
          </span>
        </div>
      ))}
    </div>
  );
}

function StatCard({
  label,
  value,
  icon,
  gradient,
  subtitle,
}: {
  label: string;
  value: number | string;
  icon: string;
  gradient: string;
  subtitle?: string;
}) {
  return (
    <motion.div
      variants={item}
      whileHover={{ y: -2, boxShadow: "0 8px 25px -5px rgba(0,0,0,0.1)" }}
      className="relative flex items-start justify-between p-5 overflow-hidden bg-white border shadow-sm rounded-xl border-slate-100 group"
    >
      <div className="z-10">
        <p className="text-sm font-medium text-slate-500">{label}</p>
        <h3 className="mt-2 text-3xl font-bold text-slate-800">
          {typeof value === "number" ? String(value).padStart(2, "0") : value}
        </h3>
        {subtitle && (
          <p className="mt-1 text-xs text-slate-400">{subtitle}</p>
        )}
      </div>
      <div
        className={`bg-gradient-to-br ${gradient} text-white p-2.5 rounded-xl shadow-lg`}
      >
        <span className="material-symbols-outlined text-[22px]">{icon}</span>
      </div>
      <div className="absolute transition-transform duration-300 rounded-full -right-6 -bottom-6 bg-gradient-to-br from-blue-50 to-transparent size-28 opacity-40 group-hover:scale-125" />
    </motion.div>
  );
}

function ProgressRing({
  percentage,
  label,
  color = "var(--color-primary)",
}: {
  percentage: number;
  label: string;
  color?: string;
}) {
  const r = 52;
  const circumference = 2 * Math.PI * r;
  const offset = circumference * (1 - percentage / 100);

  return (
    <div className="flex flex-col items-center gap-3">
      <div className="relative size-32">
        <svg className="w-full h-full -rotate-90" viewBox="0 0 120 120">
          <circle
            cx="60"
            cy="60"
            r={r}
            fill="none"
            stroke="#e2e8f0"
            strokeWidth="10"
          />
          <motion.circle
            cx="60"
            cy="60"
            r={r}
            fill="none"
            stroke={color}
            strokeWidth="10"
            strokeLinecap="round"
            strokeDasharray={circumference}
            initial={{ strokeDashoffset: circumference }}
            animate={{ strokeDashoffset: offset }}
            transition={{ duration: 1, ease: "easeOut" }}
          />
        </svg>
        <div className="absolute inset-0 flex items-center justify-center">
          <span className="text-2xl font-bold text-slate-800">
            {percentage}%
          </span>
        </div>
      </div>
      <span className="text-sm font-medium text-slate-600">{label}</span>
    </div>
  );
}

// ── Tab panels ───────────────────────────────────────────────────────────────

function TopicStatistics({
  stats,
  evalProgress,
  projects,
}: {
  stats: DepartmentHeadStats | undefined;
  evalProgress: EvaluationProgress | undefined;
  projects: DepartmentProject[];
}) {
  const statusSegments = [
    { label: "Đã duyệt", value: evalProgress?.approved ?? 0, color: "#10b981" },
    { label: "Từ chối", value: evalProgress?.rejected ?? 0, color: "#f43f5e" },
    {
      label: "Cần chỉnh sửa",
      value: evalProgress?.needsModification ?? 0,
      color: "#f59e0b",
    },
    { label: "Đang chờ", value: evalProgress?.pending ?? 0, color: "#94a3b8" },
  ];
  const totalStatus =
    (evalProgress?.approved ?? 0) +
    (evalProgress?.rejected ?? 0) +
    (evalProgress?.needsModification ?? 0) +
    (evalProgress?.pending ?? 0);

  // Group by major
  const majorColors = ["#3b82f6", "#8b5cf6", "#06b6d4", "#f97316", "#ec4899", "#14b8a6"];
  const byMajor = useMemo(() => {
    const map = new Map<string, number>();
    projects.forEach((p) => {
      map.set(p.majorName, (map.get(p.majorName) ?? 0) + 1);
    });
    return Array.from(map.entries())
      .sort((a, b) => b[1] - a[1])
      .map(([label, value], i) => ({
        label,
        value,
        color: majorColors[i % majorColors.length],
      }));
  }, [projects]);

  // Group by source type (using status naming convention)
  const sourceSegments = useMemo(() => {
    // Projects with code starting with "PT-" are from pool, "DR-" are direct registration
    let fromPool = 0;
    let directReg = 0;
    projects.forEach((p) => {
      if (p.projectCode.startsWith("DR-")) {
        directReg++;
      } else {
        fromPool++;
      }
    });
    return [
      { label: "Từ kho đề tài", value: fromPool, color: "#3b82f6" },
      { label: "Sinh viên đề xuất", value: directReg, color: "#8b5cf6" },
    ];
  }, [projects]);
  const totalSource = sourceSegments.reduce((s, v) => s + v.value, 0);

  return (
    <motion.div
      variants={container}
      initial="hidden"
      animate="show"
      className="space-y-6"
    >
      {/* Stat cards */}
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <StatCard
          label="Tổng đề tài"
          value={stats?.totalProjects ?? 0}
          icon="topic"
          gradient="from-blue-500 to-blue-600"
        />
        <StatCard
          label="Chờ phân công"
          value={stats?.pendingAssignment ?? 0}
          icon="pending_actions"
          gradient="from-amber-500 to-orange-500"
        />
        <StatCard
          label="Đang thẩm định"
          value={stats?.inEvaluation ?? 0}
          icon="rate_review"
          gradient="from-indigo-500 to-indigo-600"
        />
        <StatCard
          label="Hoàn thành"
          value={stats?.completed ?? 0}
          icon="check_circle"
          gradient="from-emerald-500 to-emerald-600"
        />
      </div>

      {/* Charts row */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Status distribution donut */}
        <motion.div
          variants={item}
          className="p-6 bg-white border shadow-sm rounded-xl border-slate-100"
        >
          <h3 className="flex items-center gap-2 mb-5 text-lg font-bold text-slate-800">
            <span className="material-symbols-outlined text-primary">
              pie_chart
            </span>
            Phân bổ trạng thái đề tài
          </h3>
          <DonutChart
            segments={statusSegments}
            total={totalStatus}
            centerLabel="đề tài"
          />
        </motion.div>

        {/* Source type donut */}
        <motion.div
          variants={item}
          className="p-6 bg-white border shadow-sm rounded-xl border-slate-100"
        >
          <h3 className="flex items-center gap-2 mb-5 text-lg font-bold text-slate-800">
            <span className="material-symbols-outlined text-primary">
              category
            </span>
            Phân bổ theo nguồn đề tài
          </h3>
          <DonutChart
            segments={sourceSegments}
            total={totalSource}
            centerLabel="đề tài"
            size={140}
          />
        </motion.div>
      </div>

      {/* Major bar chart */}
      {byMajor.length > 0 && (
        <motion.div
          variants={item}
          className="p-6 bg-white border shadow-sm rounded-xl border-slate-100"
        >
          <h3 className="flex items-center gap-2 mb-5 text-lg font-bold text-slate-800">
            <span className="material-symbols-outlined text-primary">
              school
            </span>
            Phân bổ đề tài theo chuyên ngành
          </h3>
          <HorizontalBar items={byMajor} />
        </motion.div>
      )}
    </motion.div>
  );
}

function EvaluationStatistics({
  stats,
  evalProgress,
  projects,
}: {
  stats: DepartmentHeadStats | undefined;
  evalProgress: EvaluationProgress | undefined;
  projects: DepartmentProject[];
}) {
  const totalEval =
    (evalProgress?.approved ?? 0) +
    (evalProgress?.rejected ?? 0) +
    (evalProgress?.needsModification ?? 0) +
    (evalProgress?.pending ?? 0);

  const completedEval =
    (evalProgress?.approved ?? 0) +
    (evalProgress?.rejected ?? 0) +
    (evalProgress?.needsModification ?? 0);

  const completionPct = totalEval > 0 ? Math.round((completedEval / totalEval) * 100) : 0;

  const resultSegments = [
    { label: "Đã duyệt", value: evalProgress?.approved ?? 0, color: "#10b981" },
    { label: "Từ chối", value: evalProgress?.rejected ?? 0, color: "#f43f5e" },
    {
      label: "Cần chỉnh sửa",
      value: evalProgress?.needsModification ?? 0,
      color: "#f59e0b",
    },
  ];
  const completedTotal = resultSegments.reduce((s, v) => s + v.value, 0);

  // Evaluator workload from project data
  const evaluatorWorkload = useMemo(() => {
    const map = new Map<string, { name: string; count: number; submitted: number }>();
    projects.forEach((p) => {
      p.evaluators.forEach((ev) => {
        const existing = map.get(ev.evaluatorId);
        if (existing) {
          existing.count++;
          if (ev.hasSubmitted) existing.submitted++;
        } else {
          map.set(ev.evaluatorId, {
            name: ev.evaluatorName,
            count: 1,
            submitted: ev.hasSubmitted ? 1 : 0,
          });
        }
      });
    });
    return Array.from(map.values()).sort((a, b) => b.count - a.count);
  }, [projects]);

  const avgPerEvaluator =
    evaluatorWorkload.length > 0
      ? (
          evaluatorWorkload.reduce((s, e) => s + e.count, 0) /
          evaluatorWorkload.length
        ).toFixed(1)
      : "0";

  const workloadColors = ["#3b82f6", "#8b5cf6", "#06b6d4", "#f97316", "#ec4899", "#14b8a6", "#f43f5e", "#10b981"];

  return (
    <motion.div
      variants={container}
      initial="hidden"
      animate="show"
      className="space-y-6"
    >
      {/* Stat cards */}
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <StatCard
          label="Tổng thẩm định"
          value={totalEval}
          icon="assignment"
          gradient="from-blue-500 to-blue-600"
        />
        <StatCard
          label="Đã hoàn thành"
          value={completedEval}
          icon="task_alt"
          gradient="from-emerald-500 to-emerald-600"
          subtitle={`${completionPct}% hoàn thành`}
        />
        <StatCard
          label="Cần quyết định"
          value={stats?.needsFinalDecision ?? 0}
          icon="gavel"
          gradient="from-rose-500 to-rose-600"
        />
        <StatCard
          label="Tổng thẩm định viên"
          value={stats?.totalEvaluators ?? 0}
          icon="group"
          gradient="from-indigo-500 to-indigo-600"
          subtitle={`TB ${avgPerEvaluator} đề tài/người`}
        />
      </div>

      {/* Progress + Results row */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Completion ring */}
        <motion.div
          variants={item}
          className="p-6 bg-white border shadow-sm rounded-xl border-slate-100 flex flex-col items-center justify-center"
        >
          <h3 className="flex items-center gap-2 mb-6 text-lg font-bold text-slate-800 self-start">
            <span className="material-symbols-outlined text-primary">
              donut_large
            </span>
            Tiến độ hoàn thành
          </h3>
          <ProgressRing percentage={completionPct} label="Đề tài đã thẩm định" />
          <p className="mt-4 text-sm text-slate-500">
            {completedEval}/{totalEval} đề tài đã có kết quả
          </p>
        </motion.div>

        {/* Results donut */}
        <motion.div
          variants={item}
          className="p-6 bg-white border shadow-sm rounded-xl border-slate-100 lg:col-span-2"
        >
          <h3 className="flex items-center gap-2 mb-5 text-lg font-bold text-slate-800">
            <span className="material-symbols-outlined text-primary">
              analytics
            </span>
            Kết quả thẩm định
          </h3>
          <DonutChart
            segments={resultSegments}
            total={completedTotal}
            centerLabel="đã xử lý"
            size={140}
          />
        </motion.div>
      </div>

      {/* Evaluator workload */}
      {evaluatorWorkload.length > 0 && (
        <motion.div
          variants={item}
          className="p-6 bg-white border shadow-sm rounded-xl border-slate-100"
        >
          <h3 className="flex items-center gap-2 mb-5 text-lg font-bold text-slate-800">
            <span className="material-symbols-outlined text-primary">
              leaderboard
            </span>
            Khối lượng công việc thẩm định viên
          </h3>
          <div className="space-y-3">
            {evaluatorWorkload.map((ev, idx) => (
              <div key={idx} className="flex items-center gap-3">
                <div className="flex items-center gap-2 w-44 shrink-0">
                  <div
                    className="flex items-center justify-center text-white rounded-lg size-8 text-xs font-bold shrink-0"
                    style={{
                      backgroundColor: workloadColors[idx % workloadColors.length],
                    }}
                  >
                    {ev.name
                      .split(" ")
                      .slice(-1)[0]
                      ?.charAt(0)
                      ?.toUpperCase() ?? "?"}
                  </div>
                  <span className="text-sm text-slate-700 truncate">
                    {ev.name}
                  </span>
                </div>
                <div className="flex-1 h-7 bg-slate-100 rounded-lg overflow-hidden relative flex">
                  <motion.div
                    initial={{ width: 0 }}
                    animate={{
                      width: `${(ev.submitted / Math.max(...evaluatorWorkload.map((e) => e.count), 1)) * 100}%`,
                    }}
                    transition={{ duration: 0.6, ease: "easeOut" }}
                    className="h-full bg-emerald-400"
                  />
                  <motion.div
                    initial={{ width: 0 }}
                    animate={{
                      width: `${((ev.count - ev.submitted) / Math.max(...evaluatorWorkload.map((e) => e.count), 1)) * 100}%`,
                    }}
                    transition={{ duration: 0.6, ease: "easeOut", delay: 0.1 }}
                    className="h-full bg-slate-300"
                  />
                </div>
                <div className="flex items-center gap-2 text-sm w-28 shrink-0 justify-end">
                  <span className="text-emerald-600 font-semibold">
                    {ev.submitted}
                  </span>
                  <span className="text-slate-400">/</span>
                  <span className="text-slate-700 font-bold">{ev.count}</span>
                  <span className="text-xs text-slate-400">đề tài</span>
                </div>
              </div>
            ))}
          </div>
          <div className="flex items-center gap-4 mt-4 pt-3 border-t border-slate-100">
            <div className="flex items-center gap-1.5 text-xs text-slate-500">
              <div className="w-3 h-3 rounded-sm bg-emerald-400" />
              Đã thẩm định
            </div>
            <div className="flex items-center gap-1.5 text-xs text-slate-500">
              <div className="w-3 h-3 rounded-sm bg-slate-300" />
              Chưa thẩm định
            </div>
          </div>
        </motion.div>
      )}
    </motion.div>
  );
}

function SemesterStatistics({
  stats,
  semester,
  projects,
}: {
  stats: DepartmentHeadStats | undefined;
  semester: SemesterProgressInfo | null | undefined;
  projects: DepartmentProject[];
}) {
  // Group projects by semester
  const bySemester = useMemo(() => {
    const map = new Map<string, number>();
    projects.forEach((p) => {
      const sem = p.semesterName || "Không xác định";
      map.set(sem, (map.get(sem) ?? 0) + 1);
    });
    const semColors = ["#3b82f6", "#8b5cf6", "#06b6d4", "#f97316"];
    return Array.from(map.entries())
      .sort((a, b) => a[0].localeCompare(b[0]))
      .map(([label, value], i) => ({
        label,
        value,
        color: semColors[i % semColors.length],
      }));
  }, [projects]);

  return (
    <motion.div
      variants={container}
      initial="hidden"
      animate="show"
      className="space-y-6"
    >
      {/* Resource overview cards */}
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <StatCard
          label="Tổng giảng viên"
          value={stats?.totalMentors ?? 0}
          icon="school"
          gradient="from-blue-500 to-blue-600"
        />
        <StatCard
          label="Thẩm định viên"
          value={stats?.totalEvaluators ?? 0}
          icon="group"
          gradient="from-indigo-500 to-indigo-600"
        />
        <StatCard
          label="Tổng đề tài"
          value={stats?.totalProjects ?? 0}
          icon="topic"
          gradient="from-emerald-500 to-emerald-600"
        />
        <StatCard
          label="Đã hoàn thành"
          value={stats?.completed ?? 0}
          icon="check_circle"
          gradient="from-amber-500 to-orange-500"
          subtitle={
            (stats?.totalProjects ?? 0) > 0
              ? `${Math.round(((stats?.completed ?? 0) / (stats?.totalProjects ?? 1)) * 100)}% hoàn thành`
              : undefined
          }
        />
      </div>

      {/* Semester timeline */}
      {semester && semester.phases.length > 0 && (
        <motion.div
          variants={item}
          className="p-6 bg-white border shadow-sm rounded-xl border-slate-100"
        >
          <div className="flex items-center gap-2 mb-4">
            <span className="material-symbols-outlined text-primary">
              timeline
            </span>
            <h3 className="text-lg font-bold text-slate-800">
              Tiến trình học kỳ hiện tại
            </h3>
            <span className="ml-auto text-sm font-medium text-primary bg-primary/10 px-3 py-1 rounded-full">
              {semester.semesterName}
            </span>
          </div>
          <SemesterTimeline phases={semester.phases} />
        </motion.div>
      )}

      {/* Projects per semester bar chart */}
      {bySemester.length > 0 && (
        <motion.div
          variants={item}
          className="p-6 bg-white border shadow-sm rounded-xl border-slate-100"
        >
          <h3 className="flex items-center gap-2 mb-5 text-lg font-bold text-slate-800">
            <span className="material-symbols-outlined text-primary">
              bar_chart
            </span>
            Số lượng đề tài theo học kỳ
          </h3>
          <HorizontalBar items={bySemester} />
        </motion.div>
      )}

      {/* Resource ratio summary */}
      <motion.div
        variants={item}
        className="p-6 bg-white border shadow-sm rounded-xl border-slate-100"
      >
        <h3 className="flex items-center gap-2 mb-5 text-lg font-bold text-slate-800">
          <span className="material-symbols-outlined text-primary">
            equalizer
          </span>
          Tỷ lệ nguồn lực
        </h3>
        <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
          <div className="text-center p-4 bg-blue-50/50 rounded-xl border border-blue-100">
            <p className="text-sm text-blue-600 font-medium mb-1">
              Đề tài / Giảng viên
            </p>
            <p className="text-3xl font-bold text-blue-700">
              {(stats?.totalMentors ?? 0) > 0
                ? ((stats?.totalProjects ?? 0) / (stats?.totalMentors ?? 1)).toFixed(1)
                : "0"}
            </p>
          </div>
          <div className="text-center p-4 bg-indigo-50/50 rounded-xl border border-indigo-100">
            <p className="text-sm text-indigo-600 font-medium mb-1">
              Đề tài / Thẩm định viên
            </p>
            <p className="text-3xl font-bold text-indigo-700">
              {(stats?.totalEvaluators ?? 0) > 0
                ? ((stats?.totalProjects ?? 0) / (stats?.totalEvaluators ?? 1)).toFixed(1)
                : "0"}
            </p>
          </div>
          <div className="text-center p-4 bg-emerald-50/50 rounded-xl border border-emerald-100">
            <p className="text-sm text-emerald-600 font-medium mb-1">
              Tỷ lệ duyệt thành công
            </p>
            <p className="text-3xl font-bold text-emerald-700">
              {(stats?.totalProjects ?? 0) > 0
                ? `${Math.round(((stats?.completed ?? 0) / (stats?.totalProjects ?? 1)) * 100)}%`
                : "0%"}
            </p>
          </div>
        </div>
      </motion.div>
    </motion.div>
  );
}

// ── Main component ───────────────────────────────────────────────────────────

export function DepartmentHeadStatisticsPage() {
  const [activeTab, setActiveTab] = useState<TabKey>("topics");
  const [dashboardData, setDashboardData] =
    useState<DepartmentHeadDashboardData | null>(null);
  const [projectsData, setProjectsData] =
    useState<DepartmentProjectsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const { user } = useAuth();
  const { showError } = useSystemError();

  useEffect(() => {
    Promise.all([
      dashboardService.getDepartmentHeadDashboard(),
      projectService.getDepartmentProjects(),
    ])
      .then(([dashboard, projects]) => {
        setDashboardData(dashboard);
        setProjectsData(projects);
      })
      .catch((err) =>
        showError(
          err instanceof Error ? err.message : "Không thể tải dữ liệu thống kê."
        )
      )
      .finally(() => setLoading(false));
  }, []);

  const stats = dashboardData?.stats;
  const evalProgress = dashboardData?.evaluationProgress;
  const semester = dashboardData?.semesterProgress;
  const projects = projectsData?.items ?? [];

  return (
    <div className="flex flex-col h-full">
      <Header
        title="Thống kê"
        subtitle={
          dashboardData?.departmentName
            ? `Bộ môn ${dashboardData.departmentName}`
            : "Thống kê tổng hợp bộ môn"
        }
        role="department-head"
        showSearch={false}
      />

      <div className="flex-1 overflow-y-auto">
        <div className="px-8 py-6 space-y-6">
          {/* Welcome */}
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-extrabold text-slate-900">
                Thống kê bộ môn
              </h1>
              <p className="mt-1 text-slate-500">
                Tổng quan dữ liệu đề tài, thẩm định và học kỳ
                {user?.name && (
                  <>
                    {" "}
                    · Chủ nhiệm{" "}
                    <span className="font-semibold text-primary">
                      {user.name}
                    </span>
                  </>
                )}
              </p>
            </div>
          </div>

          {/* Tab navigation */}
          <div className="flex gap-1 p-1 bg-slate-100 rounded-xl w-fit">
            {TABS.map((tab) => (
              <button
                key={tab.key}
                onClick={() => setActiveTab(tab.key)}
                className={`flex items-center gap-2 px-5 py-2.5 rounded-lg text-sm font-semibold transition-all ${
                  activeTab === tab.key
                    ? "bg-white text-primary shadow-sm"
                    : "text-slate-500 hover:text-slate-700 hover:bg-white/50"
                }`}
              >
                <span className="material-symbols-outlined text-[18px]">
                  {tab.icon}
                </span>
                {tab.label}
              </button>
            ))}
          </div>

          {/* Tab content */}
          {loading ? (
            <div className="space-y-6">
              <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                {Array.from({ length: 4 }).map((_, i) => (
                  <div
                    key={i}
                    className="h-32 p-5 bg-white border rounded-xl border-slate-100 animate-pulse"
                  >
                    <div className="w-20 h-4 mb-3 rounded bg-slate-200" />
                    <div className="w-12 h-8 rounded bg-slate-200" />
                  </div>
                ))}
              </div>
              <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
                {Array.from({ length: 2 }).map((_, i) => (
                  <div
                    key={i}
                    className="p-6 bg-white border h-64 rounded-xl border-slate-100 animate-pulse"
                  >
                    <div className="w-40 h-5 mb-4 rounded bg-slate-200" />
                    <div className="w-32 h-32 rounded-full bg-slate-200 mx-auto" />
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <AnimatePresence mode="wait">
              <motion.div key={activeTab} {...tabContent}>
                {activeTab === "topics" && (
                  <TopicStatistics
                    stats={stats}
                    evalProgress={evalProgress}
                    projects={projects}
                  />
                )}
                {activeTab === "evaluations" && (
                  <EvaluationStatistics
                    stats={stats}
                    evalProgress={evalProgress}
                    projects={projects}
                  />
                )}
                {activeTab === "semester" && (
                  <SemesterStatistics
                    stats={stats}
                    semester={semester}
                    projects={projects}
                  />
                )}
              </motion.div>
            </AnimatePresence>
          )}
        </div>
      </div>
    </div>
  );
}
