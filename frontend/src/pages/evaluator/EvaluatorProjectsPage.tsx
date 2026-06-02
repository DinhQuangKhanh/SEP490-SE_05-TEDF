import React, { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { motion } from "framer-motion";
import { apiClient } from "@/lib/apiClient";
import { useSystemError } from "@/contexts/SystemErrorContext";

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.05 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

interface FilterOptionDto {
  value: number;
  label: string;
}

interface FilterOptionsResponse {
  semesters: FilterOptionDto[];
  majors: FilterOptionDto[];
}

interface EvaluatorProjectItemDto {
  assignmentId: string;
  projectId: string;
  projectCode: string;
  projectNameVi: string;
  majorName: string;
  studentName: string;
  studentAvatar: string | null;
  mentorName: string;
  submittedAt: string | null;
  assignedAt: string;
  individualResult: string;
  isUrgent: boolean;
}

interface EvaluatorProjectsDto {
  items: EvaluatorProjectItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const RESULT_DISPLAY: Record<string, { label: string; colors: string; animate: boolean }> = {
  Pending: { label: "Chờ duyệt", colors: "bg-blue-50 text-blue-600 border-blue-100", animate: true },
  Approved: { label: "Đã duyệt", colors: "bg-green-50 text-green-600 border-green-100", animate: false },
  NeedsModification: { label: "Cần chỉnh sửa", colors: "bg-amber-50 text-amber-600 border-amber-100", animate: false },
  Rejected: { label: "Từ chối", colors: "bg-red-50 text-red-600 border-red-200", animate: false },
};

const PAGE_SIZE = 10;

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

function getInitials(name: string): string {
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .slice(-2);
}

export function EvaluatorProjectsPage() {
  const navigate = useNavigate();
  const { showError } = useSystemError();
  const [searchParams, setSearchParams] = useSearchParams();

  // Đọc giá trị từ URL hoặc dùng giá trị mặc định
  const search = searchParams.get("search") || "";
  const semesterId = searchParams.get("semesterId") || "";
  const majorId = searchParams.get("majorId") || "";
  const result = searchParams.get("result") || "";
  const page = parseInt(searchParams.get("page") || "1", 10);

  const [data, setData] = useState<EvaluatorProjectsDto | null>(null);
  const [filterOptions, setFilterOptions] = useState<FilterOptionsResponse>({ semesters: [], majors: [] });
  const [loading, setLoading] = useState(true);

  // Fetch filter options once on mount
  useEffect(() => {
    apiClient
      .get<FilterOptionsResponse>("/api/evaluator/filter-options")
      .then(setFilterOptions)
      .catch((err) => showError(err.message));
  }, [showError]);

  // Helper function để cập nhật URL params
  function updateParams(updates: Record<string, string | number | null>) {
    const newParams = new URLSearchParams(searchParams);

    Object.entries(updates).forEach(([key, value]) => {
      if (value === null || value === "") {
        newParams.delete(key);
      } else {
        newParams.set(key, String(value));
      }
    });

    setSearchParams(newParams, { replace: true });
  }

  useEffect(() => {
    const timeout = setTimeout(
      () => {
        const params = new URLSearchParams();
        params.set("page", String(page));
        params.set("pageSize", String(PAGE_SIZE));
        if (search) params.set("search", search);
        if (semesterId) params.set("semesterId", semesterId);
        if (majorId) params.set("majorId", majorId);
        if (result) params.set("result", result);

        setLoading(true);
        apiClient
          .get<EvaluatorProjectsDto>(`/api/evaluator/projects?${params}`)
          .then(setData)
          .catch((err) => showError(err.message))
          .finally(() => setLoading(false));
      },
      search ? 400 : 0,
    );

    return () => clearTimeout(timeout);
  }, [search, semesterId, majorId, result, page, showError]);

  function clearFilters() {
    setSearchParams({}, { replace: true });
  }

  function handleRowAction(project: EvaluatorProjectItemDto) {
    navigate(`/evaluator/review/${project.projectId}`);
  }

  function handleDownload() {
    if (!data || data.items.length === 0) return;

    const headers = ["Mã đề tài", "Tên đề tài", "Chuyên ngành", "Sinh viên", "Mentor", "Ngày nộp", "Trạng thái"];
    const rows = data.items.map((p) => {
      const resultInfo = RESULT_DISPLAY[p.individualResult];
      return [
        p.projectCode,
        p.projectNameVi,
        p.majorName,
        p.studentName,
        p.mentorName,
        p.submittedAt ? formatDate(p.submittedAt) : "",
        resultInfo?.label ?? p.individualResult,
      ];
    });

    const csvContent = [headers, ...rows]
      .map((row) => row.map((cell) => `"${cell.replace(/"/g, '""')}"`).join(","))
      .join("\n");

    const blob = new Blob(["\uFEFF" + csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "danh-sach-de-tai.csv";
    a.click();
    URL.revokeObjectURL(url);
  }

  function handlePrint() {
    window.print();
  }

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / PAGE_SIZE);
  const from = totalCount === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const to = Math.min(page * PAGE_SIZE, totalCount);

  function renderPageButtons() {
    if (totalPages <= 1) return null;
    const buttons: React.ReactNode[] = [];

    buttons.push(
      <button
        key="prev"
        disabled={page <= 1}
        onClick={() => updateParams({ page: Math.max(1, page - 1) })}
        className="size-8 flex items-center justify-center rounded-lg border border-gray-200 hover:bg-gray-50 text-slate-500 disabled:opacity-50 transition-all"
      >
        <span className="material-symbols-outlined text-sm">chevron_left</span>
      </button>,
    );

    for (let i = 1; i <= Math.min(totalPages, 5); i++) {
      buttons.push(
        <button
          key={i}
          onClick={() => updateParams({ page: i })}
          className={`size-8 flex items-center justify-center rounded-lg text-xs font-bold transition-all ${
            page === i
              ? "bg-primary text-white shadow-md shadow-primary/20"
              : "border border-gray-200 hover:bg-gray-50 text-slate-500"
          }`}
        >
          {i}
        </button>,
      );
    }

    if (totalPages > 5) {
      buttons.push(
        <button
          key="ellipsis"
          className="size-8 flex items-center justify-center rounded-lg border border-gray-200 text-slate-500 text-xs font-bold"
        >
          ...
        </button>,
      );
      buttons.push(
        <button
          key={totalPages}
          onClick={() => updateParams({ page: totalPages })}
          className={`size-8 flex items-center justify-center rounded-lg text-xs font-bold transition-all ${
            page === totalPages
              ? "bg-primary text-white shadow-md shadow-primary/20"
              : "border border-gray-200 hover:bg-gray-50 text-slate-500"
          }`}
        >
          {totalPages}
        </button>,
      );
    }

    buttons.push(
      <button
        key="next"
        disabled={page >= totalPages}
        onClick={() => updateParams({ page: Math.min(totalPages, page + 1) })}
        className="size-8 flex items-center justify-center rounded-lg border border-gray-200 hover:bg-gray-50 text-slate-500 disabled:opacity-50 transition-all"
      >
        <span className="material-symbols-outlined text-sm">chevron_right</span>
      </button>,
    );

    return buttons;
  }

  const activeSemesterLabel = semesterId
    ? filterOptions.semesters.find((s) => String(s.value) === semesterId)?.label
    : null;

  return (
    <>
      {/* Header */}
      <header className="bg-primary px-8 py-6 shrink-0 shadow-lg z-10">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 w-full">
          <div className="flex flex-col gap-1">
            <h2 className="text-white text-2xl font-bold tracking-tight flex items-center gap-2">
              <span className="material-symbols-outlined">folder_shared</span>
              Danh sách đề tài
            </h2>
            <p className="text-blue-100/80 text-sm">Quản lý và thẩm định tất cả các đề tài đồ án được phân công.</p>
          </div>
          {activeSemesterLabel && (
            <div className="hidden md:flex items-center bg-primary-dark/50 rounded-lg px-4 py-2 border border-blue-400/30">
              <span className="text-blue-100 text-xs font-semibold uppercase tracking-wider mr-2">Học kỳ:</span>
              <span className="text-white text-sm font-bold">{activeSemesterLabel}</span>
            </div>
          )}
        </div>
      </header>

      {/* Main Content */}
      <div className="w-full p-6 md:p-8 flex flex-col gap-6 flex-1">
        <motion.div variants={container} initial="hidden" animate="show" className="flex flex-col gap-6">
          {/* Filters */}
          <motion.div variants={item} className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
            <div className="grid grid-cols-1 md:grid-cols-12 gap-4 items-end">
              <div className="md:col-span-3 flex flex-col gap-1.5">
                <label className="text-xs font-bold text-slate-500 uppercase tracking-wide">Tìm kiếm</label>
                <div className="relative group">
                  <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 group-focus-within:text-primary transition-colors">
                    search
                  </span>
                  <input
                    className="w-full pl-10 pr-4 py-2.5 rounded-lg border border-gray-200 bg-gray-50 text-sm font-medium focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all outline-none"
                    placeholder="Mã đề tài, Tên đề tài..."
                    type="text"
                    value={search}
                    onChange={(e) => {
                      updateParams({ search: e.target.value, page: 1 });
                    }}
                  />
                </div>
              </div>
              <div className="md:col-span-2 flex flex-col gap-1.5">
                <label className="text-xs font-bold text-slate-500 uppercase tracking-wide">Kỳ học</label>
                <select
                  className="w-full px-3 py-2.5 rounded-lg border border-gray-200 bg-white text-sm font-medium focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none cursor-pointer"
                  value={semesterId}
                  onChange={(e) => {
                    updateParams({ semesterId: e.target.value, page: 1 });
                  }}
                >
                  <option value="">Tất cả</option>
                  {filterOptions.semesters.map((s) => (
                    <option key={s.value} value={s.value}>
                      {s.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="md:col-span-2 flex flex-col gap-1.5">
                <label className="text-xs font-bold text-slate-500 uppercase tracking-wide">Chuyên ngành</label>
                <select
                  className="w-full px-3 py-2.5 rounded-lg border border-gray-200 bg-white text-sm font-medium focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none cursor-pointer"
                  value={majorId}
                  onChange={(e) => {
                    updateParams({ majorId: e.target.value, page: 1 });
                  }}
                >
                  <option value="">Tất cả</option>
                  {filterOptions.majors.map((m) => (
                    <option key={m.value} value={m.value}>
                      {m.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="md:col-span-2 flex flex-col gap-1.5">
                <label className="text-xs font-bold text-slate-500 uppercase tracking-wide">Trạng thái</label>
                <select
                  className="w-full px-3 py-2.5 rounded-lg border border-gray-200 bg-white text-sm font-medium focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none cursor-pointer"
                  value={result}
                  onChange={(e) => {
                    updateParams({ result: e.target.value, page: 1 });
                  }}
                >
                  <option value="">Tất cả</option>
                  <option value="Pending">Chờ duyệt</option>
                  <option value="Approved">Đã duyệt</option>
                  <option value="NeedsModification">Cần chỉnh sửa</option>
                  <option value="Rejected">Từ chối</option>
                </select>
              </div>
              <div className="md:col-span-3 flex justify-end gap-2">
                <button
                  onClick={clearFilters}
                  className="flex-1 md:flex-none h-[42px] px-4 rounded-lg border border-gray-200 text-slate-500 font-semibold text-sm hover:bg-gray-50 transition-colors flex items-center justify-center gap-2"
                >
                  <span className="material-symbols-outlined text-[18px]">filter_alt_off</span>
                  Xóa lọc
                </button>
              </div>
            </div>
          </motion.div>

          {/* Table */}
          <motion.div
            variants={item}
            className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden flex flex-col flex-1"
          >
            <div className="px-6 py-4 border-b border-gray-100 flex justify-between items-center">
              <div className="flex items-center gap-2">
                <span className="flex items-center justify-center size-6 rounded bg-primary/10 text-primary text-xs font-bold">
                  {totalCount}
                </span>
                <h3 className="text-slate-900 text-base font-bold">Danh sách đề tài cần thẩm định</h3>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={handleDownload}
                  className="p-2 rounded-lg hover:bg-gray-100 text-slate-500 transition-colors"
                  title="Tải xuống CSV"
                >
                  <span className="material-symbols-outlined text-[20px]">download</span>
                </button>
                <button
                  onClick={handlePrint}
                  className="p-2 rounded-lg hover:bg-gray-100 text-slate-500 transition-colors"
                  title="In"
                >
                  <span className="material-symbols-outlined text-[20px]">print</span>
                </button>
              </div>
            </div>
            <div className="overflow-x-auto">
              {loading ? (
                <div className="flex items-center justify-center py-16 text-slate-400 gap-3">
                  <span className="material-symbols-outlined animate-spin">progress_activity</span>
                  <span className="text-sm">Đang tải...</span>
                </div>
              ) : items.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-16 text-slate-400 gap-2">
                  <span className="material-symbols-outlined text-4xl">folder_off</span>
                  <p className="text-sm font-medium">Không tìm thấy đề tài nào</p>
                </div>
              ) : (
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="bg-gray-50/80 border-b border-gray-100">
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">
                        Mã đề tài
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider w-1/4">
                        Tên đề tài
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                        Sinh viên
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                        Mentor
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">
                        Ngày nộp
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider text-center">
                        Trạng thái
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider text-right sticky right-0 bg-gray-50/80 shadow-[-10px_0_10px_-10px_rgba(0,0,0,0.05)]">
                        Thao tác
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {items.map((project) => {
                      const resultInfo = RESULT_DISPLAY[project.individualResult];
                      const isPending = project.individualResult === "Pending";
                      return (
                        <motion.tr
                          key={project.assignmentId}
                          whileHover={{ backgroundColor: "rgb(239 246 255 / 0.3)" }}
                          className="group transition-colors"
                        >
                          <td className="px-6 py-4 whitespace-nowrap">
                            <span className="font-mono text-xs font-bold text-slate-500 bg-gray-100 px-2 py-1 rounded">
                              {project.projectCode}
                            </span>
                          </td>
                          <td className="px-6 py-4">
                            <div className="flex flex-col">
                              <span className="text-slate-900 font-bold text-sm line-clamp-2">
                                {project.projectNameVi}
                              </span>
                              {project.isUrgent ? (
                                <span className="text-xs text-red-500 font-bold mt-1 flex items-center gap-1">
                                  <span className="material-symbols-outlined text-[14px]">priority_high</span>
                                  Ưu tiên cao
                                </span>
                              ) : (
                                <span className="text-xs text-slate-500 mt-1">Chuyên ngành: {project.majorName}</span>
                              )}
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="flex items-center gap-3">
                              {project.studentAvatar ? (
                                <div
                                  className="size-8 rounded-full bg-cover ring-1 ring-gray-100"
                                  style={{ backgroundImage: `url('${project.studentAvatar}')` }}
                                />
                              ) : (
                                <div className="size-8 rounded-full bg-primary/10 text-primary flex items-center justify-center font-bold text-xs ring-1 ring-primary/10">
                                  {project.studentName ? getInitials(project.studentName) : "?"}
                                </div>
                              )}
                              <span className="text-slate-900 font-medium text-sm">{project.studentName || "—"}</span>
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <span className="text-sm text-slate-900 font-medium">{project.mentorName || "—"}</span>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <span className="text-slate-500 text-sm font-medium">
                              {project.submittedAt ? formatDate(project.submittedAt) : "—"}
                            </span>
                          </td>
                          <td className="px-6 py-4 text-center whitespace-nowrap">
                            <span
                              className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border ${resultInfo?.colors ?? "bg-gray-100 text-gray-600 border-gray-200"}`}
                            >
                              {resultInfo?.animate && (
                                <span className="size-1.5 rounded-full bg-blue-500 animate-pulse" />
                              )}
                              {resultInfo?.label ?? project.individualResult}
                            </span>
                          </td>
                          <td className="px-6 py-4 text-right sticky right-0 bg-white group-hover:bg-blue-50/30 transition-colors shadow-[-10px_0_10px_-10px_rgba(0,0,0,0.05)]">
                            <button
                              onClick={() => handleRowAction(project)}
                              className={`inline-flex items-center justify-center h-8 px-4 text-xs font-bold rounded-lg transition-all ${
                                isPending
                                  ? "bg-primary text-white hover:bg-primary-dark shadow-sm shadow-primary/20 hover:shadow-md hover:-translate-y-0.5"
                                  : "bg-white border border-gray-200 text-slate-900 hover:bg-gray-50 hover:border-primary/50 hover:text-primary"
                              }`}
                            >
                              {isPending
                                ? "Thẩm định"
                                : project.individualResult === "Approved"
                                  ? "Xem lại"
                                  : "Chi tiết"}
                            </button>
                          </td>
                        </motion.tr>
                      );
                    })}
                  </tbody>
                </table>
              )}
            </div>
            {/* Pagination */}
            {!loading && totalCount > 0 && (
              <div className="px-6 py-4 border-t border-gray-100 flex items-center justify-between bg-white shrink-0">
                <p className="text-xs text-slate-500 font-medium">
                  Hiển thị <span className="font-bold text-slate-900">{from}</span> đến{" "}
                  <span className="font-bold text-slate-900">{to}</span> trong tổng số{" "}
                  <span className="font-bold text-slate-900">{totalCount}</span> đề tài
                </p>
                <div className="flex items-center gap-2">{renderPageButtons()}</div>
              </div>
            )}
          </motion.div>
        </motion.div>
      </div>
    </>
  );
}
