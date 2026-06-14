import { useState, useEffect, useCallback } from "react";
import { AutoResizeTextarea } from "@/components/common/AutoResizeTextarea";
import { motion, AnimatePresence } from "framer-motion";
import { RegisterTopicModal } from "@/components/mentor/RegisterTopicModal";
import { Header } from "@/components/layout/Header";
import {
  topicService,
  topicPoolService,
  proposedTopicService,
  semesterService,
  sourceTypeLabel,
  statusConfig,
  evaluationStatusConfig,
} from "@/lib";
import type {
  MentorTopicItem,
  MentorTopicsResponse,
  SemesterOption,
  TopicDetail,
  TopicDocument,
  UpdatePoolTopicRequest,
} from "@/types";

// ── Animation variants ───────────────────────────────────────────────────────

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.05 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

// ── Helpers ──────────────────────────────────────────────────────────────────

function formatDate(iso: string | null): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
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

const PAGE_SIZE = 10;

// ── Main Component ───────────────────────────────────────────────────────────

export function MentorTopicsPage() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [detailTopic, setDetailTopic] = useState<MentorTopicItem | null>(null);

  // Data
  const [data, setData] = useState<MentorTopicsResponse | null>(null);
  const [semesters, setSemesters] = useState<SemesterOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [selectedSemester, setSelectedSemester] = useState<number | undefined>(undefined);
  const [page, setPage] = useState(1);

  // Debounce search
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(timer);
  }, [search]);

  // Reset page when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, selectedSemester]);

  // Load semesters once
  useEffect(() => {
    semesterService
      .getSemesterOptions()
      .then((list) => {
        // Sort by startDate desc
        const sorted = list.sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime());
        setSemesters(sorted);
        // Default to the first (latest) semester
        if (sorted.length > 0) {
          setSelectedSemester(sorted[0].id);
        }
      })
      .catch(() => {
        // semesters load failure is non-critical
      });
  }, []);

  // Load topics
  const fetchTopics = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await topicService.getMentorTopics({
        semesterId: selectedSemester,
        search: debouncedSearch || undefined,
        page,
        pageSize: PAGE_SIZE,
      });
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tải dữ liệu");
    } finally {
      setLoading(false);
    }
  }, [selectedSemester, debouncedSearch, page]);

  useEffect(() => {
    fetchTopics();
  }, [fetchTopics]);

  const totalPages = data?.totalPages ?? 1;
  const pageNumbers = getPageNumbers(page, totalPages);

  return (
    <>
      <Header
        title="Danh sách đề tài"
        subtitle="Quản lý và theo dõi trạng thái thẩm định các đề tài hướng dẫn"
        role="mentor"
        showSearch={false}
        breadcrumb={[{ label: "TEDF" }, { label: "Danh sách đề tài" }]}
      />

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-8 bg-slate-100">
        <motion.div variants={container} initial="hidden" animate="show" className="space-y-6">
          {/* Title & Actions */}
          <motion.div variants={item} className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div>
              <h2 className="text-2xl font-bold text-slate-900">Kho Đề Tài Của Mentor</h2>
              <p className="text-slate-500 mt-1 text-sm">
                Quản lý và theo dõi trạng thái thẩm định các đề tài hướng dẫn.
              </p>
            </div>
            <button
              onClick={() => setIsModalOpen(true)}
              className="inline-flex items-center justify-center gap-2 bg-primary hover:bg-primary/90 text-white px-4 py-2 rounded-lg font-semibold shadow-sm transition-all text-sm"
            >
              <span className="material-symbols-outlined text-[20px]">add</span>
              <span>Đề xuất mới</span>
            </button>
          </motion.div>

          {/* Search & Filter */}
          <motion.div
            variants={item}
            className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex flex-col md:flex-row gap-4 items-center justify-between"
          >
            <div className="relative w-full md:w-96">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <span className="material-symbols-outlined text-slate-400 text-[20px]">search</span>
              </div>
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="block w-full pl-10 pr-9 py-2 border border-slate-200 rounded-lg bg-slate-50 text-slate-900 placeholder-slate-400 focus:outline-none focus:bg-white focus:ring-1 focus:ring-primary focus:border-primary sm:text-sm transition-all"
                placeholder="Tìm kiếm theo mã hoặc tên đề tài..."
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
            <div className="flex items-center gap-3 w-full md:w-auto">
              <div className="flex items-center gap-2 min-w-[200px]">
                <span className="text-sm text-slate-500 font-medium whitespace-nowrap">Kỳ học:</span>
                <select
                  value={selectedSemester ?? ""}
                  onChange={(e) => {
                    const val = e.target.value;
                    setSelectedSemester(val ? Number(val) : undefined);
                  }}
                  className="form-select block w-full pl-3 pr-10 py-1.5 text-sm border-slate-200 focus:ring-primary focus:border-primary rounded-lg bg-slate-50"
                >
                  <option value="">Tất cả</option>
                  {semesters.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </motion.div>

          {/* Topics Table */}
          <motion.div variants={item} className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            {loading ? (
              <TableSkeleton />
            ) : error ? (
              <ErrorState message={error} onRetry={fetchTopics} />
            ) : !data || data.items.length === 0 ? (
              <EmptyState />
            ) : (
              <>
                <div className="overflow-x-auto">
                  <table className="w-full text-left border-collapse">
                    <thead className="bg-slate-50 border-b border-slate-200 text-slate-500 text-xs uppercase font-bold tracking-wider">
                      <tr>
                        <th className="px-6 py-4 w-36">Mã Đề Tài</th>
                        <th className="px-6 py-4 min-w-[300px]">Tên Đề Tài</th>
                        <th className="px-6 py-4 w-40 text-center">Loại Đề Tài</th>
                        <th className="px-6 py-4 w-40 text-center">Thẩm Định</th>
                        <th className="px-6 py-4 w-36">Ngày Gửi</th>
                        <th className="px-6 py-4 w-32 text-right"></th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {data.items.map((topic) => {
                        const evalSc = evaluationStatusConfig(topic.status);
                        return (
                          <tr
                            key={topic.id}
                            className="hover:bg-slate-50 transition-colors group cursor-pointer"
                            onClick={() => setDetailTopic(topic)}
                          >
                            <td className="px-6 py-4 text-sm font-medium text-slate-500 font-mono">{topic.code}</td>
                            <td className="px-6 py-4">
                              <div className="flex items-start gap-3">
                                <div className="mt-0.5 bg-primary/10 text-primary rounded p-1.5 flex-shrink-0">
                                  <span className="material-symbols-outlined text-[18px]">description</span>
                                </div>
                                <div>
                                  <p className="text-sm font-bold text-slate-900 group-hover:text-primary transition-colors">
                                    {topic.nameVi}
                                  </p>
                                  <p className="text-xs text-slate-500 mt-0.5">Lĩnh vực: {topic.majorName}</p>
                                </div>
                              </div>
                            </td>
                            <td className="px-6 py-4">
                              <span
                                className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                  topic.sourceType === 0
                                    ? "bg-indigo-50 text-indigo-700 border border-indigo-100"
                                    : "bg-slate-100 text-slate-700 border border-slate-200"
                                }`}
                              >
                                {sourceTypeLabel(topic.sourceType)}
                              </span>
                            </td>
                            <td className="px-6 py-4 text-center">
                              <span
                                className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold ${evalSc.bg} ${evalSc.text} border border-current/10`}
                              >
                                <span className={`size-1.5 rounded-full ${evalSc.dot}`} />
                                {evalSc.label}
                              </span>
                            </td>
                            <td className="px-6 py-4 text-sm text-slate-600">
                              {formatDate(topic.submittedAt ?? topic.createdAt)}
                            </td>
                            <td className="px-6 py-4 text-right"></td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>

                {/* Pagination */}
                {data.totalCount > 0 && (
                  <div className="bg-white border-t border-slate-200 px-6 py-4 flex items-center justify-between">
                    <p className="text-sm text-slate-500">
                      Hiển thị <span className="font-medium text-slate-900">{(page - 1) * PAGE_SIZE + 1}</span> đến{" "}
                      <span className="font-medium text-slate-900">{Math.min(page * PAGE_SIZE, data.totalCount)}</span>{" "}
                      trong số <span className="font-medium text-slate-900">{data.totalCount}</span> đề tài
                    </p>
                    {totalPages > 1 && (
                      <div className="flex items-center gap-1">
                        <button
                          onClick={() => setPage((p) => Math.max(1, p - 1))}
                          disabled={page <= 1}
                          className="px-3 py-1.5 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-md hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          Trước
                        </button>
                        {pageNumbers.map((p, i) =>
                          p === "..." ? (
                            <span key={`dots-${i}`} className="px-2 py-1 text-slate-400">
                              ...
                            </span>
                          ) : (
                            <button
                              key={p}
                              onClick={() => setPage(p)}
                              className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                                p === page
                                  ? "bg-primary text-white"
                                  : "text-slate-600 border border-slate-200 hover:bg-slate-50"
                              }`}
                            >
                              {p}
                            </button>
                          ),
                        )}
                        <button
                          onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                          disabled={page >= totalPages}
                          className="px-3 py-1.5 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-md hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          Sau
                        </button>
                      </div>
                    )}
                  </div>
                )}
              </>
            )}
          </motion.div>
        </motion.div>
      </div>

      {/* Register Topic Modal */}
      <RegisterTopicModal
        isOpen={isModalOpen}
        onClose={(success) => {
          setIsModalOpen(false);
          if (success) fetchTopics();
        }}
      />

      {/* Topic Detail Modal */}
      <AnimatePresence>
        {detailTopic && (
          <TopicDetailModal
            topic={detailTopic}
            onClose={() => setDetailTopic(null)}
            onReviewed={() => {
              setDetailTopic(null);
              fetchTopics();
            }}
          />
        )}
      </AnimatePresence>
    </>
  );
}

// ── Topic Detail Modal ───────────────────────────────────────────────────────

function TopicDetailModal({
  topic,
  onClose,
  onReviewed,
}: {
  topic: MentorTopicItem;
  onClose: () => void;
  onReviewed: () => void;
}) {
  const [detail, setDetail] = useState<TopicDetail | null>(null);
  const [documents, setDocuments] = useState<TopicDocument[]>([]);
  const [loading, setLoading] = useState(true);
  const [reviewAction, setReviewAction] = useState<"approve" | "requestModification" | null>(null);
  const [feedback, setFeedback] = useState("");
  const [reviewLoading, setReviewLoading] = useState(false);
  const [reviewError, setReviewError] = useState<string | null>(null);

  // Edit/resubmit state for pool topics with NeedsModification
  const [isEditing, setIsEditing] = useState(false);
  const [editForm, setEditForm] = useState<UpdatePoolTopicRequest>({
    nameVi: "",
    nameEn: "",
    nameAbbr: "",
    description: "",
    objectives: "",
    scope: "",
    technologies: "",
    expectedResults: "",
    maxStudents: 1,
  });
  const [editLoading, setEditLoading] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [resubmitLoading, setResubmitLoading] = useState(false);
  const [resubmitError, setResubmitError] = useState<string | null>(null);

  const isPendingMentorReview = topic.status === 8;
  const isPoolNeedsModification = topic.sourceType === 0 && topic.status === 2;
  const isDirectNeedsModification = topic.sourceType === 1 && topic.status === 2;

  useEffect(() => {
    setLoading(true);
    Promise.allSettled([topicService.getTopicDetail(topic.id), topicService.getTopicDocuments(topic.id)]).then(
      ([detailRes, docsRes]) => {
        if (detailRes.status === "fulfilled") {
          const d = detailRes.value;
          setDetail(d);
          // Pre-populate edit form for pool topics
          if (topic.sourceType === 0) {
            setEditForm({
              nameVi: d.nameVi,
              nameEn: d.nameEn,
              nameAbbr: d.nameAbbr,
              description: d.description,
              objectives: d.objectives,
              scope: d.scope ?? "",
              technologies: d.technologies ?? "",
              expectedResults: d.expectedResults ?? "",
              maxStudents: d.maxStudents,
            });
          }
        }
        if (docsRes.status === "fulfilled") setDocuments(docsRes.value);
        setLoading(false);
      },
    );
  }, [topic.id, topic.sourceType]);

  const sc = statusConfig(topic.status);

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
        className="bg-white rounded-2xl shadow-2xl w-full max-w-2xl max-h-[85vh] flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-200 flex items-center justify-between shrink-0">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <span className="text-xs font-mono text-slate-400">{topic.code}</span>
              <span
                className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-semibold ${sc.bg} ${sc.text}`}
              >
                <span className={`size-1.5 rounded-full ${sc.dot}`} />
                {sc.label}
              </span>
              <span
                className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
                  topic.sourceType === 0 ? "bg-indigo-50 text-indigo-700" : "bg-slate-100 text-slate-600"
                }`}
              >
                {sourceTypeLabel(topic.sourceType)}
              </span>
            </div>
            <h2 className="text-lg font-bold text-slate-900 truncate">{topic.nameVi}</h2>
            {topic.nameEn && <p className="text-sm text-slate-500 truncate">{topic.nameEn}</p>}
          </div>
          <button onClick={onClose} className="p-1.5 hover:bg-slate-100 rounded-lg transition-colors ml-3">
            <span className="material-symbols-outlined text-slate-400">close</span>
          </button>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto px-6 py-5">
          {loading ? (
            <div className="space-y-4">
              {[1, 2, 3, 4].map((i) => (
                <div key={i} className="animate-pulse">
                  <div className="h-4 w-24 bg-slate-200 rounded mb-2" />
                  <div className="h-4 w-full bg-slate-100 rounded" />
                </div>
              ))}
            </div>
          ) : detail ? (
            isEditing && isPoolNeedsModification ? (
              /* ── Edit Form for Pool Topics ── */
              <div className="space-y-4">
                {editError && (
                  <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">{editError}</div>
                )}
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                      Tên tiếng Việt *
                    </label>
                    <input
                      type="text"
                      value={editForm.nameVi}
                      onChange={(e) => setEditForm({ ...editForm, nameVi: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                      Tên tiếng Anh *
                    </label>
                    <input
                      type="text"
                      value={editForm.nameEn}
                      onChange={(e) => setEditForm({ ...editForm, nameEn: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                    />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                      Tên viết tắt *
                    </label>
                    <input
                      type="text"
                      value={editForm.nameAbbr}
                      onChange={(e) => setEditForm({ ...editForm, nameAbbr: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                      SV tối đa *
                    </label>
                    <input
                      type="number"
                      min={1}
                      max={5}
                      value={editForm.maxStudents}
                      onChange={(e) => setEditForm({ ...editForm, maxStudents: Number(e.target.value) })}
                      className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Mô tả *
                  </label>
                  <AutoResizeTextarea
                    rows={3}
                    value={editForm.description}
                    onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Mục tiêu *
                  </label>
                  <AutoResizeTextarea
                    rows={3}
                    value={editForm.objectives}
                    onChange={(e) => setEditForm({ ...editForm, objectives: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Phạm vi
                  </label>
                  <AutoResizeTextarea
                    rows={2}
                    value={editForm.scope ?? ""}
                    onChange={(e) => setEditForm({ ...editForm, scope: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Công nghệ sử dụng
                  </label>
                  <AutoResizeTextarea
                    rows={2}
                    value={editForm.technologies ?? ""}
                    onChange={(e) => setEditForm({ ...editForm, technologies: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Kết quả mong đợi
                  </label>
                  <AutoResizeTextarea
                    rows={2}
                    value={editForm.expectedResults ?? ""}
                    onChange={(e) => setEditForm({ ...editForm, expectedResults: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                  />
                </div>
              </div>
            ) : (
              /* ── Read-only Detail View ── */
              <div className="space-y-5">
                {/* Meta info */}
                <div className="grid grid-cols-2 gap-4">
                  <InfoField label="Lĩnh vực" value={detail.majorName} icon="school" />
                  <InfoField label="Học kỳ" value={topic.semesterName} icon="calendar_today" />
                  <InfoField label="Ngày tạo" value={formatDate(detail.createdAt)} icon="event" />
                  <InfoField label="SV tối đa" value={String(detail.maxStudents)} icon="groups" />
                </div>

                {/* Mentors */}
                {detail.mentors.length > 0 && (
                  <div>
                    <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">
                      Giảng viên hướng dẫn
                    </h4>
                    <div className="flex flex-wrap gap-2">
                      {detail.mentors.map((m) => (
                        <span
                          key={m.mentorId}
                          className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-primary/5 text-primary text-sm font-medium rounded-full"
                        >
                          <span className="material-symbols-outlined text-[16px]">person</span>
                          {m.fullName}
                        </span>
                      ))}
                    </div>
                  </div>
                )}

                {/* Description */}
                {detail.description && <DetailSection title="Mô tả" content={detail.description} />}
                {detail.objectives && <DetailSection title="Mục tiêu" content={detail.objectives} />}
                {detail.scope && <DetailSection title="Phạm vi" content={detail.scope} />}
                {detail.technologies && <DetailSection title="Công nghệ sử dụng" content={detail.technologies} />}
                {detail.expectedResults && <DetailSection title="Kết quả mong đợi" content={detail.expectedResults} />}

                {/* Documents */}
                {documents.length > 0 && (
                  <div>
                    <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">
                      Tài liệu đính kèm
                    </h4>
                    <div className="space-y-2">
                      {documents.map((doc) => (
                        <div
                          key={doc.id}
                          className="flex items-center gap-3 p-3 bg-slate-50 rounded-lg border border-slate-100"
                        >
                          <span className="material-symbols-outlined text-slate-400 text-[20px]">attach_file</span>
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium text-slate-700 truncate">{doc.originalFileName}</p>
                            <p className="text-xs text-slate-400">
                              {(doc.fileSize / 1024).toFixed(1)} KB &middot; {formatDate(doc.uploadedAt)}
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )
          ) : (
            <p className="text-sm text-slate-500 text-center py-8">Không thể tải thông tin chi tiết.</p>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-200 shrink-0">
          {isPendingMentorReview && !reviewAction && (
            <div className="flex items-center gap-3">
              <button
                onClick={() => setReviewAction("approve")}
                className="flex-1 px-4 py-2.5 bg-emerald-600 text-white text-sm font-semibold rounded-lg hover:bg-emerald-700 transition-colors flex items-center justify-center gap-1.5"
              >
                <span className="material-symbols-outlined text-lg">check_circle</span>
                Duyệt & gửi thẩm định
              </button>
              <button
                onClick={() => setReviewAction("requestModification")}
                className="flex-1 px-4 py-2.5 bg-amber-500 text-white text-sm font-semibold rounded-lg hover:bg-amber-600 transition-colors flex items-center justify-center gap-1.5"
              >
                <span className="material-symbols-outlined text-lg">edit_note</span>
                Yêu cầu chỉnh sửa
              </button>
            </div>
          )}

          {reviewAction === "approve" && (
            <div className="space-y-3">
              {reviewError && <p className="text-sm text-red-600">{reviewError}</p>}
              <p className="text-sm text-slate-600">Xác nhận duyệt đề tài này và gửi đi thẩm định?</p>
              <div className="flex items-center gap-3">
                <button
                  onClick={async () => {
                    setReviewLoading(true);
                    setReviewError(null);
                    try {
                      await proposedTopicService.mentorReviewProposedTopic(topic.id, { action: "approve" });
                      onReviewed();
                    } catch (err) {
                      setReviewError(err instanceof Error ? err.message : "Đã xảy ra lỗi.");
                    } finally {
                      setReviewLoading(false);
                    }
                  }}
                  disabled={reviewLoading}
                  className="flex-1 px-4 py-2.5 bg-emerald-600 text-white text-sm font-semibold rounded-lg hover:bg-emerald-700 transition-colors disabled:opacity-50 flex items-center justify-center gap-1.5"
                >
                  {reviewLoading && (
                    <div className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent" />
                  )}
                  Xác nhận duyệt
                </button>
                <button
                  onClick={() => setReviewAction(null)}
                  className="px-4 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-100 rounded-lg transition-colors"
                >
                  Hủy
                </button>
              </div>
            </div>
          )}

          {reviewAction === "requestModification" && (
            <div className="space-y-3">
              {reviewError && <p className="text-sm text-red-600">{reviewError}</p>}
              <AutoResizeTextarea
                value={feedback}
                onChange={(e) => setFeedback(e.target.value)}
                rows={3}
                className="w-full px-3 py-2.5 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                placeholder="Nhập góp ý cho sinh viên (tùy chọn)..."
              />
              <div className="flex items-center gap-3">
                <button
                  onClick={async () => {
                    setReviewLoading(true);
                    setReviewError(null);
                    try {
                      await proposedTopicService.mentorReviewProposedTopic(topic.id, {
                        action: "requestModification",
                        feedback: feedback.trim() || undefined,
                      });
                      onReviewed();
                    } catch (err) {
                      setReviewError(err instanceof Error ? err.message : "Đã xảy ra lỗi.");
                    } finally {
                      setReviewLoading(false);
                    }
                  }}
                  disabled={reviewLoading}
                  className="flex-1 px-4 py-2.5 bg-amber-500 text-white text-sm font-semibold rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 flex items-center justify-center gap-1.5"
                >
                  {reviewLoading && (
                    <div className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent" />
                  )}
                  Gửi yêu cầu chỉnh sửa
                </button>
                <button
                  onClick={() => {
                    setReviewAction(null);
                    setFeedback("");
                  }}
                  className="px-4 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-100 rounded-lg transition-colors"
                >
                  Hủy
                </button>
              </div>
            </div>
          )}

          {/* Pool topic NeedsModification: Edit mode footer */}
          {isEditing && isPoolNeedsModification && (
            <div className="flex items-center gap-3">
              <button
                onClick={async () => {
                  setEditLoading(true);
                  setEditError(null);
                  try {
                    await topicPoolService.updatePoolTopic(topic.id, editForm);
                    setIsEditing(false);
                    // Reload detail
                    const updated = await topicService.getTopicDetail(topic.id);
                    setDetail(updated);
                  } catch (err) {
                    setEditError(err instanceof Error ? err.message : "Đã xảy ra lỗi khi cập nhật.");
                  } finally {
                    setEditLoading(false);
                  }
                }}
                disabled={editLoading}
                className="flex-1 px-4 py-2.5 bg-primary text-white text-sm font-semibold rounded-lg hover:bg-primary/90 transition-colors disabled:opacity-50 flex items-center justify-center gap-1.5"
              >
                {editLoading && (
                  <div className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent" />
                )}
                <span className="material-symbols-outlined text-lg">save</span>
                Lưu thay đổi
              </button>
              <button
                onClick={() => {
                  setIsEditing(false);
                  setEditError(null);
                  // Reset form to current detail
                  if (detail) {
                    setEditForm({
                      nameVi: detail.nameVi,
                      nameEn: detail.nameEn,
                      nameAbbr: detail.nameAbbr,
                      description: detail.description,
                      objectives: detail.objectives,
                      scope: detail.scope ?? "",
                      technologies: detail.technologies ?? "",
                      expectedResults: detail.expectedResults ?? "",
                      maxStudents: detail.maxStudents,
                    });
                  }
                }}
                className="px-4 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-100 rounded-lg transition-colors"
              >
                Hủy
              </button>
            </div>
          )}

          {/* Pool topic NeedsModification: View mode footer */}
          {!isEditing && isPoolNeedsModification && !reviewAction && (
            <div className="space-y-3">
              {resubmitError && <p className="text-sm text-red-600">{resubmitError}</p>}
              <div className="flex items-center gap-3">
                <button
                  onClick={() => setIsEditing(true)}
                  className="flex-1 px-4 py-2.5 bg-amber-500 text-white text-sm font-semibold rounded-lg hover:bg-amber-600 transition-colors flex items-center justify-center gap-1.5"
                >
                  <span className="material-symbols-outlined text-lg">edit</span>
                  Chỉnh sửa đề tài
                </button>
                <button
                  onClick={async () => {
                    setResubmitLoading(true);
                    setResubmitError(null);
                    try {
                      await topicPoolService.resubmitPoolTopic(topic.id);
                      onReviewed();
                    } catch (err) {
                      setResubmitError(err instanceof Error ? err.message : "Đã xảy ra lỗi.");
                    } finally {
                      setResubmitLoading(false);
                    }
                  }}
                  disabled={resubmitLoading}
                  className="flex-1 px-4 py-2.5 bg-emerald-600 text-white text-sm font-semibold rounded-lg hover:bg-emerald-700 transition-colors disabled:opacity-50 flex items-center justify-center gap-1.5"
                >
                  {resubmitLoading && (
                    <div className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent" />
                  )}
                  <span className="material-symbols-outlined text-lg">send</span>
                  Gửi lại thẩm định
                </button>
              </div>
            </div>
          )}

          {/* Direct registration NeedsModification: Info message for mentor */}
          {isDirectNeedsModification && !reviewAction && (
            <div className="flex items-center gap-3">
              <div className="flex-1 flex items-center gap-2 px-4 py-3 bg-slate-50 border border-slate-200 rounded-lg">
                <span className="material-symbols-outlined text-slate-400 text-lg">info</span>
                <p className="text-sm text-slate-600">Đề tài đang chờ sinh viên chỉnh sửa và gửi lại.</p>
              </div>
              <button
                onClick={onClose}
                className="px-4 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-100 rounded-lg transition-colors"
              >
                Đóng
              </button>
            </div>
          )}

          {!isPendingMentorReview && !isPoolNeedsModification && !isDirectNeedsModification && !reviewAction && (
            <div className="flex justify-end">
              <button
                onClick={onClose}
                className="px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 rounded-lg transition-colors"
              >
                Đóng
              </button>
            </div>
          )}
        </div>
      </motion.div>
    </motion.div>
  );
}

function InfoField({ label, value, icon }: { label: string; value: string; icon: string }) {
  return (
    <div className="flex items-start gap-2">
      <span className="material-symbols-outlined text-slate-400 text-[18px] mt-0.5">{icon}</span>
      <div>
        <p className="text-xs text-slate-400 font-medium">{label}</p>
        <p className="text-sm text-slate-700 font-medium">{value}</p>
      </div>
    </div>
  );
}

function DetailSection({ title, content }: { title: string; content: string }) {
  return (
    <div>
      <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1.5">{title}</h4>
      <p className="text-sm text-slate-700 whitespace-pre-line leading-relaxed">{content}</p>
    </div>
  );
}

// ── Shared Components ────────────────────────────────────────────────────────

function EmptyState() {
  return (
    <div className="flex flex-col items-center gap-3 py-16 text-center">
      <span className="material-symbols-outlined text-5xl text-slate-300">topic</span>
      <p className="text-sm font-medium text-slate-400">Không có đề tài nào</p>
      <p className="text-xs text-slate-400">Thử thay đổi bộ lọc hoặc đề xuất đề tài mới.</p>
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

function TableSkeleton() {
  return (
    <div className="p-6 space-y-4">
      {[1, 2, 3, 4, 5].map((i) => (
        <div key={i} className="flex items-center gap-4 animate-pulse">
          <div className="h-4 w-28 bg-slate-200 rounded" />
          <div className="flex-1 space-y-2">
            <div className="h-4 w-3/4 bg-slate-200 rounded" />
            <div className="h-3 w-1/3 bg-slate-100 rounded" />
          </div>
          <div className="h-6 w-20 bg-slate-200 rounded-full" />
          <div className="h-6 w-20 bg-slate-200 rounded-full" />
          <div className="h-4 w-24 bg-slate-200 rounded" />
        </div>
      ))}
    </div>
  );
}
