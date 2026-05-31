import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
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

interface EvaluatorHistoryStatsDto {
  totalReviewed: number;
  approvedCount: number;
  needsModificationCount: number;
  rejectedCount: number;
}

interface EvaluatorHistoryItemDto {
  projectId: string;
  projectCode: string;
  projectNameVi: string;
  studentName: string;
  studentAvatar: string | null;
  evaluatedAt: string;
  result: string;
  feedback: string | null;
}

interface EvaluatorHistoryDto {
  stats: EvaluatorHistoryStatsDto;
  items: EvaluatorHistoryItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const PAGE_SIZE = 10;

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

const RESULT_DISPLAY: Record<string, { label: string; colors: string }> = {
  Approved: { label: "Đã duyệt", colors: "bg-green-50 text-green-600 border-green-200" },
  NeedsModification: { label: "Cần chỉnh sửa", colors: "bg-amber-50 text-amber-600 border-amber-200" },
  Rejected: { label: "Từ chối", colors: "bg-red-50 text-red-600 border-red-200" },
};

export function EvaluatorHistoryPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState("");
  const [dateRange, setDateRange] = useState("");
  const [result, setResult] = useState("");
  const [page, setPage] = useState(1);

  const [data, setData] = useState<EvaluatorHistoryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const { showError } = useSystemError();

  useEffect(() => {
    const timeout = setTimeout(
      () => {
        const params = new URLSearchParams();
        params.set("page", String(page));
        params.set("pageSize", String(PAGE_SIZE));
        if (search) params.set("search", search);
        if (dateRange) params.set("dateRange", dateRange);
        if (result) params.set("result", result);

        setLoading(true);
        apiClient
          .get<EvaluatorHistoryDto>(`/api/evaluator/history?${params}`)
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

  function renderPageButtons() {
    if (totalPages <= 1) return null;
    const buttons: React.ReactNode[] = [];

    buttons.push(
      <button
        key="prev"
        disabled={page <= 1}
        onClick={() => setPage((p) => Math.max(1, p - 1))}
        className="size-8 flex items-center justify-center rounded-lg border border-gray-200 hover:bg-gray-50 text-slate-500 disabled:opacity-50 transition-all"
      >
        <span className="material-symbols-outlined text-sm">chevron_left</span>
      </button>,
    );

    for (let i = 1; i <= Math.min(totalPages, 5); i++) {
      buttons.push(
        <button
          key={i}
          onClick={() => setPage(i)}
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
          onClick={() => setPage(totalPages)}
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
        onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
        className="size-8 flex items-center justify-center rounded-lg border border-gray-200 hover:bg-gray-50 text-slate-500 disabled:opacity-50 transition-all"
      >
        <span className="material-symbols-outlined text-sm">chevron_right</span>
      </button>,
    );

    return buttons;
  }

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
                    placeholder="Tên đề tài, mã..."
                    type="text"
                    value={search}
                    onChange={(e) => {
                      setSearch(e.target.value);
                      setPage(1);
                    }}
                  />
                </div>
              </div>
              <div className="md:col-span-2 flex flex-col gap-1.5">
                <label className="text-xs font-bold text-slate-500 uppercase tracking-wide">Thời gian</label>
                <select
                  className="w-full px-3 py-2.5 rounded-lg border border-gray-200 bg-white text-sm font-medium focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none cursor-pointer"
                  value={dateRange}
                  onChange={(e) => {
                    setDateRange(e.target.value);
                    setPage(1);
                  }}
                >
                  <option value="">Tất cả</option>
                  <option value="thisMonth">Tháng này</option>
                  <option value="thisWeek">Tuần này</option>
                  <option value="yesterday">Hôm qua</option>
                </select>
              </div>
              <div className="md:col-span-2 flex flex-col gap-1.5">
                <label className="text-xs font-bold text-slate-500 uppercase tracking-wide">Kết quả</label>
                <select
                  className="w-full px-3 py-2.5 rounded-lg border border-gray-200 bg-white text-sm font-medium focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none cursor-pointer"
                  value={result}
                  onChange={(e) => {
                    setResult(e.target.value);
                    setPage(1);
                  }}
                >
                  <option value="">Tất cả</option>
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
                              onClick={() => navigate(`/evaluator/review/${histItem.projectId}`)}
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
                <div className="flex items-center gap-2">{renderPageButtons()}</div>
              </div>
            )}
          </motion.div>
        </motion.div>
      </div>
    </>
  );
}
