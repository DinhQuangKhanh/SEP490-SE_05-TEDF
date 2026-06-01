import { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import { useSystemError } from "@/contexts/SystemErrorContext";
import {
  evaluatorService,
  type ProjectReviewDetail,
  type SimilarTitle,
} from "@/lib/evaluatorService";

export function EvaluatorReviewPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { showError } = useSystemError();

  const [project, setProject] = useState<ProjectReviewDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [verdict, setVerdict] = useState<number | null>(null);
  const [feedback, setFeedback] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);

  // Similarity state
  const [similarTitles, setSimilarTitles] = useState<SimilarTitle[]>([]);
  const [showSimilarity, setShowSimilarity] = useState(false);
  const [loadingSimilarity, setLoadingSimilarity] = useState(false);
  const [expandedCompare, setExpandedCompare] = useState<string | null>(null);

  useEffect(() => {
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

  const handleCheckSimilarity = useCallback(async () => {
    if (!id) return;
    setLoadingSimilarity(true);
    setShowSimilarity(true);
    try {
      const titles = await evaluatorService.checkSimilarity(id);
      setSimilarTitles(titles);
    } catch {
      showError("Không thể kiểm tra trùng lặp. Vui lòng thử lại sau.");
    } finally {
      setLoadingSimilarity(false);
    }
  }, [id, showError]);

  const handleSubmit = useCallback(async () => {
    if (!id || verdict === null) return;
    setSubmitting(true);
    try {
      await evaluatorService.submitEvaluation(id, {
        result: verdict,
        feedback: feedback || undefined,
      });
      setShowSuccess(true);
      setTimeout(() => navigate("/evaluator"), 2000);
    } catch {
      showError("Không thể gửi thẩm định. Vui lòng thử lại sau.");
    } finally {
      setSubmitting(false);
    }
  }, [id, verdict, feedback, navigate, showError]);

  const getSimilarityBadge = (similarity: number) => {
    if (similarity >= 70)
      return { label: "Rất có khả năng trùng", bg: "bg-red-50", text: "text-red-600", border: "border-red-200" };
    if (similarity >= 40)
      return { label: "Cần kiểm tra thêm", bg: "bg-amber-50", text: "text-amber-600", border: "border-amber-200" };
    return { label: "Thấp", bg: "bg-green-50", text: "text-green-600", border: "border-green-200" };
  };

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
          <button
            onClick={() => navigate("/evaluator")}
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
              <button
                onClick={() => navigate("/evaluator")}
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
              <button
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
          <div className="max-w-4xl mx-auto space-y-6">
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

            {/* Similarity Results (inline) */}
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
                        <h3 className="text-sm font-bold text-slate-900">Kết quả kiểm tra trùng lặp tiêu đề</h3>
                      </div>
                      <button
                        onClick={() => { setShowSimilarity(false); setExpandedCompare(null); }}
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
                    ) : similarTitles.length === 0 ? (
                      <div className="text-center py-8">
                        <span className="material-symbols-outlined text-4xl text-green-400 mb-2 block">verified</span>
                        <p className="text-sm font-medium text-green-600">Không tìm thấy đề tài tương tự</p>
                        <p className="text-xs text-slate-500 mt-1">
                          Tiêu đề không trùng với các đề tài trong 2 học kì gần nhất.
                        </p>
                      </div>
                    ) : (
                      <div className="space-y-3">
                        {similarTitles.map((item) => {
                          const badge = getSimilarityBadge(item.similarity);
                          const isExpanded = expandedCompare === item.projectId;
                          return (
                            <div key={item.projectId}>
                              {/* Similarity card — clickable */}
                              <button
                                type="button"
                                onClick={() => setExpandedCompare(isExpanded ? null : item.projectId)}
                                className={`w-full text-left p-4 rounded-lg border transition-all ${
                                  isExpanded
                                    ? `${badge.border} ring-2 ring-offset-1 ring-primary/30 ${badge.bg}`
                                    : `${badge.border} ${badge.bg} hover:shadow-md`
                                }`}
                              >
                                <div className="flex items-start justify-between gap-3">
                                  <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2 mb-1">
                                      <span className={`text-lg font-bold ${badge.text}`}>
                                        {item.similarity}%
                                      </span>
                                      <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${badge.bg} ${badge.text}`}>
                                        {badge.label}
                                      </span>
                                    </div>
                                    <p className="text-sm font-medium text-slate-800">{item.nameEn}</p>
                                    <p className="text-xs text-slate-500">{item.nameVi}</p>
                                    <div className="flex items-center gap-2 mt-2 text-xs text-slate-500">
                                      <span className="font-mono">{item.projectCode}</span>
                                      <span>•</span>
                                      <span>{item.semesterName}</span>
                                      {item.mentorName && (
                                        <>
                                          <span>•</span>
                                          <span>GVHD: {item.mentorName}</span>
                                        </>
                                      )}
                                    </div>
                                    {item.commonKeywords.length > 0 && (
                                      <div className="flex flex-wrap gap-1 mt-2">
                                        {item.commonKeywords.map((kw) => (
                                          <span
                                            key={kw}
                                            className="px-1.5 py-0.5 rounded text-[10px] font-medium bg-white/80 text-slate-600 border border-slate-200"
                                          >
                                            {kw}
                                          </span>
                                        ))}
                                      </div>
                                    )}
                                  </div>
                                  <span className={`material-symbols-outlined text-slate-400 text-[20px] transition-transform ${isExpanded ? "rotate-180" : ""}`}>
                                    expand_more
                                  </span>
                                </div>
                              </button>

                              {/* Expanded comparison panel */}
                              <AnimatePresence>
                                {isExpanded && (
                                  <motion.div
                                    initial={{ opacity: 0, height: 0 }}
                                    animate={{ opacity: 1, height: "auto" }}
                                    exit={{ opacity: 0, height: 0 }}
                                    className="overflow-hidden"
                                  >
                                    <div className="mt-2 rounded-xl border border-gray-200 bg-gray-50 p-5">
                                      <h4 className="text-xs font-bold text-slate-500 uppercase mb-4 flex items-center gap-2">
                                        <span className="material-symbols-outlined text-[16px]">compare_arrows</span>
                                        So sánh chi tiết
                                      </h4>
                                      <div className="grid md:grid-cols-2 gap-4">
                                        {/* Left: Current project */}
                                        <div className="bg-white rounded-lg border border-blue-200 p-4">
                                          <div className="flex items-center gap-2 mb-3">
                                            <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-blue-50 text-blue-600 border border-blue-200">
                                              Đang thẩm định
                                            </span>
                                            <span className="text-xs font-mono text-slate-400">#{project.projectCode}</span>
                                          </div>
                                          <div className="space-y-3">
                                            <div>
                                              <span className="text-[10px] font-bold text-slate-400 uppercase">Tên tiếng Anh</span>
                                              <p className="text-xs text-slate-800 mt-0.5 font-medium">{project.nameEn}</p>
                                            </div>
                                            <div>
                                              <span className="text-[10px] font-bold text-slate-400 uppercase">Mô tả</span>
                                              <p className="text-xs text-slate-700 mt-0.5 line-clamp-4">{project.description}</p>
                                            </div>
                                            <div>
                                              <span className="text-[10px] font-bold text-slate-400 uppercase">Mục tiêu</span>
                                              <p className="text-xs text-slate-700 mt-0.5 line-clamp-4">{project.objectives}</p>
                                            </div>
                                            {project.technologies && (
                                              <div>
                                                <span className="text-[10px] font-bold text-slate-400 uppercase">Công nghệ</span>
                                                <div className="flex flex-wrap gap-1 mt-1">
                                                  {project.technologies.split(",").map((t) => (
                                                    <span key={t.trim()} className="px-1.5 py-0.5 rounded text-[10px] bg-blue-50 text-blue-700">{t.trim()}</span>
                                                  ))}
                                                </div>
                                              </div>
                                            )}
                                            <div className="text-[10px] text-slate-400">
                                              GVHD: {project.mentorName || "—"} • SV: {project.studentName || "—"}
                                            </div>
                                          </div>
                                        </div>

                                        {/* Right: Compared project */}
                                        <div className={`bg-white rounded-lg border p-4 ${badge.border}`}>
                                          <div className="flex items-center gap-2 mb-3">
                                            <span className={`px-2 py-0.5 rounded text-[10px] font-bold ${badge.bg} ${badge.text}`}>
                                              {item.similarity}% trùng
                                            </span>
                                            <span className="text-xs font-mono text-slate-400">{item.projectCode}</span>
                                            <span className="text-[10px] text-slate-400">{item.semesterName}</span>
                                          </div>
                                          <div className="space-y-3">
                                            <div>
                                              <span className="text-[10px] font-bold text-slate-400 uppercase">Tên tiếng Anh</span>
                                              <p className="text-xs text-slate-800 mt-0.5 font-medium">{item.nameEn}</p>
                                            </div>
                                            <div>
                                              <span className="text-[10px] font-bold text-slate-400 uppercase">Mô tả</span>
                                              <p className="text-xs text-slate-700 mt-0.5 line-clamp-4">{item.description || "—"}</p>
                                            </div>
                                            <div>
                                              <span className="text-[10px] font-bold text-slate-400 uppercase">Mục tiêu</span>
                                              <p className="text-xs text-slate-700 mt-0.5 line-clamp-4">{item.objectives || "—"}</p>
                                            </div>
                                            {item.technologies && (
                                              <div>
                                                <span className="text-[10px] font-bold text-slate-400 uppercase">Công nghệ</span>
                                                <div className="flex flex-wrap gap-1 mt-1">
                                                  {item.technologies.split(",").map((t) => (
                                                    <span key={t.trim()} className="px-1.5 py-0.5 rounded text-[10px] bg-gray-100 text-slate-700">{t.trim()}</span>
                                                  ))}
                                                </div>
                                              </div>
                                            )}
                                            <div className="text-[10px] text-slate-400">
                                              GVHD: {item.mentorName || "—"} • SV: {item.studentName || "—"}
                                            </div>
                                          </div>
                                        </div>
                                      </div>
                                    </div>
                                  </motion.div>
                                )}
                              </AnimatePresence>
                            </div>
                          );
                        })}
                      </div>
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
                return (
                  <button
                    key={value}
                    onClick={() => setVerdict(value)}
                    disabled={!!project.existingResult}
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
                  <button
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
            <button
              onClick={() => navigate("/evaluator")}
              className="flex-1 h-11 rounded-xl border border-gray-200 text-slate-700 font-semibold text-sm hover:bg-gray-50"
            >
              Quay lại
            </button>
            <button
              disabled={verdict === null || submitting || !!project.existingResult}
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
