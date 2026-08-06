import { useState, useEffect, useCallback, useMemo } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { evaluatorService, checklistService } from "@/lib";
import type {
  ProjectReviewResponse,
  SimilarityMatchDto,
  TranslatedThesisDto,
  ProjectChecklistResponse,
  ChecklistScoreItemInput,
} from "@/types";
import { useSignalR, type ProjectStatusUpdatedPayload } from "@/hooks/useSignalR";
import { useNotificationTargetRefresh } from "@/hooks/useNotificationTargetRefresh";
import { EvaluationChecklistModal } from "@/components/lecturer";

/** Level bucket (from the DASSF engine) → colours + Vietnamese label for the score pill. */
function levelStyle(level: string): { bg: string; text: string; border: string; ring: string; label: string } {
  switch (level) {
    case "Critical":
      return { bg: "bg-red-50", text: "text-red-600", border: "border-red-200", ring: "ring-red-200", label: "Rất cao" };
    case "High":
      return { bg: "bg-orange-50", text: "text-orange-600", border: "border-orange-200", ring: "ring-orange-200", label: "Cao" };
    case "Moderate":
      return { bg: "bg-amber-50", text: "text-amber-600", border: "border-amber-200", ring: "ring-amber-200", label: "Trung bình" };
    default:
      return { bg: "bg-green-50", text: "text-green-600", border: "border-green-200", ring: "ring-green-200", label: "Thấp" };
  }
}

/** The comparable content fields shown side-by-side. */
type FieldKey = "title" | "description" | "objectives" | "scope" | "technologies" | "expectedResults";
const ALL_FIELDS: FieldKey[] = ["title", "description", "objectives", "scope", "technologies", "expectedResults"];
const FIELD_LABEL: Record<FieldKey, string> = {
  title: "Tên đề tài",
  description: "Mô tả",
  objectives: "Mục tiêu",
  scope: "Phạm vi",
  technologies: "Công nghệ",
  expectedResults: "Kết quả mong đợi",
};

interface ReasonMeta {
  icon: string;
  label: string;
  detail: string;
  cls: string;
  /** <mark> highlight classes for this reason's colour. */
  mark: string;
  /** Which content fields this reason is derived from (so we highlight the right text). */
  fields: FieldKey[];
}

/**
 * Maps each DASSF reason phrase (produced by the Python score_calculator) to an icon, a
 * Vietnamese label, an explanation, and — crucially — the content fields it draws on, so
 * clicking a reason highlights exactly the overlapping text behind it.
 */
const REASON_CATALOG: Record<string, ReasonMeta> = {
  "same tech stack with a different business domain": {
    icon: "layers",
    label: "Cùng công nghệ · khác lĩnh vực",
    detail: "Trùng ngăn xếp công nghệ / kiến trúc nhưng khác lĩnh vực nghiệp vụ — dấu hiệu “sao chép cấu trúc”.",
    cls: "bg-amber-50 text-amber-700 border-amber-200",
    mark: "bg-amber-200/70 text-amber-900",
    fields: ["technologies", "scope"],
  },
  "same business domain": {
    icon: "domain",
    label: "Cùng lĩnh vực nghiệp vụ",
    detail: "Hai đề tài cùng giải quyết một lĩnh vực / bài toán nghiệp vụ.",
    cls: "bg-purple-50 text-purple-700 border-purple-200",
    mark: "bg-purple-200/70 text-purple-900",
    fields: ["description", "objectives"],
  },
  "similar architecture or scope": {
    icon: "architecture",
    label: "Kiến trúc · phạm vi tương tự",
    detail: "Cùng kiểu kiến trúc, công nghệ hoặc phạm vi triển khai.",
    cls: "bg-blue-50 text-blue-700 border-blue-200",
    mark: "bg-blue-200/70 text-blue-900",
    fields: ["scope", "description", "technologies"],
  },
  "shared weighted terms across fields": {
    icon: "match_word",
    label: "Trùng nhiều thuật ngữ trọng số",
    detail: "Nhiều từ khoá quan trọng (TF-IDF) xuất hiện ở cả hai đề tài.",
    cls: "bg-teal-50 text-teal-700 border-teal-200",
    mark: "bg-teal-200/70 text-teal-900",
    fields: ALL_FIELDS,
  },
  "similar semantic content": {
    icon: "psychology",
    label: "Ngữ nghĩa tương đồng",
    detail: "Nội dung tổng thể của 5 trường mô tả tương đồng.",
    cls: "bg-indigo-50 text-indigo-700 border-indigo-200",
    mark: "bg-indigo-200/70 text-indigo-900",
    fields: ALL_FIELDS,
  },
};

const pct = (score: number) => Math.round(score * 100);

// ── Token overlap + highlighting ────────────────────────────────────────────────
// Words to ignore when computing overlap — generic EN/VN terms that add noise, not signal.
const STOPWORDS = new Set<string>([
  "the", "and", "for", "with", "using", "use", "from", "are", "based", "into",
  "system", "application", "app", "web", "website", "platform", "management", "managing",
  "build", "building", "develop", "developing", "development", "project", "projects", "data",
  "user", "users", "support", "supporting", "online", "service", "services", "software",
  "và", "cho", "các", "một", "của", "với", "hệ", "thống", "xây", "dựng", "sử", "dụng",
  "ứng", "quản", "lý", "dữ", "liệu", "người", "dùng", "trên", "theo", "được", "có",
  "tại", "đề", "tài", "phần", "mềm", "nền", "tảng", "hỗ", "trợ", "giải", "pháp", "thông", "tin",
]);

const WORD_RE = /[\p{L}\p{N}][\p{L}\p{N}+#]*/gu;

function tokenize(text: string | null | undefined): Set<string> {
  const out = new Set<string>();
  for (const m of (text ?? "").toLowerCase().matchAll(WORD_RE)) {
    if (m[0].length >= 3 && !STOPWORDS.has(m[0])) out.add(m[0]);
  }
  return out;
}

/** Significant tokens appearing in BOTH texts — the concrete overlap that drives similarity. */
function sharedTokens(a: string | null | undefined, b: string | null | undefined): Set<string> {
  const bs = tokenize(b);
  const shared = new Set<string>();
  for (const t of tokenize(a)) if (bs.has(t)) shared.add(t);
  return shared;
}

/** Renders text with the shared tokens wrapped in a coloured <mark>. */
function HighlightedText({
  text,
  tokens,
  markCls,
}: Readonly<{ text: string | null | undefined; tokens: Set<string>; markCls: string }>) {
  if (!text) return <span className="text-slate-400">—</span>;
  if (tokens.size === 0) return <>{text}</>;

  const segments = text.split(/([\p{L}\p{N}+#]+)/gu);
  return (
    <>
      {segments.map((seg, i) =>
        tokens.has(seg.toLowerCase()) ? (
          <mark key={i} className={`rounded px-0.5 ${markCls}`}>
            {seg}
          </mark>
        ) : (
          <span key={i}>{seg}</span>
        ),
      )}
    </>
  );
}

/** Why the "Duyệt" verdict is blocked — shared by the submit guard and the button tooltip. */
function approveGateMessage(hasActiveConfig: boolean, required: number, total: number): string {
  return hasActiveConfig
    ? `Đề tài cần đạt ít nhất ${required}/${total} tiêu chí thẩm định trước khi được duyệt.`
    : "Học kỳ này chưa được cấu hình checklist thẩm định. Vui lòng liên hệ Trưởng bộ môn.";
}

/** Trailing badge of the checklist button: spinner / passed-count / not-configured warning. */
function ChecklistBadge({
  loading,
  checklist,
  canApprove,
  total,
}: Readonly<{
  loading: boolean;
  checklist: ProjectChecklistResponse | null;
  canApprove: boolean;
  total: number;
}>) {
  if (loading) {
    return (
      <span className="material-symbols-outlined animate-spin text-[18px] text-slate-400">progress_activity</span>
    );
  }

  if (checklist?.hasActiveConfig) {
    return (
      <span
        className={`rounded-full px-2 py-0.5 text-xs font-bold ${
          canApprove ? "bg-green-100 text-green-600" : "bg-amber-100 text-amber-700"
        }`}
      >
        {checklist.passedCount}/{total}
      </span>
    );
  }

  return <span className="material-symbols-outlined text-[18px] text-amber-500">report</span>;
}

export function LecturerReviewPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { showError } = useSystemError();

  const [project, setProject] = useState<ProjectReviewResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [verdict, setVerdict] = useState<number | null>(null);
  const [feedback, setFeedback] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);

  // Similarity state
  const [matches, setMatches] = useState<SimilarityMatchDto[]>([]);
  const [showSimilarity, setShowSimilarity] = useState(false);
  const [loadingSimilarity, setLoadingSimilarity] = useState(false);

  // Evaluation checklist state (gates the "Duyệt" verdict on the backend and here).
  const [checklist, setChecklist] = useState<ProjectChecklistResponse | null>(null);
  const [checklistOpen, setChecklistOpen] = useState(false);
  const [checklistLoading, setChecklistLoading] = useState(false);

  const fetchProjectForReview = useCallback(() => {
    if (!id) return;
    setLoading(true);
    evaluatorService
      .getProjectForReview(id)
      .then((project) => {
        setProject(project);
        if (project.existingFeedback) setFeedback(project.existingFeedback);
        if (project.existingResult) {
          const resultMap: Record<string, number> = {
            Approved: 1,
            NeedsModification: 2,
            Rejected: 3,
          };
          setVerdict(resultMap[project.existingResult] ?? null);
        }
      })
      .catch(() => showError("Không thể tải thông tin đề tài. Vui lòng thử lại sau."))
      .finally(() => setLoading(false));
  }, [id, showError]);

  useEffect(() => {
    fetchProjectForReview();
  }, [fetchProjectForReview]);

  const fetchChecklist = useCallback(() => {
    if (!id) return;
    setChecklistLoading(true);
    checklistService
      .getProjectChecklist(id)
      .then(setChecklist)
      // 403/404 (e.g. not the assigned evaluator) simply leaves the gate closed.
      .catch(() => setChecklist(null))
      .finally(() => setChecklistLoading(false));
  }, [id]);

  useEffect(() => {
    fetchChecklist();
  }, [fetchChecklist]);

  const handleSaveChecklist = useCallback(
    async (items: ChecklistScoreItemInput[], note: string) => {
      if (!id) return;
      await checklistService.saveProjectChecklist(id, { items, note });
      fetchChecklist();
    },
    [id, fetchChecklist],
  );

  // Real-time: when a checklist is saved for this project, reload the official data from the API.
  const handleChecklistUpdated = useCallback(
    (payload: { projectId: string }) => {
      if (payload.projectId === id) fetchChecklist();
    },
    [id, fetchChecklist],
  );

  // Clicking a "Phân công thẩm định" notification while already on this exact
  // project's page doesn't trigger a route change, so refetch on the refresh event.
  useNotificationTargetRefresh(fetchProjectForReview);

  const handleProjectStatusUpdated = useCallback(
    (payload: ProjectStatusUpdatedPayload) => {
      if (payload.projectId !== id) return;
      evaluatorService.getProjectForReview(payload.projectId).then(setProject).catch(() => {
        /* silently ignore — UI keeps last known state */
      });
    },
    [id],
  );

  const { joinProjectChannel, leaveProjectChannel } = useSignalR({
    onProjectStatusUpdated: handleProjectStatusUpdated,
    onChecklistUpdated: handleChecklistUpdated,
  });

  useEffect(() => {
    if (!id) return;
    joinProjectChannel(id);
    return () => leaveProjectChannel(id);
  }, [id, joinProjectChannel, leaveProjectChannel]);

  const handleCheckSimilarity = useCallback(async () => {
    if (!id) return;
    setLoadingSimilarity(true);
    setShowSimilarity(true);
    try {
      const result = await evaluatorService.checkSimilarity(id);
      setMatches(result);
    } catch {
      showError("Không thể kiểm tra trùng lặp. Vui lòng thử lại sau.");
    } finally {
      setLoadingSimilarity(false);
    }
  }, [id, showError]);

  const checklistRequired = checklist?.requiredPassCount ?? 0;
  const checklistTotal = checklist?.totalCriteria ?? 0;
  const canApprove = checklist?.canApprove ?? false;
  const hasActiveConfig = checklist?.hasActiveConfig ?? false;

  const handleSubmit = useCallback(async () => {
    if (!id || verdict === null) return;

    // Server-side is authoritative, but block here too for a clear, immediate message.
    if (verdict === 1 && !canApprove) {
      showError(approveGateMessage(hasActiveConfig, checklistRequired, checklistTotal));
      return;
    }

    setSubmitting(true);
    try {
      await evaluatorService.submitEvaluation(id, {
        result: verdict,
        feedback: feedback || undefined,
      });
      setShowSuccess(true);
      setTimeout(() => navigate("/lecturer/moderate"), 2000);
    } catch {
      showError("Không thể gửi thẩm định. Vui lòng thử lại sau.");
    } finally {
      setSubmitting(false);
    }
  }, [id, verdict, feedback, navigate, showError, canApprove, hasActiveConfig, checklistRequired, checklistTotal]);

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <div className="size-10 animate-spin rounded-full border-4 border-primary border-t-transparent" />
          <p className="text-sm text-slate-500">Đang tải thông tin đề tài...</p>
        </div>
      </div>
    );
  }

  if (!project) {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="text-center">
          <span className="material-symbols-outlined text-6xl text-gray-300 mb-4 block">error</span>
          <p className="text-lg font-semibold text-slate-700">Không tìm thấy đề tài</p>
          <button type="button"
            onClick={() => navigate("/lecturer/moderate")}
            className="mt-4 px-4 py-2 rounded-lg bg-primary text-white text-sm font-semibold hover:bg-primary-dark"
          >
            Quay lại Dashboard
          </button>
        </div>
      </div>
    );
  }

  const verdictOptions = [
    { value: 1, label: "Duyệt", color: "green", icon: "check_circle" },
    { value: 2, label: "Chỉnh sửa", color: "amber", icon: "edit_note" },
    { value: 3, label: "Từ chối", color: "red", icon: "cancel" },
  ];

  const quickFeedback = [
    "Đề tài có tính ứng dụng cao.",
    "Cần bổ sung phương pháp nghiên cứu.",
    "Mở rộng phần tổng quan tài liệu.",
    "Cấu trúc đề tài tốt.",
    "Mục tiêu chưa rõ ràng, cần cụ thể hơn.",
    "Phạm vi quá rộng, cần thu hẹp.",
  ];

  return (
    <div className="flex h-full flex-col lg:flex-row">
      {/* Main Content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        <motion.header
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-white border-b border-gray-200 px-6 py-4 shrink-0"
        >
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-center gap-4">
              <button type="button"
                onClick={() => navigate("/lecturer/moderate")}
                className="size-10 rounded-xl border border-gray-200 flex items-center justify-center hover:bg-gray-50"
              >
                <span className="material-symbols-outlined text-slate-500">arrow_back</span>
              </button>
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-xs font-mono font-bold text-slate-500 bg-gray-100 px-2 py-0.5 rounded">
                    #{project.projectCode}
                  </span>
                  {project.existingResult ? (
                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-green-50 text-green-600 border border-green-100">
                      Đã thẩm định
                    </span>
                  ) : (
                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-blue-50 text-blue-600 border border-blue-100">
                      <span className="size-1.5 rounded-full bg-blue-500 animate-pulse" />
                      Đang thẩm định
                    </span>
                  )}
                  {project.daysElapsed > 5 && !project.existingResult && (
                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-red-50 text-red-600 border border-red-100">
                      <span className="material-symbols-outlined text-[12px]">warning</span>
                      {project.daysElapsed} ngày
                    </span>
                  )}
                </div>
                <h1 className="text-lg font-bold text-slate-900 mt-1">{project.nameVi}</h1>
                <p className="text-xs text-slate-500 mt-0.5">
                  <span className="font-medium">{project.studentName || "Chưa có sinh viên"}</span>
                  {" • "}{project.majorName}
                  {" • "}GVHD: {project.mentorName || "Chưa có"}
                </p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <button type="button"
                onClick={handleCheckSimilarity}
                disabled={loadingSimilarity}
                className="flex items-center gap-2 h-10 px-4 rounded-lg border border-gray-200 bg-white text-slate-700 text-sm font-semibold hover:bg-gray-50 disabled:opacity-50"
              >
                {loadingSimilarity ? (
                  <div className="size-4 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                ) : (
                  <span className="material-symbols-outlined text-[20px]">compare</span>
                )}
                Kiểm tra trùng lặp
              </button>
            </div>
          </div>
        </motion.header>

        {/* Scrollable content */}
        <div className="flex-1 overflow-y-auto bg-gray-50 p-6">
          <div className="mx-auto w-full max-w-[1700px] space-y-6">
            {/* English Title */}
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              className="bg-white rounded-xl border border-gray-200 p-5"
            >
              <h3 className="text-xs font-bold text-slate-400 uppercase mb-2">Tên tiếng Anh</h3>
              <p className="text-sm text-slate-800 font-medium">{project.nameEn}</p>
              {project.nameAbbr && (
                <p className="text-xs text-slate-500 mt-1">Viết tắt: {project.nameAbbr}</p>
              )}
            </motion.div>

            {/* Description + Objectives */}
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.05 }}
              className="bg-white rounded-xl border border-gray-200 p-5"
            >
              <div className="grid md:grid-cols-2 gap-6">
                <div>
                  <h3 className="text-xs font-bold text-slate-400 uppercase mb-2">Mô tả</h3>
                  <p className="text-sm text-slate-700 whitespace-pre-line">{project.description}</p>
                </div>
                <div>
                  <h3 className="text-xs font-bold text-slate-400 uppercase mb-2">Mục tiêu</h3>
                  <p className="text-sm text-slate-700 whitespace-pre-line">{project.objectives}</p>
                </div>
              </div>
            </motion.div>

            {/* Scope + Technologies + Expected Results */}
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.1 }}
              className="bg-white rounded-xl border border-gray-200 p-5"
            >
              <div className="grid md:grid-cols-3 gap-6">
                {project.scope && (
                  <div>
                    <h3 className="text-xs font-bold text-slate-400 uppercase mb-2">Phạm vi</h3>
                    <p className="text-sm text-slate-700 whitespace-pre-line">{project.scope}</p>
                  </div>
                )}
                {project.technologies && (
                  <div>
                    <h3 className="text-xs font-bold text-slate-400 uppercase mb-2">Công nghệ</h3>
                    <div className="flex flex-wrap gap-1.5">
                      {project.technologies.split(",").map((tech) => (
                        <span
                          key={tech.trim()}
                          className="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-50 text-blue-700"
                        >
                          {tech.trim()}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
                {project.expectedResults && (
                  <div>
                    <h3 className="text-xs font-bold text-slate-400 uppercase mb-2">Kết quả mong đợi</h3>
                    <p className="text-sm text-slate-700 whitespace-pre-line">{project.expectedResults}</p>
                  </div>
                )}
              </div>
            </motion.div>

            {/* Meta info */}
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.15 }}
              className="bg-white rounded-xl border border-gray-200 p-5"
            >
              <h3 className="text-xs font-bold text-slate-400 uppercase mb-3">Thông tin chung</h3>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                <div>
                  <span className="text-slate-400 text-xs">Học kì</span>
                  <p className="font-medium text-slate-800">{project.semesterName}</p>
                </div>
                <div>
                  <span className="text-slate-400 text-xs">Ngành</span>
                  <p className="font-medium text-slate-800">{project.majorName}</p>
                </div>
                <div>
                  <span className="text-slate-400 text-xs">SV tối đa</span>
                  <p className="font-medium text-slate-800">{project.maxStudents}</p>
                </div>
                <div>
                  <span className="text-slate-400 text-xs">Lần thẩm định</span>
                  <p className="font-medium text-slate-800">{project.evaluationCount}</p>
                </div>
              </div>
            </motion.div>

            {/* Similarity Results (inline) — DASSF overall score + reasons */}
            <AnimatePresence>
              {showSimilarity && (
                <motion.div
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: "auto" }}
                  exit={{ opacity: 0, height: 0 }}
                  className="overflow-hidden"
                >
                  <div className="bg-white rounded-xl border border-gray-200 p-5">
                    <div className="flex items-center justify-between mb-4">
                      <div className="flex items-center gap-2">
                        <span className="material-symbols-outlined text-amber-500">compare</span>
                        <h3 className="text-sm font-bold text-slate-900">Kết quả kiểm tra trùng lặp (AI)</h3>
                      </div>
                      <button type="button"
                        onClick={() => setShowSimilarity(false)}
                        className="size-7 rounded-lg hover:bg-gray-100 flex items-center justify-center"
                      >
                        <span className="material-symbols-outlined text-slate-400 text-[18px]">close</span>
                      </button>
                    </div>

                    {loadingSimilarity ? (
                      <div className="flex items-center justify-center py-8">
                        <div className="size-8 animate-spin rounded-full border-3 border-primary border-t-transparent" />
                        <span className="ml-3 text-sm text-slate-500">Đang phân tích...</span>
                      </div>
                    ) : matches.length === 0 ? (
                      <div className="text-center py-8">
                        <span className="material-symbols-outlined text-4xl text-green-400 mb-2 block">verified</span>
                        <p className="text-sm font-medium text-green-600">Không tìm thấy đề tài trùng lặp đáng kể</p>
                        <p className="text-xs text-slate-500 mt-1">
                          Hệ thống AI không phát hiện đề tài nào tương đồng đủ cao với đề tài này.
                        </p>
                      </div>
                    ) : (
                      <SimilarityResults matches={matches} project={project} />
                    )}
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </div>
      </div>

      {/* Right Sidebar */}
      <motion.aside
        initial={{ opacity: 0, x: 100 }}
        animate={{ opacity: 1, x: 0 }}
        className="w-full lg:w-[380px] bg-white border-l border-gray-200 flex flex-col shrink-0"
      >
        <div className="px-6 py-5 border-b border-gray-200">
          <h2 className="text-lg font-bold text-slate-900 flex items-center gap-2">
            <span className="material-symbols-outlined text-primary">rate_review</span>
            Thẩm định đề tài
          </h2>
          <p className="text-xs text-slate-500 mt-1">Đưa ra quyết định và phản hồi</p>
        </div>

        <div className="flex-1 overflow-y-auto p-6 flex flex-col gap-6">
          {/* Verdict */}
          <div>
            <h3 className="text-xs font-bold text-slate-500 uppercase mb-3">Quyết định</h3>
            <div className="grid grid-cols-3 gap-2">
              {verdictOptions.map(({ value, label, color, icon }) => {
                const selected = verdict === value;
                // "Duyệt" (value 1) is gated by the checklist threshold.
                const approveGated = value === 1 && !canApprove;
                const disabled = !!project.existingResult || approveGated;
                return (
                  <button type="button"
                    key={value}
                    onClick={() => setVerdict(value)}
                    disabled={disabled}
                    title={
                      approveGated
                        ? approveGateMessage(hasActiveConfig, checklistRequired, checklistTotal)
                        : undefined
                    }
                    className={`flex flex-col items-center justify-center p-4 rounded-xl border-2 transition-all disabled:opacity-60 disabled:cursor-not-allowed ${
                      selected
                        ? `border-${color}-500 bg-${color}-50`
                        : `border-gray-200 hover:border-${color}-300`
                    }`}
                  >
                    <span
                      className={`material-symbols-outlined text-2xl mb-1 ${
                        selected ? `text-${color}-600` : "text-gray-400"
                      }`}
                    >
                      {icon}
                    </span>
                    <span
                      className={`text-xs font-bold ${
                        selected ? `text-${color}-600` : "text-slate-500"
                      }`}
                    >
                      {label}
                    </span>
                  </button>
                );
              })}
            </div>

            {/* Checklist button + gate hint */}
            <button type="button"
              onClick={() => setChecklistOpen(true)}
              className="mt-3 flex w-full items-center justify-between rounded-xl border border-gray-200 px-4 py-3 text-sm font-semibold text-slate-700 transition-colors hover:bg-gray-50"
            >
              <span className="flex items-center gap-2">
                <span className="material-symbols-outlined text-[20px] text-primary">checklist</span>{" "}
                Checklist thẩm định
              </span>
              <ChecklistBadge
                loading={checklistLoading}
                checklist={checklist}
                canApprove={canApprove}
                total={checklistTotal}
              />
            </button>
            {!checklistLoading && checklist && !hasActiveConfig && (
              <p className="mt-2 text-xs text-amber-600">
                Học kỳ này chưa được cấu hình checklist thẩm định. Vui lòng liên hệ Trưởng bộ môn.
              </p>
            )}
            {!checklistLoading && hasActiveConfig && !canApprove && !project.existingResult && (
              <p className="mt-2 text-xs text-amber-600">
                Đề tài cần đạt ít nhất {checklistRequired}/{checklistTotal} tiêu chí thẩm định trước khi được duyệt.
              </p>
            )}
          </div>

          {/* Feedback */}
          <div>
            <h3 className="text-xs font-bold text-slate-500 uppercase mb-3">Phản hồi</h3>
            <textarea
              value={feedback}
              onChange={(e) => setFeedback(e.target.value)}
              disabled={!!project.existingResult}
              className="w-full h-32 px-4 py-3 rounded-xl border border-gray-200 bg-gray-50 text-sm resize-none focus:ring-2 focus:ring-primary/20 focus:border-primary focus:bg-white outline-none disabled:opacity-60"
              placeholder="Nhập phản hồi cho đề tài..."
            />
          </div>

          {/* Quick feedback */}
          {!project.existingResult && (
            <div>
              <h3 className="text-xs font-bold text-slate-500 uppercase mb-3">Mẫu nhanh</h3>
              <div className="flex flex-wrap gap-2">
                {quickFeedback.map((t) => (
                  <button type="button"
                    key={t}
                    onClick={() => setFeedback((f) => (f ? f + " " : "") + t)}
                    className="px-3 py-1.5 rounded-full text-xs font-medium bg-gray-100 text-slate-600 hover:bg-primary/10 hover:text-primary"
                  >
                    {t}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Assignment info */}
          <div>
            <h3 className="text-xs font-bold text-slate-500 uppercase mb-3">Thông tin phân công</h3>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-slate-500">Ngày phân công</span>
                <span className="font-medium text-slate-800">
                  {new Date(project.assignedAt).toLocaleDateString("vi-VN")}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Số ngày đã qua</span>
                <span className={`font-medium ${project.daysElapsed > 5 ? "text-red-600" : "text-slate-800"}`}>
                  {project.daysElapsed} ngày
                </span>
              </div>
              {project.submittedAt && (
                <div className="flex justify-between">
                  <span className="text-slate-500">Ngày nộp đề tài</span>
                  <span className="font-medium text-slate-800">
                    {new Date(project.submittedAt).toLocaleDateString("vi-VN")}
                  </span>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Footer buttons */}
        {!project.existingResult && (
          <div className="px-6 py-4 border-t border-gray-200 flex gap-3">
            <button type="button"
              onClick={() => navigate("/lecturer/moderate")}
              className="flex-1 h-11 rounded-xl border border-gray-200 text-slate-700 font-semibold text-sm hover:bg-gray-50"
            >
              Quay lại
            </button>
            <button type="button"
              disabled={
                verdict === null || submitting || !!project.existingResult || (verdict === 1 && !canApprove)
              }
              onClick={handleSubmit}
              className="flex-1 h-11 rounded-xl bg-primary text-white font-semibold text-sm hover:bg-primary-dark shadow-lg shadow-primary/20 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
            >
              {submitting ? (
                <>
                  <div className="size-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                  Đang gửi...
                </>
              ) : (
                "Gửi thẩm định"
              )}
            </button>
          </div>
        )}
      </motion.aside>

      {/* Evaluation checklist modal */}
      <EvaluationChecklistModal
        open={checklistOpen}
        loading={checklistLoading}
        readOnly={!!project.existingResult}
        checklist={checklist}
        onClose={() => setChecklistOpen(false)}
        onSave={handleSaveChecklist}
      />

      {/* Success Modal */}
      <AnimatePresence>
        {showSuccess && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4"
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.9 }}
              className="bg-white rounded-2xl shadow-2xl w-full max-w-sm p-8 text-center"
            >
              <div className="size-16 mx-auto rounded-full bg-green-50 flex items-center justify-center mb-4">
                <span className="material-symbols-outlined text-4xl text-green-500">check_circle</span>
              </div>
              <h3 className="text-lg font-bold text-slate-900 mb-2">Thẩm định thành công!</h3>
              <p className="text-sm text-slate-500">
                Kết quả thẩm định đã được lưu. Đang chuyển về trang dashboard...
              </p>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

// ── Similarity sub-components ────────────────────────────────────────────────────

/** The whole similarity panel body: a headline + the top-5 side-by-side comparisons. */
function SimilarityResults({
  matches,
  project,
}: Readonly<{ matches: SimilarityMatchDto[]; project: ProjectReviewResponse }>) {
  const top = matches[0];
  const topStyle = levelStyle(top.level);
  const shown = matches.slice(0, 5);

  return (
    <div className="space-y-4">
      {/* Headline: highest match */}
      <div className={`flex items-center gap-4 rounded-xl border p-4 ${topStyle.bg} ${topStyle.border}`}>
        <ScoreDial score={top.overallScore} level={top.level} />
        <div className="min-w-0">
          <p className="text-[11px] font-bold uppercase tracking-wide text-slate-500">Mức độ trùng lặp cao nhất</p>
          <p className={`text-lg font-extrabold ${topStyle.text}`}>
            {pct(top.overallScore)}% · {topStyle.label}
          </p>
          <p className="text-xs text-slate-500 mt-0.5">
            Top {shown.length} trên {matches.length} đề tài tương đồng — bấm một{" "}
            <span className="font-semibold">lý do</span> để tô sáng phần trùng tương ứng ở hai cột.
          </p>
        </div>
      </div>

      {shown.map((m, idx) => (
        <MatchComparison key={m.otherThesisId} match={m} project={project} index={idx} />
      ))}
    </div>
  );
}

/** One matched topic shown side-by-side with the one under review, with per-reason highlighting. */
function MatchComparison({
  match,
  project,
  index,
}: Readonly<{ match: SimilarityMatchDto; project: ProjectReviewResponse; index: number }>) {
  const s = levelStyle(match.level);
  const [activeReason, setActiveReason] = useState<string | null>(null);
  const [translated, setTranslated] = useState<TranslatedThesisDto | null>(null);
  const [translating, setTranslating] = useState(false);
  const [showTranslated, setShowTranslated] = useState(false);
  const [translateError, setTranslateError] = useState(false);

  const isVi = showTranslated && translated != null;

  const current: Record<FieldKey, string | null> = {
    title: project.nameEn || project.nameVi,
    description: project.description,
    objectives: project.objectives,
    scope: project.scope,
    technologies: project.technologies,
    expectedResults: project.expectedResults,
  };
  const other: Record<FieldKey, string | null> = isVi
    ? {
        title: translated.title,
        description: translated.description,
        objectives: translated.objectives,
        scope: translated.scope,
        technologies: translated.technologies.length > 0 ? translated.technologies.join(", ") : null,
        expectedResults: translated.expectedResult,
      }
    : {
        title: match.title,
        description: match.description,
        objectives: match.objectives,
        scope: match.scope,
        technologies: match.technologies.length > 0 ? match.technologies.join(", ") : null,
        expectedResults: match.expectedResult,
      };

  // Shared tokens per field — recomputed when the match changes or we switch VI/EN. Translating
  // the matched topic to Vietnamese lets its domain terms overlap with the Vietnamese project.
  const sharedByField = useMemo(() => {
    const out = {} as Record<FieldKey, Set<string>>;
    for (const f of ALL_FIELDS) out[f] = sharedTokens(current[f], other[f]);
    return out;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [match.otherThesisId, isVi]);

  const handleTranslate = async () => {
    if (translated) {
      setShowTranslated((v) => !v);
      return;
    }
    setTranslating(true);
    setTranslateError(false);
    try {
      const result = await evaluatorService.translateThesis(match.otherThesisId);
      setTranslated(result);
      setShowTranslated(true);
      if (!result.translated) setTranslateError(true);
    } catch {
      setTranslateError(true);
    } finally {
      setTranslating(false);
    }
  };

  const activeMeta = activeReason ? REASON_CATALOG[activeReason] : null;
  // Default (no reason selected) highlights every field's overlap in a neutral colour.
  const highlightFields = new Set<FieldKey>(activeMeta ? activeMeta.fields : ALL_FIELDS);
  const markCls = activeMeta ? activeMeta.mark : "bg-amber-200/70 text-amber-900";

  return (
    <div className="rounded-xl border border-gray-200 overflow-hidden">
      {/* Header: score + reason chips (toggle highlight) */}
      <div className="flex flex-wrap items-center justify-between gap-3 px-4 py-3 bg-slate-50 border-b border-gray-200">
        <div className="flex items-center gap-2.5">
          <span className="text-[10px] font-mono text-slate-400">Kết quả #{index + 1}</span>
          <span className={`text-xl font-extrabold ${s.text}`}>{pct(match.overallScore)}%</span>
          <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold border ${s.bg} ${s.text} ${s.border}`}>
            {s.label}
          </span>
          {match.semester && <span className="text-[10px] text-slate-400">{match.semester}</span>}
        </div>
        <div className="flex flex-wrap items-center gap-1.5">
          <button
            type="button"
            onClick={handleTranslate}
            disabled={translating}
            title="Dịch đề tài trùng sang tiếng Việt để đối chiếu và tô sáng phần trùng theo lĩnh vực/ngữ nghĩa"
            className="inline-flex items-center gap-1 rounded-full border border-slate-300 bg-white px-2.5 py-1 text-[11px] font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-50"
          >
            <span className={`material-symbols-outlined text-[14px] ${translating ? "animate-spin" : ""}`}>
              {translating ? "progress_activity" : "translate"}
            </span>
            {translating ? "Đang dịch..." : translated ? (isVi ? "Xem bản gốc (EN)" : "Xem bản dịch (VI)") : "Dịch sang tiếng Việt"}
          </button>
          {match.reasons.length === 0 && <span className="text-[11px] text-slate-400">Không có lý do chi tiết</span>}
          {match.reasons.map((r) => {
            const meta = REASON_CATALOG[r];
            const on = activeReason === r;
            return (
              <button
                key={r}
                type="button"
                title={meta?.detail ?? r}
                onClick={() => setActiveReason(on ? null : r)}
                className={`inline-flex items-center gap-1 rounded-full border px-2.5 py-1 text-[11px] font-semibold transition-all ${
                  meta?.cls ?? "bg-slate-50 text-slate-600 border-slate-200"
                } ${on ? "ring-2 ring-offset-1 ring-primary/50" : "hover:brightness-95"}`}
              >
                {meta && <span className="material-symbols-outlined text-[14px]">{meta.icon}</span>}
                {meta?.label ?? r}
              </button>
            );
          })}
        </div>
      </div>

      {/* Active-reason hint */}
      {activeMeta && (
        <div className="px-4 py-2 text-[11px] text-slate-600 bg-white border-b border-gray-100">
          <span className="font-semibold">{activeMeta.label}:</span> {activeMeta.detail} Đã tô sáng phần trùng ở{" "}
          <span className="font-semibold">{activeMeta.fields.map((f) => FIELD_LABEL[f]).join(", ")}</span>.
        </div>
      )}

      {/* Translation status */}
      {translateError && (
        <div className="px-4 py-2 text-[11px] text-amber-700 bg-amber-50 border-b border-amber-100">
          Không dịch được (dịch vụ AI chưa sẵn sàng) — đang hiển thị bản gốc tiếng Anh.
        </div>
      )}
      {isVi && !translateError && (
        <div className="px-4 py-2 text-[11px] text-emerald-700 bg-emerald-50 border-b border-emerald-100">
          Đã dịch đề tài trùng sang tiếng Việt — phần trùng về lĩnh vực/ngữ nghĩa giờ được tô sáng chính xác hơn.
        </div>
      )}

      {/* Two columns */}
      <div className="grid grid-cols-1 lg:grid-cols-2 divide-y lg:divide-y-0 lg:divide-x divide-gray-100">
        <ThesisColumn
          heading="Đề tài đang thẩm định"
          tone="blue"
          content={current}
          shared={sharedByField}
          highlightFields={highlightFields}
          markCls={markCls}
        />
        <ThesisColumn
          heading={isVi ? "Đề tài trùng · đã dịch (VI)" : "Đề tài trùng (EN)"}
          tone="amber"
          content={other}
          shared={sharedByField}
          highlightFields={highlightFields}
          markCls={markCls}
        />
      </div>
    </div>
  );
}

/** One column (a single topic's fields) inside a comparison, with shared tokens highlighted. */
function ThesisColumn({
  heading,
  tone,
  content,
  shared,
  highlightFields,
  markCls,
}: Readonly<{
  heading: string;
  tone: "blue" | "amber";
  content: Record<FieldKey, string | null>;
  shared: Record<FieldKey, Set<string>>;
  highlightFields: Set<FieldKey>;
  markCls: string;
}>) {
  const toneCls =
    tone === "blue"
      ? "bg-blue-50 text-blue-600 border-blue-200"
      : "bg-amber-50 text-amber-700 border-amber-200";
  return (
    <div className="p-4 space-y-3 min-w-0">
      <span className={`inline-block px-2 py-0.5 rounded text-[10px] font-bold border ${toneCls}`}>{heading}</span>
      {ALL_FIELDS.map((f) => (
        <div key={f}>
          <p className="text-[10px] font-bold uppercase tracking-wide text-slate-400">{FIELD_LABEL[f]}</p>
          <p className="text-xs text-slate-700 mt-0.5 whitespace-pre-line leading-relaxed break-words">
            <HighlightedText
              text={content[f]}
              tokens={highlightFields.has(f) ? shared[f] : new Set()}
              markCls={markCls}
            />
          </p>
        </div>
      ))}
    </div>
  );
}

/** Circular gauge for a similarity score. */
function ScoreDial({ score, level }: Readonly<{ score: number; level: string }>) {
  const style = levelStyle(level);
  const radius = 26;
  const circumference = 2 * Math.PI * radius;
  const clamped = Math.max(0, Math.min(1, score));
  const offset = circumference * (1 - clamped);
  const stroke =
    level === "Critical" ? "#dc2626" : level === "High" ? "#ea580c" : level === "Moderate" ? "#d97706" : "#16a34a";

  return (
    <div className="relative shrink-0" style={{ width: 64, height: 64 }}>
      <svg width="64" height="64" className="-rotate-90">
        <circle cx="32" cy="32" r={radius} fill="none" stroke="#e2e8f0" strokeWidth="6" />
        <circle
          cx="32"
          cy="32"
          r={radius}
          fill="none"
          stroke={stroke}
          strokeWidth="6"
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
        />
      </svg>
      <span className={`absolute inset-0 flex items-center justify-center text-sm font-extrabold ${style.text}`}>
        {pct(score)}%
      </span>
    </div>
  );
}
