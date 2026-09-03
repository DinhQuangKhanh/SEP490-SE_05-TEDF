import { useState, useEffect, useCallback, type ReactNode } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { evaluatorService, checklistService, topicService } from "@/lib";
import type {
  ProjectReviewResponse,
  SimilarityMatchDto,
  CheckSimilarityRequest,
  ExplainSimilarityRequest,
  FieldExplanation,
  FieldHighlight,
  HighlightSpan,
  ProjectChecklistResponse,
  TopicDocument,
  ChecklistEvaluationItemInput,
} from "@/types";
import { useSignalR, type ProjectStatusUpdatedPayload } from "@/hooks/useSignalR";
import { useNotificationTargetRefresh } from "@/hooks/useNotificationTargetRefresh";
import { EvaluationChecklistModal } from "@/components/lecturer";
import { TopicAttachmentList } from "@/components/common/TopicAttachmentList";

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

/**
 * The four DASSF dimensions the engine highlights. Each carries the colours used for its
 * toggle chip, its sub-score bar, and the <mark> that paints its overlapping spans in both
 * columns. Keys match both `SimilarityHighlights` and `DimensionBreakdown` (bar reads
 * `structure` for the `structural` key).
 */
type CategoryKey = "semantic" | "domain" | "structural" | "lexical";

interface CategoryMeta {
  label: string;
  icon: string;
  hint: string;
  /** Toggle-chip classes when the dimension is active. */
  chip: string;
  /** <mark> classes painting this dimension's spans. */
  mark: string;
  /** Sub-score bar fill. */
  bar: string;
}

/** Priority order — also the order highlighters are layered (earlier wins a shared span). */
const CATEGORY_ORDER: CategoryKey[] = ["semantic", "domain", "structural", "lexical"];

const CATEGORIES: Record<CategoryKey, CategoryMeta> = {
  semantic: {
    label: "Ngữ nghĩa",
    icon: "psychology",
    hint: "Những câu có ý nghĩa gần nhau nhất giữa hai đề tài (mô hình SBERT).",
    chip: "bg-indigo-50 text-indigo-700 border-indigo-300",
    mark: "bg-indigo-200/70 text-indigo-900",
    bar: "bg-indigo-500",
  },
  domain: {
    label: "Lĩnh vực",
    icon: "domain",
    hint: "Các thực thể / lĩnh vực nghiệp vụ dùng chung theo ontology SEDO.",
    chip: "bg-pink-50 text-pink-700 border-pink-300",
    mark: "bg-pink-200/70 text-pink-900",
    bar: "bg-pink-500",
  },
  structural: {
    label: "Cấu trúc",
    icon: "layers",
    hint: "Chức năng cốt lõi trùng nhau giữa hai đề tài (vd: đặt lịch, theo dõi thời gian thực) — KHÔNG tính công nghệ.",
    chip: "bg-violet-50 text-violet-700 border-violet-300",
    mark: "bg-violet-200/70 text-violet-900",
    bar: "bg-violet-500",
  },
  lexical: {
    label: "Từ vựng",
    icon: "match_word",
    hint: "Các thuật ngữ trọng số (TF-IDF) xuất hiện ở cả hai đề tài.",
    chip: "bg-teal-50 text-teal-700 border-teal-300",
    mark: "bg-teal-200/70 text-teal-900",
    bar: "bg-teal-500",
  },
};

const pct = (score: number) => Math.round(score * 100);

// ── Span highlighting ───────────────────────────────────────────────────────────
// The Python engine returns, PER FIELD, the exact overlapping spans on each side
// (`a` = topic under review, `b` = matched topic), each tagged with its angle. We paint every
// occurrence of those spans in the angle's colour, matching on WHOLE-WORD boundaries only.

/** Escapes a string for literal use inside a RegExp. */
function escapeRe(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/**
 * Renders `text` with each overlap span wrapped in its angle's coloured <mark>. Spans are matched
 * case-insensitively and longest-first (so a semantic passage wins over a short term nested inside
 * it), and ONLY on Unicode word boundaries — so a term like "is" never bleeds into "distance" or
 * "Redis". When two angles claim the same string, the first span (higher priority) wins its colour.
 */
function HighlightedText({
  text,
  spans,
}: Readonly<{ text: string | null | undefined; spans: HighlightSpan[] }>) {
  if (!text) return <span className="text-slate-400">—</span>;

  const spanCls = new Map<string, string>();
  for (const span of spans) {
    const key = span.text.trim().toLowerCase();
    if (key.length >= 2 && !spanCls.has(key)) spanCls.set(key, CATEGORIES[span.angle].mark);
  }
  if (spanCls.size === 0) return <>{text}</>;

  const ordered = [...spanCls.keys()].sort((a, b) => b.length - a.length);
  // Lookarounds enforce whole-word matches (Unicode-aware) instead of substring bleed.
  const re = new RegExp(
    `(?<![\\p{L}\\p{N}])(?:${ordered.map(escapeRe).join("|")})(?![\\p{L}\\p{N}])`,
    "giu",
  );

  const nodes: ReactNode[] = [];
  let last = 0;
  let key = 0;
  for (const m of text.matchAll(re)) {
    const start = m.index ?? 0;
    if (start > last) nodes.push(<span key={key++}>{text.slice(last, start)}</span>);
    const cls = spanCls.get(m[0].toLowerCase()) ?? "";
    nodes.push(
      <mark key={key++} className={`rounded px-0.5 ${cls}`}>
        {m[0]}
      </mark>,
    );
    last = start + m[0].length;
  }
  if (last < text.length) nodes.push(<span key={key++}>{text.slice(last)}</span>);
  return <>{nodes}</>;
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
        className={`rounded-full px-2 py-0.5 text-xs font-bold ${canApprove ? "bg-green-100 text-green-600" : "bg-amber-100 text-amber-700"
          }`}
      >
        {checklist.passedCount}/{total}
      </span>
    );
  }

  return <span className="material-symbols-outlined text-[18px] text-amber-500">report</span>;
}

/**
 * The three verdicts with STATIC Tailwind classes. The old code built them as `border-${color}-500`,
 * which Tailwind's scanner never sees as a complete string, so those shades were purged and the
 * selected/hover borders never rendered — the buttons looked flat and colourless.
 */
const VERDICTS = [
  { value: 1, label: "Duyệt", icon: "check_circle",
    selected: "border-green-500 bg-green-50", idle: "border-gray-200 hover:border-green-300 hover:bg-green-50/40", text: "text-green-600" },
  { value: 2, label: "Chỉnh sửa", icon: "edit_note",
    selected: "border-amber-500 bg-amber-50", idle: "border-gray-200 hover:border-amber-300 hover:bg-amber-50/40", text: "text-amber-600" },
  { value: 3, label: "Từ chối", icon: "cancel",
    selected: "border-red-500 bg-red-50", idle: "border-gray-200 hover:border-red-300 hover:bg-red-50/40", text: "text-red-600" },
] as const;

const QUICK_FEEDBACK = [
  "Đề tài có tính ứng dụng cao.",
  "Cần bổ sung phương pháp nghiên cứu.",
  "Mở rộng phần tổng quan tài liệu.",
  "Cấu trúc đề tài tốt.",
  "Mục tiêu chưa rõ ràng, cần cụ thể hơn.",
  "Phạm vi quá rộng, cần thu hẹp.",
];

/** Primary section heading inside a content card (the higher of the two label tiers). */
function SectionLabel({ children }: Readonly<{ children: ReactNode }>) {
  return <h3 className="mb-2 text-sm font-semibold text-slate-700">{children}</h3>;
}

/**
 * Renders a long field ("Phạm vi" / "Mục tiêu" / "Kết quả mong đợi"). When the text follows the
 * register-form shape — numbered role headings ("1. Admin:") and "-" bullets — it becomes a grouped
 * bullet list that is easy to scan; a plain paragraph falls back to preserved-whitespace prose.
 */
function StructuredText({ text }: Readonly<{ text: string | null | undefined }>) {
  if (!text || text.trim().length === 0) return <span className="text-slate-400">—</span>;

  const lines = text.split("\n").map((l) => l.trim()).filter(Boolean);
  const headingRe = /^\d+\.\s*(.+?):?\s*$/;         // "1. Admin:" / "2. Trưởng bộ môn (Department Head):"
  const bulletRe = /^[-•]\s+/;

  const hasStructure = lines.some((l) => (headingRe.test(l) && !bulletRe.test(l)) || bulletRe.test(l));
  if (!hasStructure) {
    return <p className="max-w-[72ch] whitespace-pre-line text-sm leading-relaxed text-slate-700">{text}</p>;
  }

  const groups: { heading: string | null; items: string[] }[] = [];
  for (const line of lines) {
    const h = headingRe.exec(line);
    if (h && !bulletRe.test(line)) {
      groups.push({ heading: h[1].trim(), items: [] });
    } else {
      if (groups.length === 0) groups.push({ heading: null, items: [] });
      groups[groups.length - 1].items.push(line.replace(bulletRe, ""));
    }
  }

  return (
    <div className="max-w-[72ch] space-y-3">
      {groups.map((g, i) => (
        <div key={g.heading ?? `g${i}`}>
          {g.heading && <p className="mb-1 text-[13px] font-semibold text-slate-800">{g.heading}</p>}
          <ul className="space-y-1">
            {g.items.map((it, j) => (
              <li key={j} className="flex gap-2 text-sm leading-relaxed text-slate-600">
                <span className="mt-[7px] size-1 shrink-0 rounded-full bg-slate-300" />
                <span>{it}</span>
              </li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  );
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

  // Attachments (register form + documents) for the topic under review.
  const [documents, setDocuments] = useState<TopicDocument[]>([]);

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

  // Attachments come from a separate endpoint; a failure just leaves the list empty.
  useEffect(() => {
    if (!id) return;
    topicService
      .getTopicDocuments(id)
      .then(setDocuments)
      .catch(() => setDocuments([]));
  }, [id]);

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
    async (items: ChecklistEvaluationItemInput[], note: string) => {
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
    if (!id || !project) return;
    setLoadingSimilarity(true);
    setShowSimilarity(true);
    try {
      // The DASSF engine prioritises full content (title + 5 fields) and only falls back to
      // title-only when the topic has nothing else. Technologies is stored as one string.
      const technologies = (project.technologies ?? "")
        .split(/[,;\n]+/)
        .map((t) => t.trim())
        .filter(Boolean);
      const body: CheckSimilarityRequest = {
        title: project.nameEn || project.nameVi,
        description: project.description || null,
        scope: project.scope,
        objectives: project.objectives || null,
        expectedResult: project.expectedResults,
        technologies,
      };
      const result = await evaluatorService.checkSimilarity(id, body);
      setMatches(result);
    } catch {
      showError("Không thể kiểm tra trùng lặp. Vui lòng thử lại sau.");
    } finally {
      setLoadingSimilarity(false);
    }
  }, [id, project, showError]);

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

  // Top similarity match, if the reviewer has already run the check — feeds the summary panel.
  const topMatch = matches.length > 0 ? matches[0] : null;

  return (
    <div className="flex h-full flex-col-reverse lg:flex-row">
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
                <h1 className="text-xl font-bold text-slate-900 mt-1 leading-snug">{project.nameVi}</h1>
                <div className="mt-1 flex flex-wrap items-center gap-2">
                  <p className="text-sm text-slate-600">{project.nameEn}</p>
                  {project.nameAbbr && (
                    <span className="rounded bg-slate-100 px-1.5 py-0.5 text-[11px] font-bold tracking-wide text-slate-500">
                      {project.nameAbbr}
                    </span>
                  )}
                </div>
                <p className="text-xs text-slate-500 mt-1.5">
                  <span className="font-medium text-slate-600">{project.studentName || "Chưa có sinh viên"}</span>
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
            {/* Description + Objectives */}
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              className="bg-white rounded-xl border border-gray-200 p-5"
            >
              <div className="grid md:grid-cols-2 gap-x-8 gap-y-6">
                <div>
                  <SectionLabel>Mô tả</SectionLabel>
                  <p className="max-w-[72ch] text-sm leading-relaxed text-slate-700 whitespace-pre-line">
                    {project.description}
                  </p>
                </div>
                <div>
                  <SectionLabel>Mục tiêu</SectionLabel>
                  <StructuredText text={project.objectives} />
                </div>
              </div>
            </motion.div>

            {/* Scope — full width; the register-form role list is very long, so it gets its own card */}
            {project.scope && (
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.05 }}
                className="bg-white rounded-xl border border-gray-200 p-5"
              >
                <SectionLabel>Phạm vi</SectionLabel>
                <StructuredText text={project.scope} />
              </motion.div>
            )}

            {/* Technologies + Expected Results */}
            {(project.technologies || project.expectedResults) && (
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.1 }}
                className="bg-white rounded-xl border border-gray-200 p-5"
              >
                <div className="grid md:grid-cols-2 gap-x-8 gap-y-6">
                  {project.technologies && (
                    <div>
                      <SectionLabel>Công nghệ</SectionLabel>
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
                      <SectionLabel>Kết quả mong đợi</SectionLabel>
                      <StructuredText text={project.expectedResults} />
                    </div>
                  )}
                </div>
              </motion.div>
            )}

            {/* Attachments (register form + documents) — preview inline, no download */}
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.12 }}
              className="bg-white rounded-xl border border-gray-200 p-5"
            >
              <SectionLabel>Tài liệu đính kèm</SectionLabel>
              <TopicAttachmentList documents={documents} title={null} />
            </motion.div>

            {/* Meta info */}
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.15 }}
              className="bg-white rounded-xl border border-gray-200 p-5"
            >
              <SectionLabel>Thông tin chung</SectionLabel>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                <div>
                  <span className="text-slate-500 text-xs">Học kì</span>
                  <p className="font-medium text-slate-800">{project.semesterName}</p>
                </div>
                <div>
                  <span className="text-slate-500 text-xs">Ngành</span>
                  <p className="font-medium text-slate-800">{project.majorName}</p>
                </div>
                <div>
                  <span className="text-slate-500 text-xs">SV tối đa</span>
                  <p className="font-medium text-slate-800">{project.maxStudents}</p>
                </div>
                <div>
                  <span className="text-slate-500 text-xs">Lần thẩm định</span>
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
          {/* Summary — a 5-second glance at the three things that drive the verdict */}
          <div className="grid grid-cols-3 gap-px overflow-hidden rounded-xl border border-gray-200 bg-gray-200 text-center">
            <div className="bg-white px-2 py-3">
              <p className="text-[11px] font-medium text-slate-500">Checklist</p>
              <p
                className={`mt-0.5 text-lg font-extrabold tabular-nums ${hasActiveConfig ? (canApprove ? "text-green-600" : "text-amber-600") : "text-slate-400"
                  }`}
              >
                {hasActiveConfig ? `${checklist?.passedCount ?? 0}/${checklistTotal}` : "—"}
              </p>
            </div>
            <div className="bg-white px-2 py-3">
              <p className="text-[11px] font-medium text-slate-500">Trùng lặp</p>
              <p className={`mt-0.5 text-lg font-extrabold tabular-nums ${topMatch ? levelStyle(topMatch.level).text : "text-slate-400"}`}>
                {topMatch ? `${pct(topMatch.overallScore)}%` : "—"}
              </p>
            </div>
            <div className="bg-white px-2 py-3">
              <p className="text-[11px] font-medium text-slate-500">Số ngày</p>
              <p className={`mt-0.5 text-lg font-extrabold tabular-nums ${project.daysElapsed > 5 ? "text-red-600" : "text-slate-700"}`}>
                {project.daysElapsed}
              </p>
            </div>
          </div>

          {/* Verdict */}
          <div>
            <h3 className="text-xs font-bold text-slate-500 uppercase mb-3">Quyết định</h3>
            <div className="grid grid-cols-3 gap-2">
              {VERDICTS.map(({ value, label, icon, selected: selCls, idle: idleCls, text }) => {
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
                    className={`flex flex-col items-center justify-center p-4 rounded-xl border-2 transition-all disabled:opacity-60 disabled:cursor-not-allowed ${selected ? selCls : idleCls
                      }`}
                  >
                    <span className={`material-symbols-outlined text-2xl mb-1 ${selected ? text : "text-gray-400"}`}>
                      {icon}
                    </span>
                    <span className={`text-xs font-bold ${selected ? text : "text-slate-500"}`}>{label}</span>
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
                {QUICK_FEEDBACK.map((t) => (
                  <button type="button"
                    key={t}
                    onClick={() => setFeedback((f) => (f.trim() ? f.trimEnd() + "\n" : "") + t)}
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
            Top {shown.length} trên {matches.length} đề tài tương đồng — mỗi trường được đối chiếu riêng, tô sáng
            đúng <span className="font-semibold">đoạn trùng mạnh nhất</span> A↔B kèm nhãn góc độ; bấm một hạng mục
            để bật/tắt màu tương ứng.
          </p>
        </div>
      </div>

      {shown.map((m, idx) => (
        <MatchComparison key={`${m.title ?? "match"}-${idx}`} match={m} project={project} index={idx} />
      ))}
    </div>
  );
}

/** The matched topic's `structural` highlight key reads `structure` in the breakdown. */
function breakdownValue(match: SimilarityMatchDto, c: CategoryKey): number {
  const b = match.breakdown;
  if (!b) return 0;
  return c === "structural" ? b.structure : b[c];
}

/**
 * One matched topic shown side-by-side with the one under review. Each of the four DASSF
 * dimensions is a toggle that both reports its sub-score and paints its most-overlapping
 * spans (returned by the engine) in the two columns, in its own colour.
 */
function MatchComparison({
  match,
  project,
  index,
}: Readonly<{ match: SimilarityMatchDto; project: ProjectReviewResponse; index: number }>) {
  const s = levelStyle(match.level);
  // All four dimensions highlighted by default; each chip toggles its own colour on/off.
  const [active, setActive] = useState<Set<CategoryKey>>(() => new Set(CATEGORY_ORDER));
  const toggle = (c: CategoryKey) =>
    setActive((prev) => {
      const next = new Set(prev);
      if (next.has(c)) next.delete(c);
      else next.add(c);
      return next;
    });

  const current: Record<FieldKey, string | null> = {
    title: project.nameEn || project.nameVi,
    description: project.description,
    objectives: project.objectives,
    scope: project.scope,
    technologies: project.technologies,
    expectedResults: project.expectedResults,
  };
  const other: Record<FieldKey, string | null> = {
    title: match.title,
    description: match.description,
    objectives: match.objectives,
    scope: match.scope,
    technologies: match.technologies.length > 0 ? match.technologies.join(", ") : null,
    expectedResults: match.expectedResult,
  };

  // Field-aligned overlaps keyed by FieldKey, so each field highlights only its own spans.
  // `technologies` is never highlighted: the structural dimension no longer scores on the tech
  // stack, so painting React/.NET as "Cấu trúc" would contradict the score. (Filtered here too so
  // a stale engine response can't reintroduce it.)
  const alignments = new Map<string, FieldHighlight>(
    (match.highlights?.fields ?? [])
      .filter((f) => f.field !== "technologies")
      .map((f) => [f.field, f]),
  );

  // Per-field "why these overlap" text, fetched on demand. Grounded in the SAME highlight spans
  // above, so the narrative can never disagree with the colours painted in the two columns.
  const [explanations, setExplanations] = useState<FieldExplanation[] | null>(null);
  const [explaining, setExplaining] = useState(false);
  const [explainError, setExplainError] = useState<string | null>(null);

  const handleExplain = useCallback(async () => {
    setExplaining(true);
    setExplainError(null);
    try {
      // Same technologies parsing as the similarity check: the field is stored as one string.
      const queryTech = (project.technologies ?? "")
        .split(/[,;\n]+/)
        .map((t) => t.trim())
        .filter(Boolean);
      const body: ExplainSimilarityRequest = {
        query: {
          title: project.nameEn || project.nameVi,
          description: project.description || null,
          scope: project.scope,
          objectives: project.objectives || null,
          expectedResult: project.expectedResults,
          technologies: queryTech,
        },
        match: {
          title: match.title,
          description: match.description,
          scope: match.scope,
          objectives: match.objectives,
          expectedResult: match.expectedResult,
          technologies: match.technologies,
        },
      };
      setExplanations(await evaluatorService.explainSimilarity(project.projectId, body));
    } catch {
      setExplainError("Không thể tạo giải thích. Vui lòng thử lại sau.");
    } finally {
      setExplaining(false);
    }
  }, [project, match]);

  return (
    <div className="rounded-xl border border-gray-200 overflow-hidden">
      {/* Header: rank + score + level (+ structural-duplication flag + matched title) */}
      <div className="flex flex-wrap items-center justify-between gap-3 px-4 py-3 bg-slate-50 border-b border-gray-200">
        <div className="flex items-center gap-2.5 min-w-0">
          <span className="text-[10px] font-mono text-slate-400">Kết quả #{index + 1}</span>
          <span className={`text-xl font-extrabold ${s.text}`}>{pct(match.overallScore)}%</span>
          <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold border ${s.bg} ${s.text} ${s.border}`}>
            {s.label}
          </span>
          {match.isStructuralDuplication && (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold border bg-amber-50 text-amber-700 border-amber-200">
              <span className="material-symbols-outlined text-[12px]">content_copy</span>
              Sao chép cấu trúc
            </span>
          )}
          {match.semester && <span className="text-[10px] text-slate-400">{match.semester}</span>}
        </div>
        {match.title && (
          <span className="text-[11px] font-semibold text-slate-600 truncate max-w-[45%]" title={match.title}>
            {match.title}
          </span>
        )}
      </div>

      {/* Four dimension toggles, each doubling as its sub-score bar */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 px-4 py-3 bg-white border-b border-gray-100">
        {CATEGORY_ORDER.map((c) => {
          const meta = CATEGORIES[c];
          const on = active.has(c);
          const v = breakdownValue(match, c);
          return (
            <button
              key={c}
              type="button"
              onClick={() => toggle(c)}
              title={meta.hint}
              aria-pressed={on}
              className={`flex flex-col gap-1 rounded-lg border px-2.5 py-2 text-left transition-all ${on ? `${meta.chip} shadow-sm` : "bg-slate-50 text-slate-400 border-slate-200"
                }`}
            >
              <span className="flex items-center gap-1 text-[11px] font-bold">
                <span className="material-symbols-outlined text-[14px]">{meta.icon}</span>
                {meta.label}
                <span className="ml-auto tabular-nums">{pct(v)}%</span>
              </span>
              <span className="h-1.5 w-full rounded-full bg-black/5 overflow-hidden">
                <span
                  className={`block h-full rounded-full ${on ? meta.bar : "bg-slate-300"}`}
                  style={{ width: `${Math.max(4, pct(v))}%` }}
                />
              </span>
            </button>
          );
        })}
      </div>

      {/* Hint */}
      <div className="px-4 py-2 text-[11px] text-slate-500 bg-white border-b border-gray-100">
        Mỗi trường gắn <span className="font-semibold">nhãn cho từng chiều mà nó trùng</span>; đoạn/thuật ngữ trùng
        được tô đúng màu của chiều đó ở cả hai cột. Trường không trùng đáng kể để trơn. Điểm 4 chiều xem ở các ô
        phía trên. Bấm hạng mục để bật/tắt màu.
      </div>

      {/* Explain overlap — per field, grounded in the same highlight spans painted below */}
      <div className="px-4 py-3 bg-white border-b border-gray-100">
        {!explanations && (
          <button
            type="button"
            onClick={handleExplain}
            disabled={explaining}
            className="inline-flex items-center gap-1.5 rounded-lg border border-indigo-300 bg-indigo-50 px-3 py-2 text-[13px] font-semibold text-indigo-700 shadow-sm transition-colors hover:bg-indigo-100 disabled:opacity-60"
          >
            <span className={`material-symbols-outlined text-[16px] ${explaining ? "animate-spin" : ""}`}>
              {explaining ? "progress_activity" : "chat"}
            </span>
            {explaining ? "Đang tạo giải thích…" : "Giải thích trùng lặp (AI)"}
          </button>
        )}

        {explainError && <p className="mt-1 text-[11px] font-medium text-red-600">{explainError}</p>}

        {explanations &&
          (explanations.length === 0 ? (
            <p className="text-[11px] text-slate-500">Không có hạng mục nào trùng đáng kể để giải thích.</p>
          ) : (
            <div className="space-y-2">
              <p className="flex items-center gap-1 text-[11px] font-semibold text-slate-600">
                <span className="material-symbols-outlined text-[15px] text-indigo-600">chat</span>
                Giải thích trùng lặp theo từng hạng mục
              </p>
              {explanations.map((e) => (
                <div key={e.field} className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
                  <div className="mb-1 flex flex-wrap items-center gap-2">
                    <span className="text-[11px] font-bold text-slate-700">
                      {FIELD_LABEL[e.field as FieldKey] ?? e.field}
                    </span>
                    {e.angle && <AngleBadge angle={e.angle as CategoryKey} score={e.score} />}
                  </div>
                  <p className="text-[12px] leading-relaxed text-slate-700">{e.explanation}</p>
                </div>
              ))}
            </div>
          ))}
      </div>

      {/* Two columns, side-by-side (rows are field-aligned: mô tả A ↔ mô tả B, …) */}
      <div className="grid grid-cols-1 lg:grid-cols-2 divide-y lg:divide-y-0 lg:divide-x divide-gray-100">
        <ThesisColumn heading="Đề tài đang thẩm định" tone="blue" side="a" content={current} alignments={alignments} active={active} />
        <ThesisColumn heading="Đề tài có khả năng trùng" tone="amber" side="b" content={other} alignments={alignments} active={active} />
      </div>
    </div>
  );
}

/** Small pill next to a field label: the field's dominant overlap angle + (semantic) score. */
function AngleBadge({ angle, score }: Readonly<{ angle: CategoryKey; score: number | null }>) {
  const meta = CATEGORIES[angle];
  return (
    <span
      className={`inline-flex items-center gap-0.5 rounded-full border px-1.5 py-0.5 text-[9px] font-bold ${meta.chip}`}
    >
      <span className="material-symbols-outlined text-[11px]">{meta.icon}</span>
      {meta.label}
      {score != null && <span className="tabular-nums">· {pct(score)}%</span>}
    </span>
  );
}

/**
 * One column (a single topic's fields) inside a comparison. Each field pulls its own alignment,
 * shows an angle badge, and highlights only that field's spans (filtered by the active angles).
 */
function ThesisColumn({
  heading,
  tone,
  side,
  content,
  alignments,
  active,
}: Readonly<{
  heading: string;
  tone: "blue" | "amber";
  side: "a" | "b";
  content: Record<FieldKey, string | null>;
  alignments: Map<string, FieldHighlight>;
  active: Set<CategoryKey>;
}>) {
  const toneCls =
    tone === "blue"
      ? "bg-blue-50 text-blue-600 border-blue-200"
      : "bg-amber-50 text-amber-700 border-amber-200";
  return (
    <div className="p-4 space-y-3 min-w-0">
      <span className={`inline-block px-2 py-0.5 rounded text-[10px] font-bold border ${toneCls}`}>{heading}</span>
      {ALL_FIELDS.map((f) => {
        const align = alignments.get(f);
        const spans: HighlightSpan[] = align
          ? (side === "a" ? align.a : align.b).filter((sp) => active.has(sp.angle))
          : [];
        // Every dimension this field actually overlaps on gets its own badge (not just the single
        // dominant one) — so all four DASSF angles stay visible per field, matching the colours
        // painted in the text below. The authoritative % lives in the four chips above, so the
        // per-field badges carry no (contradictory) number.
        const fieldAngles = CATEGORY_ORDER.filter((c) => spans.some((sp) => sp.angle === c));
        return (
          <div key={f}>
            <div className="flex items-center gap-2 flex-wrap">
              <p className="text-[10px] font-bold uppercase tracking-wide text-slate-400">{FIELD_LABEL[f]}</p>
              {fieldAngles.map((c) => (
                <AngleBadge key={c} angle={c} score={null} />
              ))}
            </div>
            <p className="text-xs text-slate-700 mt-0.5 whitespace-pre-line leading-relaxed break-words">
              <HighlightedText text={content[f]} spans={spans} />
            </p>
          </div>
        );
      })}
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
