import { useEffect, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { useParams, useNavigate } from "react-router-dom";
import { apiClient } from "@/lib/apiClient";
import { useAuth } from "@/contexts/AuthContext";
import { NotificationDropdown } from "@/components/layout";
import {
  validateFiles,
  formatFileSize,
  ACCEPTED_TYPES,
  MAX_ATTACHMENTS,
  MAX_FILE_SIZE_BYTES,
  MAX_TOTAL_SIZE_BYTES,
} from "@/lib/fileUploadUtils";

// ─── Types ───────────────────────────────────────────────────────────────────

interface TopicPoolDto {
  id: string;
  code: string;
  name: string;
  description: string | null;
  majorId: number;
  statusName: string;
  maxActiveTopicsPerMentor: number;
  expirationSemesters: number;
}

interface TopicPoolStatisticsDto {
  poolId: string;
  poolCode: string;
  poolName: string;
  totalMentors: number;
  totalTopicsCount: number;
  activeTopicsCount: number;
  registeredTopicsCount: number;
  expiredTopicsCount: number;
}

interface TopicInPoolItemDto {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  description: string | null;
  technologies: string | null;
  majorId: number;
  majorName: string;
  majorCode: string;
  poolStatus: number;
  poolStatusName: string;
  maxStudents: number;
  mentorName: string;
  mentorId: string;
  createdAt: string;
}

interface GetTopicsInPoolResult {
  items: TopicInPoolItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

interface MentorSummaryDto {
  mentorId: string;
  fullName: string;
}

interface TopicDetailDto {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  nameAbbr: string;
  description: string;
  objectives: string;
  scope: string | null;
  technologies: string | null;
  expectedResults: string | null;
  majorId: number;
  majorName: string;
  majorCode: string;
  poolStatus: number;
  poolStatusName: string;
  maxStudents: number;
  mentors: MentorSummaryDto[];
  createdAt: string;
  updatedAt: string | null;
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

const POOL_STATUS_LABELS: Record<number, { label: string; cls: string }> = {
  0: { label: "Còn trống", cls: "bg-green-50 text-green-700 border-green-100" },
  1: { label: "Đã giữ chỗ", cls: "bg-amber-50 text-amber-700 border-amber-100" },
  2: { label: "Đã giao", cls: "bg-blue-50 text-blue-700 border-blue-100" },
  3: { label: "Hết hạn", cls: "bg-red-50 text-red-700 border-red-100" },
};

const container = { hidden: { opacity: 0 }, show: { opacity: 1, transition: { staggerChildren: 0.06 } } };
const item = { hidden: { opacity: 0, y: 16 }, show: { opacity: 1, y: 0 } };

// ─── ProposeTopicModal ────────────────────────────────────────────────────────

interface ProposeTopicModalProps {
  poolId: string;
  onClose: () => void;
  onSuccess: () => void;
}

function ProposeTopicModal({ poolId, onClose, onSuccess }: ProposeTopicModalProps) {
  const [form, setForm] = useState({
    nameVi: "",
    nameEn: "",
    nameAbbr: "",
    description: "",
    objectives: "",
    scope: "",
    technologies: "",
    expectedResults: "",
    maxStudents: 5,
  });
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [attachments, setAttachments] = useState<File[]>([]);
  const [attachmentWarnings, setAttachmentWarnings] = useState<string[]>([]);

  const handleFiles = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files) return;
    const incoming = Array.from(e.target.files);
    const { accepted, rejected } = validateFiles(attachments, incoming);

    if (accepted.length > 0) {
      setAttachments((prev) => [...prev, ...accepted]);
    }

    setAttachmentWarnings(rejected);
    e.target.value = ""; // allow re-selecting same file
  };

  const removeFile = (idx: number) => {
    setAttachments((prev) => prev.filter((_, i) => i !== idx));
    setAttachmentWarnings([]);
  };
  const set = (field: string, val: string | number) => setForm((prev) => ({ ...prev, [field]: val }));

  const handleSubmit = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    const preflight = validateFiles([], attachments);
    if (preflight.accepted.length !== attachments.length || preflight.rejected.length > 0) {
      setAttachmentWarnings(preflight.rejected);
      setError("Có tệp đính kèm không hợp lệ. Vui lòng kiểm tra lại trước khi gửi.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const formData = new FormData();
      formData.append("nameVi", form.nameVi);
      formData.append("nameEn", form.nameEn);
      formData.append("nameAbbr", form.nameAbbr);
      formData.append("description", form.description);
      formData.append("objectives", form.objectives);
      if (form.scope) formData.append("scope", form.scope);
      if (form.technologies) formData.append("technologies", form.technologies);
      if (form.expectedResults) formData.append("expectedResults", form.expectedResults);
      formData.append("maxStudents", form.maxStudents.toString());
      attachments.forEach((f) => formData.append("attachments", f));

      await apiClient.postForm(`/api/topic-pools/${poolId}/propose`, formData);
      onSuccess();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Đề xuất thất bại");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="bg-white rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto"
      >
        <div className="sticky top-0 z-10 flex items-center justify-between px-6 py-4 bg-white border-b border-slate-100">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center rounded-lg size-9 bg-primary/10 text-primary">
              <span className="material-symbols-outlined text-[18px]">add_circle</span>
            </div>
            <div>
              <h2 className="text-base font-bold text-slate-900">Đề xuất đề tài mới</h2>
              <p className="text-xs text-slate-400">Đề tài sẽ được lưu vào kho ngay sau khi gửi</p>
            </div>
          </div>
          <button onClick={onClose} className="transition-colors text-slate-400 hover:text-slate-600">
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="flex items-center gap-2 p-3 text-sm text-red-700 border border-red-200 rounded-lg bg-red-50">
              <span className="material-symbols-outlined text-[18px]">error</span>
              {error}
            </div>
          )}

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div className="md:col-span-2">
              <label className="block mb-1 text-xs font-semibold text-slate-600">Tên đề tài (Tiếng Việt) *</label>
              <input
                required
                value={form.nameVi}
                onChange={(e) => set("nameVi", e.target.value)}
                className="w-full input-field"
                placeholder="Ví dụ: Hệ thống quản lý luận văn tốt nghiệp"
              />
            </div>
            <div className="md:col-span-2">
              <label className="block mb-1 text-xs font-semibold text-slate-600">Tên đề tài (Tiếng Anh) *</label>
              <input
                required
                value={form.nameEn}
                onChange={(e) => set("nameEn", e.target.value)}
                className="w-full input-field"
                placeholder="E.g., Thesis Management System"
              />
            </div>
            <div>
              <label className="block mb-1 text-xs font-semibold text-slate-600">Tên viết tắt *</label>
              <input
                required
                value={form.nameAbbr}
                onChange={(e) => set("nameAbbr", e.target.value)}
                className="w-full input-field"
                placeholder="Ví dụ: TMS"
              />
            </div>
            <div>
              <label className="block mb-1 text-xs font-semibold text-slate-600">Số sinh viên tối đa *</label>
              <input
                type="number"
                min={1}
                max={5}
                required
                value={form.maxStudents}
                onChange={(e) => set("maxStudents", parseInt(e.target.value))}
                className="w-full input-field"
              />
            </div>
          </div>

          <div>
            <label className="block mb-1 text-xs font-semibold text-slate-600">Mô tả *</label>
            <textarea
              required
              rows={3}
              value={form.description}
              onChange={(e) => set("description", e.target.value)}
              className="w-full resize-none input-field"
              placeholder="Mô tả tổng quan về đề tài..."
            />
          </div>

          <div>
            <label className="block mb-1 text-xs font-semibold text-slate-600">Mục tiêu *</label>
            <textarea
              required
              rows={3}
              value={form.objectives}
              onChange={(e) => set("objectives", e.target.value)}
              className="w-full resize-none input-field"
              placeholder="Mục tiêu cần đạt được..."
            />
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div>
              <label className="block mb-1 text-xs font-semibold text-slate-600">Phạm vi (tùy chọn)</label>
              <textarea
                rows={2}
                value={form.scope}
                onChange={(e) => set("scope", e.target.value)}
                className="w-full resize-none input-field"
                placeholder="Phạm vi nghiên cứu..."
              />
            </div>
            <div>
              <label className="block mb-1 text-xs font-semibold text-slate-600">Công nghệ (tùy chọn)</label>
              <textarea
                rows={2}
                value={form.technologies}
                onChange={(e) => set("technologies", e.target.value)}
                className="w-full resize-none input-field"
                placeholder="React, .NET, SQL Server..."
              />
            </div>
          </div>

          <div>
            <label className="block mb-1 text-xs font-semibold text-slate-600">Kết quả dự kiến (tùy chọn)</label>
            <textarea
              rows={2}
              value={form.expectedResults}
              onChange={(e) => set("expectedResults", e.target.value)}
              className="w-full resize-none input-field"
              placeholder="Sản phẩm, báo cáo..."
            />
          </div>

          {/* ── File attachments ───────────────────────────── */}
          <div>
            <label className="block mb-1 text-xs font-semibold text-slate-600">
              Tài liệu đính kèm
              <span className="ml-1 font-normal text-slate-400">(tùy chọn)</span>
            </label>
            <p className="text-[11px] text-slate-400 mb-2">
              Guard phía client: tối đa {MAX_ATTACHMENTS} file, mỗi file ≤ {formatFileSize(MAX_FILE_SIZE_BYTES)}, tổng ≤{" "}
              {formatFileSize(MAX_TOTAL_SIZE_BYTES)}.
            </p>
            <label className="flex flex-col items-center justify-center gap-2 px-4 py-5 text-center transition-colors border-2 border-dashed rounded-lg cursor-pointer border-slate-200 bg-slate-50 hover:border-primary hover:bg-primary/5">
              <span className="text-2xl material-symbols-outlined text-slate-400">upload_file</span>
              <span className="text-xs text-slate-500">
                Kéo thả hoặc <span className="font-semibold text-primary">nhấn để chọn file</span>
              </span>
              <span className="text-[10px] text-slate-400">PDF, Word, Excel, PPT, ZIP, RAR, JPG, PNG</span>
              <input
                type="file"
                multiple
                accept={ACCEPTED_TYPES.join(",")}
                onChange={handleFiles}
                className="sr-only"
              />
            </label>

            {attachmentWarnings.length > 0 && (
              <div className="mt-2 rounded-lg border border-amber-200 bg-amber-50 p-2.5 text-[11px] text-amber-700 space-y-1">
                {attachmentWarnings.map((warn, idx) => (
                  <p key={idx}>{warn}</p>
                ))}
              </div>
            )}

            {attachments.length > 0 && (
              <ul className="mt-2 space-y-1">
                {attachments.map((f, i) => (
                  <li
                    key={i}
                    className="flex items-center gap-2 text-xs text-slate-700 bg-slate-50 rounded-lg px-3 py-1.5 border border-slate-100"
                  >
                    <span className="material-symbols-outlined text-[14px] text-primary">attach_file</span>
                    <span className="flex-1 truncate">{f.name}</span>
                    <span className="flex-shrink-0 text-slate-400">{(f.size / 1024).toFixed(0)} KB</span>
                    <button
                      type="button"
                      onClick={() => removeFile(i)}
                      className="transition-colors text-slate-400 hover:text-red-500"
                    >
                      <span className="material-symbols-outlined text-[14px]">close</span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm transition-colors border rounded-lg border-slate-200 text-slate-600 hover:bg-slate-50"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="inline-flex items-center gap-2 px-5 py-2 text-sm font-semibold text-white transition-all rounded-lg bg-primary hover:bg-primary/90 disabled:opacity-60"
            >
              {submitting ? (
                <>
                  <span className="material-symbols-outlined text-[16px] animate-spin">progress_activity</span>Đang
                  gửi...
                </>
              ) : (
                <>
                  <span className="material-symbols-outlined text-[16px]">send</span>Gửi đề xuất
                </>
              )}
            </button>
          </div>
        </form>
      </motion.div>
    </div>
  );
}

// ─── TopicDetailModal ─────────────────────────────────────────────────────────

interface TopicDetailModalProps {
  topicId: string;
  onClose: () => void;
}

function TopicDetailModal({ topicId, onClose }: TopicDetailModalProps) {
  const [detail, setDetail] = useState<TopicDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiClient
      .get<TopicDetailDto>(`/api/topics/${topicId}`)
      .then(setDetail)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false));
  }, [topicId]);

  const stat = detail ? POOL_STATUS_LABELS[detail.poolStatus] : null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="bg-white rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto"
      >
        {/* Header */}
        <div className="sticky top-0 z-10 flex items-center justify-between px-6 py-4 bg-white border-b border-slate-100">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center rounded-lg size-9 bg-primary/10 text-primary">
              <span className="material-symbols-outlined text-[18px]">description</span>
            </div>
            <div>
              <h2 className="text-base font-bold text-slate-900">
                {loading ? "Đang tải..." : (detail?.nameVi ?? "Chi tiết đề tài")}
              </h2>
              {detail && <p className="font-mono text-xs text-slate-400">{detail.code}</p>}
            </div>
          </div>
          <button onClick={onClose} className="transition-colors text-slate-400 hover:text-slate-600">
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        <div className="p-6 space-y-5">
          {loading && (
            <div className="space-y-4 animate-pulse">
              <div className="w-3/4 h-4 rounded bg-slate-200" />
              <div className="w-1/2 h-4 rounded bg-slate-200" />
              <div className="h-20 rounded-lg bg-slate-100" />
            </div>
          )}
          {error && (
            <div className="flex items-center gap-2 p-3 text-sm text-red-700 border border-red-200 rounded-lg bg-red-50">
              <span className="material-symbols-outlined text-[18px]">error</span>
              {error}
            </div>
          )}
          {detail && (
            <>
              {/* Subtitle (EN) + status */}
              <div className="flex flex-wrap items-center gap-2">
                <p className="text-sm italic text-slate-500">{detail.nameEn}</p>
                {stat && (
                  <span className={`px-2 py-0.5 rounded-full text-xs font-bold border ${stat.cls}`}>{stat.label}</span>
                )}
              </div>

              {/* Grid info */}
              <div className="grid grid-cols-2 gap-3 text-sm">
                <div className="flex items-start gap-2">
                  <span className="material-symbols-outlined text-slate-400 text-[16px] mt-0.5">school</span>
                  <div>
                    <p className="text-xs text-slate-400">Chuyên ngành</p>
                    <p className="font-medium text-slate-800">{detail.majorName}</p>
                  </div>
                </div>
                <div className="flex items-start gap-2">
                  <span className="material-symbols-outlined text-slate-400 text-[16px] mt-0.5">group</span>
                  <div>
                    <p className="text-xs text-slate-400">Tối đa sinh viên</p>
                    <p className="font-medium text-slate-800">{detail.maxStudents} SV</p>
                  </div>
                </div>
              </div>

              {/* Sections */}
              {[
                { label: "Mô tả", value: detail.description, icon: "info" },
                { label: "Mục tiêu", value: detail.objectives, icon: "flag" },
                { label: "Phạm vi", value: detail.scope, icon: "explore" },
                { label: "Kết quả dự kiến", value: detail.expectedResults, icon: "verified" },
              ].map(
                (s) =>
                  s.value && (
                    <div key={s.label}>
                      <p className="flex items-center gap-1.5 text-xs font-semibold text-slate-500 mb-1.5">
                        <span className="material-symbols-outlined text-[14px]">{s.icon}</span>
                        {s.label}
                      </p>
                      <p className="p-3 text-sm leading-relaxed rounded-lg text-slate-700 bg-slate-50">{s.value}</p>
                    </div>
                  ),
              )}

              {detail.technologies && (
                <div>
                  <p className="flex items-center gap-1.5 text-xs font-semibold text-slate-500 mb-1.5">
                    <span className="material-symbols-outlined text-[14px]">code</span>
                    Công nghệ
                  </p>
                  <div className="flex flex-wrap gap-2">
                    {detail.technologies.split(",").map((t) => (
                      <span key={t} className="px-2.5 py-1 bg-primary/10 text-primary rounded-full text-xs font-medium">
                        {t.trim()}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              {/* Mentors */}
              {detail.mentors.length > 0 && (
                <div>
                  <p className="flex items-center gap-1.5 text-xs font-semibold text-slate-500 mb-2">
                    <span className="material-symbols-outlined text-[14px]">person</span>
                    Giảng viên hướng dẫn
                  </p>
                  <div className="flex flex-col gap-2">
                    {detail.mentors.map((m) => (
                      <div key={m.mentorId} className="flex items-center gap-2 text-sm text-slate-700">
                        <div className="flex items-center justify-center text-xs font-bold rounded-full size-8 bg-primary/10 text-primary">
                          {m.fullName.charAt(0)}
                        </div>
                        {m.fullName}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </motion.div>
    </div>
  );
}

// ─── TopicPoolDetailPage ──────────────────────────────────────────────────────

export function TopicPoolDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [pool, setPool] = useState<TopicPoolDto | null>(null);
  const [stats, setStats] = useState<TopicPoolStatisticsDto | null>(null);
  const [topics, setTopics] = useState<TopicInPoolItemDto[]>([]);
  const [topicsMeta, setTopicsMeta] = useState({ totalCount: 0, page: 1, totalPages: 1 });
  const [loading, setLoading] = useState(true);
  const [topicsLoading, setTopicsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [sortBy, setSortBy] = useState<string>("newest");
  const [page, setPage] = useState(1);

  // Modals
  const [selectedTopicId, setSelectedTopicId] = useState<string | null>(null);
  const [showProposeModal, setShowProposeModal] = useState(false);
  const [proposeSuccess, setProposeSuccess] = useState(false);

  // Load pool + stats once
  useEffect(() => {
    if (!id) return;
    setLoading(true);
    Promise.all([
      apiClient.get<TopicPoolDto>(`/api/topic-pools/${id}`),
      apiClient.get<TopicPoolStatisticsDto>(`/api/topic-pools/${id}/statistics`),
    ])
      .then(([poolData, statsData]) => {
        setPool(poolData);
        setStats(statsData);
      })
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false));
  }, [id]);

  // Load topics whenever filter/page changes
  useEffect(() => {
    if (!pool) return;
    setTopicsLoading(true);
    const params = new URLSearchParams({
      MajorId: pool.majorId.toString(),
      Page: page.toString(),
      PageSize: "10",
      SortBy: sortBy,
    });
    if (search.trim()) params.set("Search", search.trim());
    if (statusFilter) params.set("PoolStatus", statusFilter);

    apiClient
      .get<GetTopicsInPoolResult>(`/api/topics?${params}`)
      .then((data) => {
        setTopics(data.items);
        setTopicsMeta({ totalCount: data.totalCount, page: data.page, totalPages: data.totalPages });
      })
      .catch(() => {
        /* keep previous list */
      })
      .finally(() => setTopicsLoading(false));
  }, [pool, page, search, statusFilter, sortBy]);

  const statCards = stats
    ? [
        { label: "Tổng đề tài", value: stats.totalTopicsCount, icon: "description", color: "blue" },
        { label: "Còn trống", value: stats.activeTopicsCount, icon: "inventory_2", color: "green" },
        { label: "Đã đăng ký", value: stats.registeredTopicsCount, icon: "assignment_turned_in", color: "amber" },
        { label: "Đã hết hạn", value: stats.expiredTopicsCount, icon: "archive", color: "red" },
      ]
    : [];

  const isMentor = user?.role === "mentor";

  return (
    <>
      {/* Header */}
      <header className="z-50 flex items-center justify-between flex-shrink-0 h-16 px-8 bg-white border-b shadow-sm border-slate-200">
        <div className="flex items-center gap-2 text-slate-800">
          <button
            onClick={() => navigate("/mentor/topic-pools")}
            className="transition-colors text-slate-400 hover:text-primary"
          >
            <span className="material-symbols-outlined">arrow_back</span>
          </button>
          <span className="text-xl font-light text-slate-300">|</span>
          <span className="text-sm font-medium text-slate-400">Kho đề tài</span>
          <span className="text-sm material-symbols-outlined text-slate-400">chevron_right</span>
          <h2 className="max-w-xs text-lg font-bold truncate">
            {loading ? "Đang tải..." : (pool?.name ?? "Chi tiết kho")}
          </h2>
        </div>
        <div className="flex items-center gap-3">
          {isMentor && pool && (
            <button
              onClick={() => setShowProposeModal(true)}
              className="inline-flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white transition-all rounded-lg shadow-sm bg-primary hover:bg-primary/90"
            >
              <span className="material-symbols-outlined text-[16px]">add_circle</span>
              Đề xuất đề tài mới
            </button>
          )}
          <NotificationDropdown />
        </div>
      </header>

      {/* Content */}
      <div className="flex-1 p-8 overflow-y-auto bg-slate-50">
        {error ? (
          <div className="flex flex-col items-center justify-center gap-3 py-20 text-center">
            <span className="text-4xl text-red-400 material-symbols-outlined">error</span>
            <p className="font-medium text-slate-500">{error}</p>
            <button
              onClick={() => navigate("/mentor/topic-pools")}
              className="text-sm font-semibold text-primary hover:underline"
            >
              Quay lại danh sách
            </button>
          </div>
        ) : (
          <motion.div variants={container} initial="hidden" animate="show" className="space-y-6">
            {/* Pool info banner */}
            {pool && (
              <motion.div
                variants={item}
                className="flex flex-col items-start justify-between gap-4 p-6 bg-white border shadow-sm rounded-xl border-slate-200 md:flex-row md:items-center"
              >
                <div className="flex items-center gap-4">
                  <div className="flex items-center justify-center flex-shrink-0 size-14 rounded-xl bg-primary/10 text-primary">
                    <span className="text-2xl material-symbols-outlined">library_books</span>
                  </div>
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <h1 className="text-xl font-bold text-slate-900">{pool.name}</h1>
                      <span
                        className={`px-2 py-0.5 rounded-full text-xs font-bold border ${pool.statusName === "Active" ? "bg-green-50 text-green-700 border-green-100" : "bg-amber-50 text-amber-700 border-amber-100"}`}
                      >
                        {pool.statusName === "Active" ? "Đang mở" : "Tạm dừng"}
                      </span>
                    </div>
                    <div className="flex items-center gap-4 text-sm text-slate-500">
                      <span className="font-mono text-xs bg-slate-100 px-2 py-0.5 rounded">{pool.code}</span>
                      {pool.description && <span className="max-w-sm line-clamp-1">{pool.description}</span>}
                    </div>
                  </div>
                </div>
                <div className="flex gap-6 text-center">
                  <div>
                    <p className="text-xs text-slate-400 mb-0.5">Tối đa / GV</p>
                    <p className="text-lg font-bold text-slate-800">{pool.maxActiveTopicsPerMentor}</p>
                  </div>
                  <div>
                    <p className="text-xs text-slate-400 mb-0.5">Hết hạn (HK)</p>
                    <p className="text-lg font-bold text-slate-800">{pool.expirationSemesters}</p>
                  </div>
                </div>
              </motion.div>
            )}

            {/* Statistics */}
            {loading ? (
              <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
                {[1, 2, 3, 4].map((i) => (
                  <div key={i} className="p-5 bg-white border rounded-xl border-slate-200 animate-pulse">
                    <div className="w-1/2 h-3 mb-3 rounded bg-slate-200" />
                    <div className="w-1/3 h-8 rounded bg-slate-200" />
                  </div>
                ))}
              </div>
            ) : (
              <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
                {statCards.map((s) => (
                  <motion.div
                    key={s.label}
                    variants={item}
                    className="p-5 bg-white border shadow-sm rounded-xl border-slate-200"
                  >
                    <div className="flex items-center justify-between mb-3">
                      <p className="text-sm font-medium text-slate-500">{s.label}</p>
                      <div
                        className={`size-8 rounded-lg bg-${s.color}-50 text-${s.color}-600 flex items-center justify-center`}
                      >
                        <span className="material-symbols-outlined text-[18px]">{s.icon}</span>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-slate-900">{s.value}</p>
                  </motion.div>
                ))}
              </div>
            )}

            {/* Propose success banner */}
            <AnimatePresence>
              {proposeSuccess && (
                <motion.div
                  initial={{ opacity: 0, y: -10 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0 }}
                  className="flex items-center gap-3 p-4 text-green-700 border border-green-200 bg-green-50 rounded-xl"
                >
                  <span className="material-symbols-outlined">check_circle</span>
                  <p className="text-sm font-medium">Đề xuất đề tài thành công! Đề tài đã được thêm vào kho.</p>
                  <button
                    onClick={() => setProposeSuccess(false)}
                    className="ml-auto text-green-500 hover:text-green-700"
                  >
                    <span className="material-symbols-outlined text-[18px]">close</span>
                  </button>
                </motion.div>
              )}
            </AnimatePresence>

            {/* ── Topics section ─────────────────────────────────────────────────── */}
            <motion.div
              variants={item}
              className="overflow-hidden bg-white border shadow-sm rounded-xl border-slate-200"
            >
              {/* Section header + filters */}
              <div className="flex flex-col gap-3 px-6 py-4 border-b border-slate-100 md:flex-row md:items-center">
                <div className="flex items-center flex-1 gap-2">
                  <span className="material-symbols-outlined text-primary text-[20px]">list_alt</span>
                  <h2 className="font-bold text-slate-800">Danh sách đề tài</h2>
                  <span className="ml-1 text-xs font-medium text-slate-400">({topicsMeta.totalCount})</span>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  {/* Search */}
                  <div className="relative">
                    <span className="material-symbols-outlined absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400 text-[16px]">
                      search
                    </span>
                    <input
                      value={search}
                      onChange={(e) => {
                        setSearch(e.target.value);
                        setPage(1);
                      }}
                      className="pl-8 pr-3 py-1.5 rounded-lg bg-slate-100 text-sm text-slate-800 border-none focus:outline-none focus:ring-1 focus:ring-primary w-48"
                      placeholder="Tìm đề tài..."
                    />
                  </div>
                  {/* Status filter */}
                  <select
                    value={statusFilter}
                    onChange={(e) => {
                      setStatusFilter(e.target.value);
                      setPage(1);
                    }}
                    className="py-1.5 pl-2 pr-6 rounded-lg bg-slate-100 text-sm text-slate-700 border-none focus:outline-none"
                  >
                    <option value="">Tất cả trạng thái</option>
                    <option value="0">Còn trống</option>
                    <option value="1">Đã giữ chỗ</option>
                    <option value="2">Đã giao</option>
                    <option value="3">Hết hạn</option>
                  </select>
                  {/* Sort */}
                  <select
                    value={sortBy}
                    onChange={(e) => setSortBy(e.target.value)}
                    className="py-1.5 pl-2 pr-6 rounded-lg bg-slate-100 text-sm text-slate-700 border-none focus:outline-none"
                  >
                    <option value="newest">Mới nhất</option>
                    <option value="name">Tên</option>
                    <option value="mentor">Giảng viên</option>
                  </select>
                </div>
              </div>

              {/* Topics list */}
              {topicsLoading ? (
                <div className="divide-y divide-slate-100">
                  {[1, 2, 3, 4].map((i) => (
                    <div key={i} className="flex items-center gap-4 px-6 py-4 animate-pulse">
                      <div className="flex-shrink-0 rounded-lg size-8 bg-slate-200" />
                      <div className="flex-1 space-y-2">
                        <div className="w-2/3 h-4 rounded bg-slate-200" />
                        <div className="w-1/3 h-3 rounded bg-slate-100" />
                      </div>
                    </div>
                  ))}
                </div>
              ) : topics.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-16 text-center">
                  <span className="text-4xl material-symbols-outlined text-slate-300">description</span>
                  <p className="mt-3 text-sm font-medium text-slate-400">Chưa có đề tài nào trong kho này</p>
                  {isMentor && (
                    <button
                      onClick={() => setShowProposeModal(true)}
                      className="inline-flex items-center gap-2 px-4 py-2 mt-4 text-sm font-semibold transition-all rounded-lg bg-primary/10 text-primary hover:bg-primary/20"
                    >
                      <span className="material-symbols-outlined text-[16px]">add_circle</span>
                      Đề xuất đề tài đầu tiên
                    </button>
                  )}
                </div>
              ) : (
                <>
                  <div className="divide-y divide-slate-100">
                    {topics.map((t) => {
                      const st = POOL_STATUS_LABELS[t.poolStatus] ?? POOL_STATUS_LABELS[0];
                      return (
                        <div
                          key={t.id}
                          className="flex items-center gap-4 px-6 py-4 transition-colors cursor-pointer hover:bg-slate-50 group"
                          onClick={() => setSelectedTopicId(t.id)}
                        >
                          <div className="flex items-center justify-center flex-shrink-0 rounded-lg size-9 bg-primary/10 text-primary">
                            <span className="material-symbols-outlined text-[18px]">description</span>
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-semibold truncate text-slate-800">{t.nameVi}</p>
                            <div className="flex items-center gap-3 mt-0.5 text-xs text-slate-400">
                              <span className="font-mono">{t.code}</span>
                              <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-[12px]">person</span>
                                {t.mentorName}
                              </span>
                              <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-[12px]">group</span>
                                {t.maxStudents} SV
                              </span>
                            </div>
                          </div>
                          <div className="flex items-center flex-shrink-0 gap-3">
                            <span className={`px-2 py-0.5 rounded-full text-xs font-bold border ${st.cls}`}>
                              {st.label}
                            </span>
                            <button
                              onClick={() => setSelectedTopicId(t.id)}
                              className="flex items-center gap-1 text-xs font-semibold transition-opacity opacity-0 text-primary group-hover:opacity-100"
                            >
                              Xem chi tiết
                              <span className="material-symbols-outlined text-[13px]">arrow_forward</span>
                            </button>
                          </div>
                        </div>
                      );
                    })}
                  </div>

                  {/* Pagination */}
                  {topicsMeta.totalPages > 1 && (
                    <div className="flex items-center justify-between px-6 py-4 text-sm border-t border-slate-100 text-slate-500">
                      <span>
                        Hiển thị {topics.length}/{topicsMeta.totalCount} đề tài
                      </span>
                      <div className="flex items-center gap-2">
                        <button
                          disabled={page === 1}
                          onClick={() => setPage((p) => p - 1)}
                          className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors text-xs"
                        >
                          Trước
                        </button>
                        <span className="font-medium">
                          {page} / {topicsMeta.totalPages}
                        </span>
                        <button
                          disabled={page >= topicsMeta.totalPages}
                          onClick={() => setPage((p) => p + 1)}
                          className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors text-xs"
                        >
                          Sau
                        </button>
                      </div>
                    </div>
                  )}
                </>
              )}
            </motion.div>
          </motion.div>
        )}
      </div>

      {/* Modals */}
      <AnimatePresence>
        {selectedTopicId && (
          <TopicDetailModal key="detail" topicId={selectedTopicId} onClose={() => setSelectedTopicId(null)} />
        )}
        {showProposeModal && pool && (
          <ProposeTopicModal
            key="propose"
            poolId={pool.id}
            onClose={() => setShowProposeModal(false)}
            onSuccess={() => {
              setShowProposeModal(false);
              setProposeSuccess(true);
              setPage(1); // Refresh topic list
            }}
          />
        )}
      </AnimatePresence>
    </>
  );
}
