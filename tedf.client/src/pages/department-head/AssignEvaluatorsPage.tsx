import { useState, useEffect, useCallback, useMemo, useRef } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Header } from "@/components/layout/Header";
import {
  departmentHeadService,
  groupProjects,
  type DepartmentProject,
  type DepartmentEvaluator,
  type GroupedProjects,
} from "@/lib/departmentHeadService";

// ── Tab config ───────────────────────────────────────────────────────────────

type TabKey = "pending" | "in-evaluation" | "needs-decision" | "completed";

const tabs: { key: TabKey; label: string; icon: string }[] = [
  { key: "pending", label: "Chờ phân công", icon: "assignment_ind" },
  { key: "in-evaluation", label: "Đang thẩm định", icon: "rate_review" },
  { key: "needs-decision", label: "Cần quyết định", icon: "gavel" },
  { key: "completed", label: "Hoàn thành", icon: "check_circle" },
];

// ── Helpers ──────────────────────────────────────────────────────────────────

function relativeDate(iso: string | null): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return d.toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

function resultLabel(result: string | null): string {
  if (!result) return "Chưa có";
  const map: Record<string, string> = {
    Approved: "Duyệt",
    Rejected: "Từ chối",
    NeedsModification: "Yêu cầu chỉnh sửa",
  };
  return map[result] ?? result;
}

function resultColor(result: string | null): string {
  if (!result) return "text-slate-400";
  const map: Record<string, string> = {
    Approved: "text-green-600",
    Rejected: "text-red-600",
    NeedsModification: "text-amber-600",
  };
  return map[result] ?? "text-slate-500";
}

function statusBadge(status: string) {
  const map: Record<string, { bg: string; text: string; label: string }> = {
    PendingEvaluation: { bg: "bg-amber-50", text: "text-amber-700", label: "Chờ thẩm định" },
    Approved: { bg: "bg-green-50", text: "text-green-700", label: "Đã duyệt" },
    Rejected: { bg: "bg-red-50", text: "text-red-700", label: "Từ chối" },
    NeedsModification: { bg: "bg-orange-50", text: "text-orange-700", label: "Cần chỉnh sửa" },
    Cancelled: { bg: "bg-slate-100", text: "text-slate-500", label: "Đã hủy" },
  };
  const s = map[status] ?? { bg: "bg-slate-100", text: "text-slate-600", label: status };
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${s.bg} ${s.text}`}>
      {s.label}
    </span>
  );
}

function getMentorName(p: DepartmentProject): string {
  return p.mentors?.[0]?.mentorName ?? "—";
}

const PAGE_SIZE = 10;

function matchesSearch(p: DepartmentProject, query: string): boolean {
  if (!query) return true;
  const q = query.toLowerCase();
  return (
    p.nameVi.toLowerCase().includes(q) ||
    p.nameEn.toLowerCase().includes(q) ||
    p.projectCode.toLowerCase().includes(q) ||
    p.majorName.toLowerCase().includes(q) ||
    (p.mentors?.some((m) => m.mentorName.toLowerCase().includes(q)) ?? false)
  );
}

function getPageNumbers(currentPage: number, totalPages: number): (number | "...")[] {
  if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1);
  const pages: (number | "...")[] = [1];
  if (currentPage > 3) pages.push("...");
  const start = Math.max(2, currentPage - 1);
  const end = Math.min(totalPages - 1, currentPage + 1);
  for (let i = start; i <= end; i++) pages.push(i);
  if (currentPage < totalPages - 2) pages.push("...");
  if (totalPages > 1) pages.push(totalPages);
  return pages;
}

// ── Main Component ───────────────────────────────────────────────────────────

export function AssignEvaluatorsPage() {
  const [activeTab, setActiveTab] = useState<TabKey>("pending");
  const [grouped, setGrouped] = useState<GroupedProjects | null>(null);
  const [evaluators, setEvaluators] = useState<DepartmentEvaluator[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Search & pagination
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [page, setPage] = useState(1);

  // Modal state
  const [assignModal, setAssignModal] = useState<DepartmentProject | null>(null);
  const [decisionModal, setDecisionModal] = useState<DepartmentProject | null>(null);

  // Debounced search (400ms)
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(timer);
  }, [search]);

  // Reset page when tab or search changes
  useEffect(() => {
    setPage(1);
  }, [activeTab, debouncedSearch]);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [projectsResult, evalsResult] = await Promise.allSettled([
        departmentHeadService.getProjects(),
        departmentHeadService.getEvaluators(),
      ]);
      if (projectsResult.status === "fulfilled") {
        setGrouped(groupProjects(projectsResult.value));
      }
      if (evalsResult.status === "fulfilled") {
        setEvaluators(evalsResult.value ?? []);
      }
      if (projectsResult.status === "rejected" && evalsResult.status === "rejected") {
        setError("Không thể tải dữ liệu");
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tải dữ liệu");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const getTabData = (tab: TabKey): DepartmentProject[] => {
    if (!grouped) return [];
    const map: Record<TabKey, DepartmentProject[]> = {
      pending: grouped.pendingAssignment,
      "in-evaluation": grouped.inEvaluation,
      "needs-decision": grouped.needsDecision,
      completed: grouped.completed,
    };
    return map[tab] ?? [];
  };

  const getTabCount = (tab: TabKey): number => getTabData(tab).length;

  // Filtered + paginated data for active tab
  const filteredData = useMemo(
    () => getTabData(activeTab).filter((p) => matchesSearch(p, debouncedSearch)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [activeTab, debouncedSearch, grouped]
  );

  const totalPages = Math.max(1, Math.ceil(filteredData.length / PAGE_SIZE));
  const paginatedData = filteredData.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const pageNumbers = getPageNumbers(page, totalPages);

  return (
    <div>
      <Header
        title="Phân công thẩm định"
        subtitle="Phân công evaluator và theo dõi kết quả thẩm định đề tài"
        role="department-head"
        showSearch={false}
      />

      <div className="p-8">
        {/* Search */}
        <div className="mb-4">
          <div className="relative max-w-md">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-400 text-[18px]">
              search
            </span>
            <input
              className="w-full pl-9 pr-4 py-2.5 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary bg-white placeholder-slate-400 text-slate-700"
              placeholder="Tìm theo tên đề tài, mã đề tài, mentor..."
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            {search && (
              <button
                onClick={() => setSearch("")}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
              >
                <span className="material-symbols-outlined text-[18px]">close</span>
              </button>
            )}
          </div>
        </div>

        {/* Tabs */}
        <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
          <div className="border-b border-slate-200 px-4">
            <div className="flex gap-1">
              {tabs.map((tab) => (
                <button
                  key={tab.key}
                  onClick={() => setActiveTab(tab.key)}
                  className={`relative flex items-center gap-2 px-5 py-3 text-sm font-medium transition-colors ${
                    activeTab === tab.key
                      ? "text-primary"
                      : "text-gray-500 hover:text-gray-700"
                  }`}
                >
                  <span
                    className={`material-symbols-outlined text-[18px] ${activeTab === tab.key ? "fill-1" : ""}`}
                  >
                    {tab.icon}
                  </span>
                  {tab.label}
                  {getTabCount(tab.key) > 0 && (
                    <span
                      className={`text-xs px-1.5 py-0.5 rounded-full font-bold ${
                        activeTab === tab.key
                          ? "bg-primary/10 text-primary"
                          : "bg-slate-100 text-slate-500"
                      }`}
                    >
                      {getTabCount(tab.key)}
                    </span>
                  )}
                  {activeTab === tab.key && (
                    <motion.div
                      layoutId="tab-underline"
                      className="absolute bottom-0 left-0 right-0 h-0.5 bg-primary rounded-full"
                      transition={{ type: "spring", stiffness: 500, damping: 35 }}
                    />
                  )}
                </button>
              ))}
            </div>
          </div>

          {/* Search result info */}
          {debouncedSearch && !loading && !error && (
            <div className="px-6 pt-4 pb-0">
              <p className="text-sm text-slate-500">
                Tìm thấy <span className="font-semibold text-slate-700">{filteredData.length}</span> kết quả
                {debouncedSearch && (
                  <> cho &ldquo;<span className="font-medium text-slate-700">{debouncedSearch}</span>&rdquo;</>
                )}
              </p>
            </div>
          )}

          {/* Content */}
          <div className="p-6">
            {loading ? (
              <LoadingSkeleton />
            ) : error ? (
              <ErrorState message={error} onRetry={fetchData} />
            ) : (
              <AnimatePresence mode="wait">
                <motion.div
                  key={`${activeTab}-${page}-${debouncedSearch}`}
                  initial={{ opacity: 0, y: 8 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -8 }}
                  transition={{ duration: 0.15 }}
                >
                  {activeTab === "pending" && (
                    <PendingTab projects={paginatedData} onAssign={setAssignModal} />
                  )}
                  {activeTab === "in-evaluation" && (
                    <InEvaluationTab projects={paginatedData} />
                  )}
                  {activeTab === "needs-decision" && (
                    <NeedsDecisionTab projects={paginatedData} onDecide={setDecisionModal} />
                  )}
                  {activeTab === "completed" && (
                    <CompletedTab projects={paginatedData} />
                  )}
                </motion.div>
              </AnimatePresence>
            )}
          </div>

          {/* Pagination */}
          {!loading && !error && filteredData.length > PAGE_SIZE && (
            <div className="p-4 border-t border-slate-200 flex items-center justify-between bg-white">
              <span className="text-sm text-slate-500 hidden sm:inline">
                Hiển thị{" "}
                <span className="font-medium text-slate-900">
                  {(page - 1) * PAGE_SIZE + 1}-{Math.min(page * PAGE_SIZE, filteredData.length)}
                </span>{" "}
                trên <span className="font-medium text-slate-900">{filteredData.length}</span> đề tài
              </span>
              <div className="flex gap-1 w-full sm:w-auto justify-center sm:justify-end">
                <button
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1}
                  className="px-3 py-1 border border-slate-200 rounded hover:bg-slate-50 text-slate-600 text-sm disabled:opacity-50 transition-colors"
                >
                  Trước
                </button>
                {pageNumbers.map((p, i) =>
                  p === "..." ? (
                    <span key={`dots-${i}`} className="px-2 py-1 text-slate-400 hidden sm:inline">
                      ...
                    </span>
                  ) : (
                    <button
                      key={p}
                      onClick={() => setPage(p)}
                      className={`px-3 py-1 rounded text-sm transition-colors ${
                        p === page
                          ? "bg-primary text-white hover:bg-primary/90"
                          : "border border-slate-200 hover:bg-slate-50 text-slate-600"
                      }`}
                    >
                      {p}
                    </button>
                  )
                )}
                <button
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page >= totalPages}
                  className="px-3 py-1 border border-slate-200 rounded hover:bg-slate-50 text-slate-600 text-sm disabled:opacity-50 transition-colors"
                >
                  Sau
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Assign Modal */}
      <AnimatePresence>
        {assignModal && (
          <AssignEvaluatorModal
            project={assignModal}
            evaluators={evaluators}
            onClose={() => setAssignModal(null)}
            onSuccess={() => {
              setAssignModal(null);
              fetchData();
            }}
          />
        )}
      </AnimatePresence>

      {/* Decision Modal */}
      <AnimatePresence>
        {decisionModal && (
          <FinalDecisionModal
            project={decisionModal}
            onClose={() => setDecisionModal(null)}
            onSuccess={() => {
              setDecisionModal(null);
              fetchData();
            }}
          />
        )}
      </AnimatePresence>
    </div>
  );
}

// ── Tab: Chờ phân công ───────────────────────────────────────────────────────

function PendingTab({
  projects,
  onAssign,
}: {
  projects: DepartmentProject[];
  onAssign: (p: DepartmentProject) => void;
}) {
  if (projects.length === 0) return <EmptyState message="Không có đề tài nào chờ phân công" icon="assignment_turned_in" />;
  return (
    <div className="space-y-3">
      {projects.map((p) => (
        <div key={p.projectId} className="flex items-center justify-between p-4 bg-slate-50 rounded-lg border border-slate-100">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <span className="text-xs font-mono text-slate-400">{p.projectCode}</span>
              {statusBadge(p.status)}
            </div>
            <p className="font-semibold text-slate-800 truncate">{p.nameVi}</p>
            <p className="text-sm text-slate-500 truncate">{p.nameEn}</p>
            <div className="flex items-center gap-4 mt-2 text-xs text-slate-500">
              <span className="flex items-center gap-1">
                <span className="material-symbols-outlined text-[14px]">person</span>
                {getMentorName(p)}
              </span>
              <span className="flex items-center gap-1">
                <span className="material-symbols-outlined text-[14px]">school</span>
                {p.majorName}
              </span>
              <span className="flex items-center gap-1">
                <span className="material-symbols-outlined text-[14px]">calendar_today</span>
                {relativeDate(p.submittedAt)}
              </span>
              <span className="flex items-center gap-1">
                <span className="material-symbols-outlined text-[14px]">rate_review</span>
                {p.assignedEvaluatorCount}/2 evaluator
              </span>
            </div>
          </div>
          <button
            onClick={() => onAssign(p)}
            className="ml-4 flex items-center gap-2 px-4 py-2 bg-primary text-white text-sm font-medium rounded-lg hover:bg-primary/90 transition-colors shrink-0"
          >
            <span className="material-symbols-outlined text-[18px]">person_add</span>
            Phân công
          </button>
        </div>
      ))}
    </div>
  );
}

// ── Tab: Đang thẩm định ──────────────────────────────────────────────────────

function InEvaluationTab({ projects }: { projects: DepartmentProject[] }) {
  if (projects.length === 0) return <EmptyState message="Không có đề tài nào đang thẩm định" icon="rate_review" />;
  return (
    <div className="space-y-3">
      {projects.map((p) => (
        <div key={p.projectId} className="p-4 bg-slate-50 rounded-lg border border-slate-100">
          <div className="flex items-center gap-2 mb-1">
            <span className="text-xs font-mono text-slate-400">{p.projectCode}</span>
            {statusBadge(p.status)}
          </div>
          <p className="font-semibold text-slate-800">{p.nameVi}</p>
          <div className="flex items-center gap-4 mt-1 text-xs text-slate-500">
            <span>Mentor: {getMentorName(p)}</span>
            <span>{p.majorName}</span>
          </div>
          <div className="mt-3 grid grid-cols-2 gap-3">
            {p.evaluators.map((a) => (
              <div key={a.assignmentId} className="flex items-center gap-3 p-3 bg-white rounded-lg border border-slate-200">
                <div className="size-8 rounded-full bg-primary/10 flex items-center justify-center shrink-0">
                  <span className="material-symbols-outlined text-primary text-[16px]">person</span>
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-slate-700 truncate">{a.evaluatorName}</p>
                  <p className={`text-xs font-medium ${a.hasSubmitted ? resultColor(a.individualResult) : "text-slate-400"}`}>
                    {a.hasSubmitted ? resultLabel(a.individualResult) : "Chưa thẩm định"}
                  </p>
                </div>
                {a.hasSubmitted ? (
                  <span className="material-symbols-outlined text-green-500 text-[18px]">check_circle</span>
                ) : (
                  <span className="material-symbols-outlined text-slate-300 text-[18px]">hourglass_empty</span>
                )}
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

// ── Tab: Cần quyết định ──────────────────────────────────────────────────────

function NeedsDecisionTab({
  projects,
  onDecide,
}: {
  projects: DepartmentProject[];
  onDecide: (p: DepartmentProject) => void;
}) {
  if (projects.length === 0) return <EmptyState message="Không có đề tài nào cần quyết định" icon="gavel" />;
  return (
    <div className="space-y-4">
      {projects.map((p) => (
        <div key={p.projectId} className="p-4 bg-amber-50/50 rounded-lg border border-amber-200">
          <div className="flex items-center gap-2 mb-1">
            <span className="text-xs font-mono text-slate-400">{p.projectCode}</span>
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-700">
              Cần quyết định
            </span>
          </div>
          <p className="font-semibold text-slate-800">{p.nameVi}</p>
          <div className="flex items-center gap-4 mt-1 text-xs text-slate-500">
            <span>Mentor: {getMentorName(p)}</span>
            <span>{p.majorName}</span>
          </div>

          {/* Evaluator results */}
          <div className="mt-3 grid grid-cols-2 gap-3">
            {p.evaluators.map((a) => (
              <div key={a.assignmentId} className="p-3 bg-white rounded-lg border border-slate-200">
                <div className="flex items-center gap-2 mb-2">
                  <div className="size-7 rounded-full bg-primary/10 flex items-center justify-center shrink-0">
                    <span className="material-symbols-outlined text-primary text-[14px]">person</span>
                  </div>
                  <span className="text-sm font-medium text-slate-700">{a.evaluatorName}</span>
                </div>
                <p className={`text-sm font-semibold ${resultColor(a.individualResult)}`}>{resultLabel(a.individualResult)}</p>
                {a.feedback && <p className="text-xs text-slate-500 mt-1 line-clamp-3">{a.feedback}</p>}
              </div>
            ))}
          </div>

          <div className="mt-3 flex justify-end">
            <button
              onClick={() => onDecide(p)}
              className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white text-sm font-medium rounded-lg hover:bg-amber-600 transition-colors"
            >
              <span className="material-symbols-outlined text-[18px]">gavel</span>
              Đưa ra quyết định
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}

// ── Tab: Hoàn thành ──────────────────────────────────────────────────────────

function CompletedTab({ projects }: { projects: DepartmentProject[] }) {
  if (projects.length === 0) return <EmptyState message="Chưa có đề tài nào hoàn thành thẩm định" icon="check_circle" />;
  return (
    <div className="space-y-3">
      {projects.map((p) => (
        <div key={p.projectId} className="p-4 bg-slate-50 rounded-lg border border-slate-100">
          <div className="flex items-center gap-2 mb-1">
            <span className="text-xs font-mono text-slate-400">{p.projectCode}</span>
            {statusBadge(p.status)}
          </div>
          <p className="font-semibold text-slate-800">{p.nameVi}</p>
          <div className="flex items-center gap-4 mt-1 text-xs text-slate-500">
            <span>Mentor: {getMentorName(p)}</span>
            <span>{p.majorName}</span>
          </div>
          <div className="mt-3 grid grid-cols-2 gap-3">
            {p.evaluators.map((a) => (
              <div key={a.assignmentId} className="flex items-center gap-2 p-2 bg-white rounded border border-slate-200">
                <span className="text-sm text-slate-600">{a.evaluatorName}:</span>
                <span className={`text-sm font-medium ${resultColor(a.individualResult)}`}>{resultLabel(a.individualResult)}</span>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

// ── Assign Evaluator Modal ───────────────────────────────────────────────────

function AssignEvaluatorModal({
  project,
  evaluators,
  onClose,
  onSuccess,
}: {
  project: DepartmentProject;
  evaluators: DepartmentEvaluator[];
  onClose: () => void;
  onSuccess: () => void;
}) {
  const existingIds = new Set(project.evaluators.map((a) => a.evaluatorId));
  const nextOrder = project.evaluators.length + 1;
  const needsBoth = project.evaluators.length === 0;

  const [eval1, setEval1] = useState("");
  const [eval2, setEval2] = useState("");
  const [eval1Search, setEval1Search] = useState("");
  const [eval2Search, setEval2Search] = useState("");
  const [eval1Open, setEval1Open] = useState(false);
  const [eval2Open, setEval2Open] = useState(false);
  const eval1Ref = useRef<HTMLDivElement>(null);
  const eval2Ref = useRef<HTMLDivElement>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (eval1Ref.current && !eval1Ref.current.contains(e.target as Node)) setEval1Open(false);
      if (eval2Ref.current && !eval2Ref.current.contains(e.target as Node)) setEval2Open(false);
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Filter out mentors and already-assigned evaluators
  const mentorIds = new Set(project.mentors.map((m) => m.mentorId));
  const available = evaluators.filter(
    (e) => !mentorIds.has(e.userId) && !existingIds.has(e.userId)
  );

  const selectedEval1 = available.find((e) => e.userId === eval1);
  const selectedEval2 = available.find((e) => e.userId === eval2);

  function filterEvaluators(list: typeof available, search: string) {
    if (!search.trim()) return list;
    const q = search.toLowerCase();
    return list.filter((e) => `${e.fullName} ${e.email}`.toLowerCase().includes(q));
  }

  const handleSubmit = async () => {
    setSubmitting(true);
    setError(null);
    try {
      if (needsBoth) {
        if (!eval1 || !eval2) {
          setError("Vui lòng chọn 2 evaluator");
          setSubmitting(false);
          return;
        }
        if (eval1 === eval2) {
          setError("Không thể chọn cùng 1 evaluator");
          setSubmitting(false);
          return;
        }
        await departmentHeadService.assignEvaluator(project.projectId, eval1, 1);
        await departmentHeadService.assignEvaluator(project.projectId, eval2, 2);
      } else {
        if (!eval1) {
          setError("Vui lòng chọn evaluator");
          setSubmitting(false);
          return;
        }
        await departmentHeadService.assignEvaluator(project.projectId, eval1, nextOrder);
      }
      setSuccess(true);
      setTimeout(onSuccess, 1500);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Phân công thất bại");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <motion.div
        initial={{ opacity: 0, scale: 0.95, y: 20 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.95, y: 20 }}
        className="bg-white rounded-2xl shadow-2xl w-full max-w-lg"
        onClick={(e) => e.stopPropagation()}
      >
        {success ? (
          <div className="p-8 text-center">
            <div className="size-16 mx-auto rounded-full bg-green-50 flex items-center justify-center mb-4">
              <span className="material-symbols-outlined text-4xl text-green-500">check_circle</span>
            </div>
            <h3 className="text-lg font-bold text-slate-900 mb-2">Phân công thành công!</h3>
            <p className="text-sm text-slate-500">Evaluator sẽ nhận được thông báo thẩm định đề tài.</p>
          </div>
        ) : (
          <>
            {/* Header */}
            <div className="px-6 py-4 border-b border-slate-200">
              <div className="flex items-center justify-between">
                <h2 className="text-lg font-bold text-slate-900">Phân công Evaluator</h2>
                <button onClick={onClose} className="p-1 hover:bg-slate-100 rounded-lg transition-colors">
                  <span className="material-symbols-outlined text-slate-400">close</span>
                </button>
              </div>
              <p className="text-sm text-slate-500 mt-1">{project.nameVi}</p>
              <p className="text-xs text-slate-400">Mentor: {getMentorName(project)}</p>
            </div>

            {/* Body */}
            <div className="p-6 space-y-4">
              {error && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">{error}</div>
              )}

              {/* Evaluator 1 */}
              {needsBoth && (
                <div ref={eval1Ref} className="relative">
                  <label className="block text-sm font-medium text-slate-700 mb-1.5">Evaluator 1</label>
                  <div className="relative">
                    <input
                      value={eval1 ? (selectedEval1 ? selectedEval1.fullName : '') : eval1Search}
                      onChange={(e) => {
                        setEval1Search(e.target.value);
                        setEval1Open(true);
                        if (eval1) { setEval1(""); }
                      }}
                      onFocus={() => setEval1Open(true)}
                      placeholder="Nhập tên evaluator để tìm kiếm..."
                      className="w-full px-3 py-2.5 pr-8 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary"
                    />
                    {eval1 ? (
                      <button type="button" onClick={() => { setEval1(""); setEval1Search(""); }} className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-red-500">
                        <span className="material-symbols-outlined text-lg">close</span>
                      </button>
                    ) : (
                      <span className="material-symbols-outlined text-lg text-slate-400 absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none">search</span>
                    )}
                  </div>
                  {eval1Open && !eval1 && (() => {
                    const filtered = filterEvaluators(available.filter((e) => e.userId !== eval2), eval1Search);
                    return filtered.length > 0 ? (
                      <div className="absolute z-20 mt-1 w-full bg-white border border-slate-200 rounded-lg shadow-lg max-h-48 overflow-y-auto">
                        {filtered.map((e) => (
                          <button key={e.userId} type="button" onClick={() => { setEval1(e.userId); setEval1Search(""); setEval1Open(false); }}
                            className="w-full text-left px-3 py-2.5 text-sm border-b border-slate-100 last:border-b-0 hover:bg-primary/5 transition-colors">
                            <div className="font-medium text-slate-900">{e.fullName}</div>
                            <div className="text-xs text-slate-500">{e.email} · {e.activeAssignmentCount} đề tài đang thẩm định</div>
                          </button>
                        ))}
                      </div>
                    ) : eval1Search.trim() ? (
                      <div className="absolute z-20 mt-1 w-full bg-white border border-slate-200 rounded-lg shadow-lg p-3 text-sm text-slate-500 text-center">Không tìm thấy</div>
                    ) : null;
                  })()}
                  {selectedEval1 && (
                    <p className="mt-1 text-xs text-slate-500">{selectedEval1.email} · {selectedEval1.activeAssignmentCount} đề tài đang thẩm định</p>
                  )}
                </div>
              )}

              {/* Evaluator 2 (or single remaining) */}
              <div ref={eval2Ref} className="relative">
                <label className="block text-sm font-medium text-slate-700 mb-1.5">
                  {needsBoth ? "Evaluator 2" : `Evaluator ${nextOrder}`}
                </label>
                <div className="relative">
                  <input
                    value={(needsBoth ? eval2 : eval1) ? ((needsBoth ? selectedEval2 : selectedEval1)?.fullName ?? '') : eval2Search}
                    onChange={(e) => {
                      setEval2Search(e.target.value);
                      setEval2Open(true);
                      if (needsBoth ? eval2 : eval1) { needsBoth ? setEval2("") : setEval1(""); }
                    }}
                    onFocus={() => setEval2Open(true)}
                    placeholder="Nhập tên evaluator để tìm kiếm..."
                    className="w-full px-3 py-2.5 pr-8 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                  {(needsBoth ? eval2 : eval1) ? (
                    <button type="button" onClick={() => { needsBoth ? setEval2("") : setEval1(""); setEval2Search(""); }} className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-red-500">
                      <span className="material-symbols-outlined text-lg">close</span>
                    </button>
                  ) : (
                    <span className="material-symbols-outlined text-lg text-slate-400 absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none">search</span>
                  )}
                </div>
                {eval2Open && !(needsBoth ? eval2 : eval1) && (() => {
                  const filtered = filterEvaluators(available.filter((e) => (needsBoth ? e.userId !== eval1 : true)), eval2Search);
                  return filtered.length > 0 ? (
                    <div className="absolute z-20 mt-1 w-full bg-white border border-slate-200 rounded-lg shadow-lg max-h-48 overflow-y-auto">
                      {filtered.map((e) => (
                        <button key={e.userId} type="button" onClick={() => { needsBoth ? setEval2(e.userId) : setEval1(e.userId); setEval2Search(""); setEval2Open(false); }}
                          className="w-full text-left px-3 py-2.5 text-sm border-b border-slate-100 last:border-b-0 hover:bg-primary/5 transition-colors">
                          <div className="font-medium text-slate-900">{e.fullName}</div>
                          <div className="text-xs text-slate-500">{e.email} · {e.activeAssignmentCount} đề tài đang thẩm định</div>
                        </button>
                      ))}
                    </div>
                  ) : eval2Search.trim() ? (
                    <div className="absolute z-20 mt-1 w-full bg-white border border-slate-200 rounded-lg shadow-lg p-3 text-sm text-slate-500 text-center">Không tìm thấy</div>
                  ) : null;
                })()}
                {(needsBoth ? selectedEval2 : selectedEval1) && (
                  <p className="mt-1 text-xs text-slate-500">{(needsBoth ? selectedEval2 : selectedEval1)!.email} · {(needsBoth ? selectedEval2 : selectedEval1)!.activeAssignmentCount} đề tài đang thẩm định</p>
                )}
              </div>
            </div>

            {/* Footer */}
            <div className="px-6 py-4 border-t border-slate-200 flex justify-end gap-3">
              <button
                onClick={onClose}
                className="px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 rounded-lg transition-colors"
              >
                Hủy
              </button>
              <button
                onClick={handleSubmit}
                disabled={submitting}
                className="flex items-center gap-2 px-4 py-2 bg-primary text-white text-sm font-medium rounded-lg hover:bg-primary/90 transition-colors disabled:opacity-50"
              >
                {submitting ? (
                  <>
                    <span className="material-symbols-outlined text-[18px] animate-spin">progress_activity</span>
                    Đang phân công...
                  </>
                ) : (
                  <>
                    <span className="material-symbols-outlined text-[18px]">person_add</span>
                    Phân công
                  </>
                )}
              </button>
            </div>
          </>
        )}
      </motion.div>
    </motion.div>
  );
}

// ── Final Decision Modal ─────────────────────────────────────────────────────

function FinalDecisionModal({
  project,
  onClose,
  onSuccess,
}: {
  project: DepartmentProject;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [result, setResult] = useState<number | null>(null);
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const decisions = [
    { value: 1, label: "Duyệt", icon: "check_circle", color: "border-green-300 bg-green-50 text-green-700" },
    { value: 2, label: "Yêu cầu chỉnh sửa", icon: "edit_note", color: "border-amber-300 bg-amber-50 text-amber-700" },
    { value: 3, label: "Từ chối", icon: "cancel", color: "border-red-300 bg-red-50 text-red-700" },
  ];

  const handleSubmit = async () => {
    if (result === null) {
      setError("Vui lòng chọn quyết định");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await departmentHeadService.submitFinalDecision(project.projectId, result, notes || undefined);
      setSuccess(true);
      setTimeout(onSuccess, 1500);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Gửi quyết định thất bại");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <motion.div
        initial={{ opacity: 0, scale: 0.95, y: 20 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.95, y: 20 }}
        className="bg-white rounded-2xl shadow-2xl w-full max-w-lg"
        onClick={(e) => e.stopPropagation()}
      >
        {success ? (
          <div className="p-8 text-center">
            <div className="size-16 mx-auto rounded-full bg-green-50 flex items-center justify-center mb-4">
              <span className="material-symbols-outlined text-4xl text-green-500">check_circle</span>
            </div>
            <h3 className="text-lg font-bold text-slate-900 mb-2">Quyết định đã được gửi!</h3>
            <p className="text-sm text-slate-500">Thông báo sẽ được gửi đến tất cả các bên liên quan.</p>
          </div>
        ) : (
          <>
            {/* Header */}
            <div className="px-6 py-4 border-b border-slate-200">
              <div className="flex items-center justify-between">
                <h2 className="text-lg font-bold text-slate-900">Quyết định cuối cùng</h2>
                <button onClick={onClose} className="p-1 hover:bg-slate-100 rounded-lg transition-colors">
                  <span className="material-symbols-outlined text-slate-400">close</span>
                </button>
              </div>
              <p className="text-sm text-slate-500 mt-1">{project.nameVi}</p>
            </div>

            {/* Evaluator results summary */}
            <div className="px-6 pt-4">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Kết quả thẩm định</p>
              <div className="grid grid-cols-2 gap-3">
                {project.evaluators.map((a) => (
                  <div key={a.assignmentId} className="p-3 bg-slate-50 rounded-lg border border-slate-200">
                    <p className="text-sm font-medium text-slate-700">{a.evaluatorName}</p>
                    <p className={`text-sm font-semibold ${resultColor(a.individualResult)}`}>{resultLabel(a.individualResult)}</p>
                    {a.feedback && <p className="text-xs text-slate-500 mt-1 line-clamp-2">{a.feedback}</p>}
                  </div>
                ))}
              </div>
            </div>

            {/* Decision */}
            <div className="p-6 space-y-4">
              {error && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">{error}</div>
              )}

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-2">Quyết định của bạn</label>
                <div className="grid grid-cols-3 gap-2">
                  {decisions.map((d) => (
                    <button
                      key={d.value}
                      onClick={() => setResult(d.value)}
                      className={`flex flex-col items-center gap-1.5 p-3 rounded-lg border-2 transition-all ${
                        result === d.value
                          ? d.color + " ring-2 ring-offset-1 ring-current"
                          : "border-slate-200 hover:border-slate-300 text-slate-600"
                      }`}
                    >
                      <span className="material-symbols-outlined text-[22px]">{d.icon}</span>
                      <span className="text-xs font-medium">{d.label}</span>
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Ghi chú (tùy chọn)</label>
                <textarea
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder="Nhập ghi chú hoặc lý do..."
                  rows={3}
                  className="w-full px-3 py-2.5 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary resize-none"
                />
              </div>
            </div>

            {/* Footer */}
            <div className="px-6 py-4 border-t border-slate-200 flex justify-end gap-3">
              <button
                onClick={onClose}
                className="px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 rounded-lg transition-colors"
              >
                Hủy
              </button>
              <button
                onClick={handleSubmit}
                disabled={submitting || result === null}
                className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white text-sm font-medium rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50"
              >
                {submitting ? (
                  <>
                    <span className="material-symbols-outlined text-[18px] animate-spin">progress_activity</span>
                    Đang gửi...
                  </>
                ) : (
                  <>
                    <span className="material-symbols-outlined text-[18px]">gavel</span>
                    Xác nhận quyết định
                  </>
                )}
              </button>
            </div>
          </>
        )}
      </motion.div>
    </motion.div>
  );
}

// ── Shared Components ────────────────────────────────────────────────────────

function EmptyState({ message, icon }: { message: string; icon: string }) {
  return (
    <div className="flex flex-col items-center gap-3 py-16 text-center">
      <span className="material-symbols-outlined text-5xl text-slate-300">{icon}</span>
      <p className="text-sm font-medium text-slate-400">{message}</p>
    </div>
  );
}

function ErrorState({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <div className="flex flex-col items-center gap-3 py-16 text-center">
      <span className="material-symbols-outlined text-4xl text-red-400">error</span>
      <p className="text-sm text-slate-500">{message}</p>
      <button onClick={onRetry} className="text-sm font-medium text-primary hover:underline">
        Thử lại
      </button>
    </div>
  );
}

function LoadingSkeleton() {
  return (
    <div className="space-y-3">
      {[1, 2, 3].map((i) => (
        <div key={i} className="p-4 rounded-lg border border-slate-100 animate-pulse">
          <div className="flex items-center gap-2 mb-2">
            <div className="h-4 w-20 bg-slate-200 rounded" />
            <div className="h-5 w-24 bg-slate-200 rounded-full" />
          </div>
          <div className="h-5 w-3/4 bg-slate-200 rounded mb-2" />
          <div className="h-4 w-1/2 bg-slate-200 rounded" />
        </div>
      ))}
    </div>
  );
}
