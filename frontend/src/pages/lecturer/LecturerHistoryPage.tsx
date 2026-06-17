import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { evaluatorService } from "@/lib";
import type { EvaluatorHistoryResponse } from "@/types";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { fadeContainer as container, fadeItem as item, formatDate } from "@/lib/common/ui";
import { EvaluatorPagination } from "@/components/lecturer/EvaluatorPagination";
import { EvaluatorFilterBar } from "@/components/lecturer/EvaluatorFilterBar";

const PAGE_SIZE = 10;

const RESULT_DISPLAY: Record<string, { label: string; colors: string }> = {
  Approved: { label: "Đã duyệt", colors: "bg-green-50 text-green-600 border-green-200" },
  NeedsModification: { label: "Cần chỉnh sửa", colors: "bg-amber-50 text-amber-600 border-amber-200" },
  Rejected: { label: "Từ chối", colors: "bg-red-50 text-red-600 border-red-200" },
};

export function LecturerHistoryPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState("");
  const [dateRange, setDateRange] = useState("");
  const [result, setResult] = useState("");
  const [page, setPage] = useState(1);

  const [data, setData] = useState<EvaluatorHistoryResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const { showError } = useSystemError();

  useEffect(() => {
    const timeout = setTimeout(
      () => {
        setLoading(true);
        evaluatorService
          .getHistory({
            page,
            pageSize: PAGE_SIZE,
            search: search || undefined,
            dateRange: dateRange || undefined,
            result: result || undefined,
          })
          .then(setData)
          .catch((err) => showError(err.message))
          .finally(() => setLoading(false));
      },
      search ? 400 : 0,
    );

    return () => clearTimeout(timeout);
  }, [search, dateRange, result, page, showError]);

  function clearFilters() {
    setSearch("");
    setDateRange("");
    setResult("");
    setPage(1);
  }

  const stats = data?.stats;
  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  const from = totalCount === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const to = Math.min(page * PAGE_SIZE, totalCount);

  return (
    <>
      {/* Header */}
      <header className="bg-white border-b border-gray-200 px-8 py-6 shrink-0">
        <div className="w-full flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex flex-col gap-1">
            <h2 className="text-slate-900 text-2xl font-bold tracking-tight flex items-center gap-2">
              <span className="material-symbols-outlined text-primary">history</span>
              Lịch sử thẩm định
            </h2>
            <p className="text-slate-500 text-sm">Xem lại các đề tài đã thẩm định và phản hồi của bạn.</p>
          </div>
          <div className="flex gap-3">
            <button className="flex items-center justify-center gap-2 h-10 px-4 rounded-lg border border-gray-200 bg-white text-slate-700 text-sm font-semibold hover:bg-gray-50 transition-colors">
              <span className="material-symbols-outlined text-[20px]">download</span>
              <span>Xuất Excel</span>
            </button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <div className="w-full p-6 md:p-8 flex flex-col gap-6 flex-1">
        <motion.div variants={container} initial="hidden" animate="show" className="flex flex-col gap-6">
          {/* Stats */}
          <motion.div variants={item} className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="bg-white rounded-xl border border-gray-200 p-5 flex items-center gap-4">
              <div className="size-12 rounded-xl bg-primary/10 text-primary flex items-center justify-center">
                <span className="material-symbols-outlined text-2xl">assignment_turned_in</span>
              </div>
              <div>
                <p className="text-2xl font-bold text-slate-900">
                  {loading && !stats ? "—" : (stats?.totalReviewed ?? 0)}
                </p>
                <p className="text-xs text-slate-500 font-medium">Tổng đã thẩm định</p>
              </div>
            </div>
            <div className="bg-white rounded-xl border border-gray-200 p-5 flex items-center gap-4">
              <div className="size-12 rounded-xl bg-green-50 text-green-600 flex items-center justify-center">
                <span className="material-symbols-outlined text-2xl">check_circle</span>
              </div>
              <div>
                <p className="text-2xl font-bold text-slate-900">
                  {loading && !stats ? "—" : (stats?.approvedCount ?? 0)}
                </p>
                <p className="text-xs text-slate-500 font-medium">Đã duyệt</p>
              </div>
            </div>
            <div className="bg-white rounded-xl border border-gray-200 p-5 flex items-center gap-4">
              <div className="size-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center">
                <span className="material-symbols-outlined text-2xl">edit_note</span>
              </div>
              <div>
                <p className="text-2xl font-bold text-slate-900">
                  {loading && !stats ? "—" : (stats?.needsModificationCount ?? 0)}
                </p>
                <p className="text-xs text-slate-500 font-medium">Cần chỉnh sửa</p>
              </div>
            </div>
            <div className="bg-white rounded-xl border border-gray-200 p-5 flex items-center gap-4">
              <div className="size-12 rounded-xl bg-red-50 text-red-600 flex items-center justify-center">
                <span className="material-symbols-outlined text-2xl">cancel</span>
              </div>
              <div>
                <p className="text-2xl font-bold text-slate-900">
                  {loading && !stats ? "—" : (stats?.rejectedCount ?? 0)}
                </p>
                <p className="text-xs text-slate-500 font-medium">Từ chối</p>
              </div>
            </div>
          </motion.div>

          {/* Filters */}
          <EvaluatorFilterBar
            search={search}
            onSearch={(v) => {
              setSearch(v);
              setPage(1);
            }}
            searchPlaceholder="Tên đề tài, mã..."
            onClear={clearFilters}
            selects={[
              {
                label: "Thời gian",
                value: dateRange,
                onChange: (v) => {
                  setDateRange(v);
                  setPage(1);
                },
                colSpan: 2,
                options: [
                  { value: "", label: "Tất cả" },
                  { value: "thisMonth", label: "Tháng này" },
                  { value: "thisWeek", label: "Tuần này" },
                  { value: "yesterday", label: "Hôm qua" },
                ],
              },
              {
                label: "Kết quả",
                value: result,
                onChange: (v) => {
                  setResult(v);
                  setPage(1);
                },
                colSpan: 2,
                options: [
                  { value: "", label: "Tất cả" },
                  { value: "Approved", label: "Đã duyệt" },
                  { value: "NeedsModification", label: "Cần chỉnh sửa" },
                  { value: "Rejected", label: "Từ chối" },
                ],
              },
            ]}
          />

          {/* History Table */}
          <motion.div
            variants={item}
            className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden flex flex-col flex-1"
          >
            <div className="overflow-x-auto">
              {loading ? (
                <div className="flex items-center justify-center py-16 text-slate-400 gap-3">
                  <span className="material-symbols-outlined animate-spin">progress_activity</span>
                  <span className="text-sm">Đang tải...</span>
                </div>
              ) : items.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-16 text-slate-400 gap-2">
                  <span className="material-symbols-outlined text-4xl">history</span>
                  <p className="text-sm font-medium">Không tìm thấy kết quả nào</p>
                </div>
              ) : (
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="bg-gray-50/80 border-b border-gray-100">
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider w-1/4">
                        Đề tài
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                        Sinh viên
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">
                        Ngày thẩm định
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider text-center">
                        Kết quả
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                        Phản hồi
                      </th>
                      <th className="px-6 py-4 text-[11px] font-bold text-slate-500 uppercase tracking-wider text-right">
                        Thao tác
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {items.map((histItem) => {
                      const resultInfo = RESULT_DISPLAY[histItem.result];
                      return (
                        <motion.tr
                          key={`${histItem.projectId}-${histItem.evaluatedAt}`}
                          whileHover={{ backgroundColor: "rgb(249 250 251)" }}
                          className="group transition-colors"
                        >
                          <td className="px-6 py-4">
                            <div className="flex flex-col">
                              <span className="text-slate-900 font-semibold text-sm line-clamp-1">
                                {histItem.projectNameVi}
                              </span>
                              <span className="text-xs text-slate-500 font-mono mt-1">{histItem.projectCode}</span>
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="flex items-center gap-2">
                              {histItem.studentAvatar ? (
                                <div
                                  className="size-7 rounded-full bg-cover ring-1 ring-gray-100"
                                  style={{ backgroundImage: `url('${histItem.studentAvatar}')` }}
                                />
                              ) : (
                                <div className="size-7 rounded-full bg-primary/10 text-primary flex items-center justify-center text-[10px] font-bold ring-1 ring-gray-100">
                                  {histItem.studentName.charAt(0)}
                                </div>
                              )}
                              <span className="text-slate-900 font-medium text-sm">{histItem.studentName || "—"}</span>
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <span className="text-slate-500 text-sm font-medium">
                              {formatDate(histItem.evaluatedAt)}
                            </span>
                          </td>
                          <td className="px-6 py-4 text-center whitespace-nowrap">
                            <span
                              className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border ${resultInfo?.colors ?? "bg-gray-100 text-gray-600 border-gray-200"}`}
                            >
                              {resultInfo?.label ?? histItem.result}
                            </span>
                          </td>
                          <td className="px-6 py-4">
                            <p className="text-sm text-slate-500 line-clamp-2 max-w-xs">{histItem.feedback || "—"}</p>
                          </td>
                          <td className="px-6 py-4 text-right whitespace-nowrap">
                            <button
                              onClick={() => navigate(`/lecturer/moderate/${histItem.projectId}`)}
                              className="inline-flex items-center justify-center h-8 px-4 bg-white border border-gray-200 text-slate-700 text-xs font-bold rounded-lg hover:bg-gray-50 hover:border-primary/50 hover:text-primary transition-all"
                            >
                              Chi tiết
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
                  <span className="font-bold text-slate-900">{totalCount}</span> bản ghi
                </p>
                <EvaluatorPagination page={page} totalPages={totalPages} onPage={setPage} />
              </div>
            )}
          </motion.div>
        </motion.div>
      </div>
    </>
  );
}
