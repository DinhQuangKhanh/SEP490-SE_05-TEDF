import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { motion } from "framer-motion";
import { evaluatorService } from "@/lib";
import type { EvaluatorFilterOptionsResponse, EvaluatorProjectItemDto, EvaluatorProjectsResponse } from "@/types";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { fadeContainer as container, fadeItem as item, formatDate } from "@/lib/common/ui";
import { EvaluatorPagination } from "@/components/lecturer/EvaluatorPagination";
import { EvaluatorFilterBar } from "@/components/lecturer/EvaluatorFilterBar";

const RESULT_DISPLAY: Record<string, { label: string; colors: string; animate: boolean }> = {
  Pending: { label: "Chờ duyệt", colors: "bg-blue-50 text-blue-600 border-blue-100", animate: true },
  Approved: { label: "Đã duyệt", colors: "bg-green-50 text-green-600 border-green-100", animate: false },
  NeedsModification: { label: "Cần chỉnh sửa", colors: "bg-amber-50 text-amber-600 border-amber-100", animate: false },
  Rejected: { label: "Từ chối", colors: "bg-red-50 text-red-600 border-red-200", animate: false },
};

const PAGE_SIZE = 10;

function getInitials(name: string): string {
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .slice(-2);
}

export function LecturerModerationPage() {
  const navigate = useNavigate();
  const { showError } = useSystemError();
  const [searchParams, setSearchParams] = useSearchParams();

  // Đọc giá trị từ URL hoặc dùng giá trị mặc định
  const search = searchParams.get("search") || "";
  const semesterId = searchParams.get("semesterId") || "";
  const majorId = searchParams.get("majorId") || "";
  const result = searchParams.get("result") || "";
  const page = parseInt(searchParams.get("page") || "1", 10);

  const [data, setData] = useState<EvaluatorProjectsResponse | null>(null);
  const [filterOptions, setFilterOptions] = useState<EvaluatorFilterOptionsResponse>({ semesters: [], majors: [] });
  const [loading, setLoading] = useState(true);

  // Fetch filter options once on mount
  useEffect(() => {
    evaluatorService
      .getFilterOptions()
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
        setLoading(true);
        evaluatorService
          .getProjects({
            page,
            pageSize: PAGE_SIZE,
            search: search || undefined,
            semesterId: semesterId ? Number(semesterId) : undefined,
            majorId: majorId ? Number(majorId) : undefined,
            result: result || undefined,
          })
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
    navigate(`/lecturer/moderate/${project.projectId}`);
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
          <EvaluatorFilterBar
            search={search}
            onSearch={(v) => updateParams({ search: v, page: 1 })}
            searchPlaceholder="Mã đề tài, Tên đề tài..."
            onClear={clearFilters}
            selects={[
              {
                label: "Kỳ học",
                value: semesterId,
                onChange: (v) => updateParams({ semesterId: v, page: 1 }),
                colSpan: 2,
                options: [
                  { value: "", label: "Tất cả" },
                  ...filterOptions.semesters.map((s) => ({ value: String(s.value), label: s.label })),
                ],
              },
              {
                label: "Trạng thái",
                value: result,
                onChange: (v) => updateParams({ result: v, page: 1 }),
                colSpan: 3,
                options: [
                  { value: "", label: "Tất cả" },
                  { value: "Pending", label: "Chờ duyệt" },
                  { value: "Approved", label: "Đã duyệt" },
                  { value: "NeedsModification", label: "Cần chỉnh sửa" },
                  { value: "Rejected", label: "Từ chối" },
                ],
              },
            ]}
          />

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
                              className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold text-primary bg-primary/5 hover:bg-primary/10 rounded-lg transition-colors whitespace-nowrap"
                            >
                              <span className="material-symbols-outlined text-[16px]">visibility</span>
                              Xem chi tiết
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
                <EvaluatorPagination page={page} totalPages={totalPages} onPage={(p) => updateParams({ page: p })} />
              </div>
            )}
          </motion.div>
        </motion.div>
      </div>
    </>
  );
}
