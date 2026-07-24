import React, { useState, useEffect, useMemo } from "react";
import { motion } from "framer-motion";
import { Header } from "@/components/layout";

// ── Animation variants ─────────────────────────────────────────────────────
const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.05 } },
};
const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

// ── Action configuration ────────────────────────────────────────────────────
interface ActionConfig {
  label: string;
  icon: string;
  color: string;
  bg: string;
  border: string;
}

const ACTION_CONFIG: Record<string, ActionConfig> = {
  Submitted: {
    label: "Nộp đề tài",
    icon: "send",
    color: "text-blue-700",
    bg: "bg-blue-50",
    border: "border-blue-200",
  },
  Resubmitted: {
    label: "Nộp lại đề tài",
    icon: "replay",
    color: "text-indigo-700",
    bg: "bg-indigo-50",
    border: "border-indigo-200",
  },
  SubmittedToMentor: {
    label: "Nộp cho GVHD",
    icon: "forward_to_inbox",
    color: "text-cyan-700",
    bg: "bg-cyan-50",
    border: "border-cyan-200",
  },
  MentorApproved: {
    label: "GVHD duyệt",
    icon: "check_circle",
    color: "text-emerald-700",
    bg: "bg-emerald-50",
    border: "border-emerald-200",
  },
  MentorNeedsModification: {
    label: "GVHD yêu cầu chỉnh sửa",
    icon: "edit_note",
    color: "text-amber-700",
    bg: "bg-amber-50",
    border: "border-amber-200",
  },
  Approved: {
    label: "Hội đồng duyệt",
    icon: "verified",
    color: "text-green-700",
    bg: "bg-green-50",
    border: "border-green-200",
  },
  NeedsModification: {
    label: "Hội đồng yêu cầu chỉnh sửa",
    icon: "rate_review",
    color: "text-orange-700",
    bg: "bg-orange-50",
    border: "border-orange-200",
  },
  Rejected: {
    label: "Hội đồng từ chối",
    icon: "cancel",
    color: "text-red-700",
    bg: "bg-red-50",
    border: "border-red-200",
  },
  MentorAssigned: {
    label: "Phân công GVHD",
    icon: "assignment_ind",
    color: "text-purple-700",
    bg: "bg-purple-50",
    border: "border-purple-200",
  },
  EvaluatorAssigned: {
    label: "Phân công Evaluator",
    icon: "person_add",
    color: "text-indigo-700",
    bg: "bg-indigo-50",
    border: "border-indigo-200",
  },
  DocumentUploaded: {
    label: "Tải lên tài liệu",
    icon: "upload_file",
    color: "text-teal-700",
    bg: "bg-teal-50",
    border: "border-teal-200",
  },
  DocumentDeleted: {
    label: "Xóa tài liệu",
    icon: "delete",
    color: "text-rose-700",
    bg: "bg-rose-50",
    border: "border-rose-200",
  },
};

function getActionConfig(action: string): ActionConfig {
  return (
    ACTION_CONFIG[action] ?? {
      label: action,
      icon: "info",
      color: "text-slate-700",
      bg: "bg-slate-50",
      border: "border-slate-200",
    }
  );
}

// ── Mock Data ───────────────────────────────────────────────────────────────
interface AuditLogEntry {
  id: string;
  projectCode: string;
  projectName: string;
  action: string;
  performedByName: string;
  timestamp: string;
  metadata: Record<string, unknown> | null;
}

function generateMockData(): AuditLogEntry[] {
  const now = Date.now();
  const h = 3600000; // 1 hour in ms

  return [
    {
      id: "m01",
      projectCode: "POOL-SE-001",
      projectName: "Xây dựng hệ thống quản lý Đặt lịch khám bệnh viện",
      action: "Submitted",
      performedByName: "Nguyễn Văn An",
      timestamp: new Date(now - 168 * h).toISOString(),
      metadata: { submissionNumber: 1 },
    },
    {
      id: "m02",
      projectCode: "POOL-SE-001",
      projectName: "Xây dựng hệ thống quản lý Đặt lịch khám bệnh viện",
      action: "MentorAssigned",
      performedByName: "PGS.TS. Trần Minh Đức",
      timestamp: new Date(now - 165 * h).toISOString(),
      metadata: { mentorName: "TS. Lê Hoàng Nam" },
    },
    {
      id: "m03",
      projectCode: "POOL-SE-002",
      projectName: "Ứng dụng chăm sóc thú cưng sử dụng React Native",
      action: "Submitted",
      performedByName: "Trần Thị Bích",
      timestamp: new Date(now - 160 * h).toISOString(),
      metadata: { submissionNumber: 1 },
    },
    {
      id: "m03a",
      projectCode: "POOL-SE-001",
      projectName: "Xây dựng hệ thống quản lý Đặt lịch khám bệnh viện",
      action: "EvaluatorAssigned",
      performedByName: "PGS.TS. Trần Minh Đức",
      timestamp: new Date(now - 150 * h).toISOString(),
      metadata: { evaluatorName: "TS. Nguyễn Huy Hoàng", phaseId: 1 },
    },
    {
      id: "m04",
      projectCode: "POOL-SE-001",
      projectName: "Xây dựng hệ thống quản lý Đặt lịch khám bệnh viện",
      action: "MentorNeedsModification",
      performedByName: "TS. Lê Hoàng Nam",
      timestamp: new Date(now - 144 * h).toISOString(),
      metadata: { feedback: "Cần bổ sung phạm vi đề tài, làm rõ công nghệ sử dụng và kết quả mong đợi" },
    },
    {
      id: "m05",
      projectCode: "POOL-SE-003",
      projectName: "Website thương mại điện tử bán hàng trực tuyến",
      action: "SubmittedToMentor",
      performedByName: "Lê Văn Cường",
      timestamp: new Date(now - 130 * h).toISOString(),
      metadata: null,
    },
    {
      id: "m06",
      projectCode: "POOL-SE-002",
      projectName: "Ứng dụng chăm sóc thú cưng sử dụng React Native",
      action: "DocumentUploaded",
      performedByName: "Trần Thị Bích",
      timestamp: new Date(now - 120 * h).toISOString(),
      metadata: { documentType: "Proposal", fileName: "de_cuong_chi_tiet.pdf" },
    },
    {
      id: "m07",
      projectCode: "POOL-SE-001",
      projectName: "Xây dựng hệ thống quản lý Đặt lịch khám bệnh viện",
      action: "Resubmitted",
      performedByName: "Nguyễn Văn An",
      timestamp: new Date(now - 96 * h).toISOString(),
      metadata: { submissionNumber: 2 },
    },
    {
      id: "m08",
      projectCode: "POOL-SE-003",
      projectName: "Website thương mại điện tử bán hàng trực tuyến",
      action: "MentorApproved",
      performedByName: "PGS.TS. Trần Minh Đức",
      timestamp: new Date(now - 72 * h).toISOString(),
      metadata: { comment: "Đề tài đáp ứng yêu cầu, đồng ý duyệt" },
    },
    {
      id: "m09",
      projectCode: "POOL-SE-001",
      projectName: "Xây dựng hệ thống quản lý Đặt lịch khám bệnh viện",
      action: "MentorApproved",
      performedByName: "TS. Lê Hoàng Nam",
      timestamp: new Date(now - 48 * h).toISOString(),
      metadata: { comment: "Bản chỉnh sửa đạt yêu cầu" },
    },
    {
      id: "m10",
      projectCode: "POOL-SE-002",
      projectName: "Ứng dụng chăm sóc thú cưng sử dụng React Native",
      action: "NeedsModification",
      performedByName: "TS. Phạm Thị Hương",
      timestamp: new Date(now - 40 * h).toISOString(),
      metadata: { feedback: "Scope quá rộng, cần thu hẹp phạm vi và làm rõ tính khả thi" },
    },
    {
      id: "m11",
      projectCode: "POOL-SE-003",
      projectName: "Website thương mại điện tử bán hàng trực tuyến",
      action: "Approved",
      performedByName: "TS. Phạm Thị Hương",
      timestamp: new Date(now - 36 * h).toISOString(),
      metadata: null,
    },
    {
      id: "m12",
      projectCode: "POOL-SE-001",
      projectName: "Xây dựng hệ thống quản lý Đặt lịch khám bệnh viện",
      action: "Approved",
      performedByName: "TS. Phạm Thị Hương",
      timestamp: new Date(now - 24 * h).toISOString(),
      metadata: null,
    },
    {
      id: "m13",
      projectCode: "POOL-SE-002",
      projectName: "Ứng dụng chăm sóc thú cưng sử dụng React Native",
      action: "DocumentDeleted",
      performedByName: "Trần Thị Bích",
      timestamp: new Date(now - 20 * h).toISOString(),
      metadata: { fileName: "de_cuong_v1_cu.pdf", deletedBy: "Trần Thị Bích" },
    },
    {
      id: "m14",
      projectCode: "POOL-SE-002",
      projectName: "Ứng dụng chăm sóc thú cưng sử dụng React Native",
      action: "Resubmitted",
      performedByName: "Trần Thị Bích",
      timestamp: new Date(now - 12 * h).toISOString(),
      metadata: { submissionNumber: 2 },
    },
    {
      id: "m15",
      projectCode: "POOL-SE-004",
      projectName: "Hệ thống quản lý ký túc xá sinh viên",
      action: "Submitted",
      performedByName: "Phạm Đức Minh",
      timestamp: new Date(now - 6 * h).toISOString(),
      metadata: { submissionNumber: 1 },
    },
    {
      id: "m16",
      projectCode: "POOL-SE-002",
      projectName: "Ứng dụng chăm sóc thú cưng sử dụng React Native",
      action: "Rejected",
      performedByName: "TS. Phạm Thị Hương",
      timestamp: new Date(now - 2 * h).toISOString(),
      metadata: { reason: "Đề tài không đáp ứng tiêu chí kỹ thuật sau 2 lần chỉnh sửa" },
    },
    {
      id: "m17",
      projectCode: "POOL-SE-004",
      projectName: "Hệ thống quản lý ký túc xá sinh viên",
      action: "MentorAssigned",
      performedByName: "PGS.TS. Trần Minh Đức",
      timestamp: new Date(now - 1 * h).toISOString(),
      metadata: { mentorName: "TS. Nguyễn Quốc Bảo" },
    },
  ];
}

// ── Action filter tabs ──────────────────────────────────────────────────────
const ACTION_FILTERS = [
  { key: "", label: "Tất cả", icon: "list" },
  { key: "Submitted,Resubmitted,SubmittedToMentor", label: "Nộp đề tài", icon: "send" },
  { key: "MentorApproved,Approved", label: "Duyệt", icon: "check_circle" },
  { key: "MentorNeedsModification,NeedsModification", label: "Yêu cầu sửa", icon: "edit_note" },
  { key: "Rejected", label: "Từ chối", icon: "cancel" },
  { key: "MentorAssigned,EvaluatorAssigned", label: "Phân công", icon: "assignment_ind" },
  { key: "DocumentUploaded,DocumentDeleted", label: "Tài liệu", icon: "description" },
];

const PAGE_SIZE = 10;

// ── Page Component ──────────────────────────────────────────────────────────
export function ProjectAuditLogsPage() {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [actionFilter, setActionFilter] = useState("");
  const [page, setPage] = useState(1);
  const [expandedIds, setExpandedIds] = useState<string[]>([]);

  // Mock data (replace with API call when real data is available)
  const allLogs = useMemo(() => generateMockData(), []);

  // Debounce search
  useEffect(() => {
    const id = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(id);
  }, [search]);

  // Reset page when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, actionFilter]);

  // Filter logic
  const filteredLogs = useMemo(() => {
    let result = allLogs;

    if (debouncedSearch) {
      const q = debouncedSearch.toLowerCase();
      result = result.filter(
        (log) =>
          log.projectCode.toLowerCase().includes(q) ||
          log.projectName.toLowerCase().includes(q) ||
          log.performedByName.toLowerCase().includes(q)
      );
    }

    if (actionFilter) {
      const actions = actionFilter.split(",");
      result = result.filter((log) => actions.includes(log.action));
    }

    return result;
  }, [allLogs, debouncedSearch, actionFilter]);

  const totalCount = filteredLogs.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const pagedLogs = filteredLogs.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  // Summary stats
  const stats = useMemo(() => {
    const submitted = allLogs.filter((l) =>
      ["Submitted", "Resubmitted", "SubmittedToMentor"].includes(l.action)
    ).length;
    const approved = allLogs.filter((l) => ["MentorApproved", "Approved"].includes(l.action)).length;
    const revision = allLogs.filter((l) =>
      ["MentorNeedsModification", "NeedsModification"].includes(l.action)
    ).length;
    const rejected = allLogs.filter((l) => l.action === "Rejected").length;
    return { total: allLogs.length, submitted, approved, revision, rejected };
  }, [allLogs]);

  return (
    <>
      <Header title="Nhật Ký Thao Tác Đề Tài" showSearch={false} />

      <div className="flex-1 p-8 overflow-y-auto scrollbar-hide bg-slate-50">
        <motion.div variants={container} initial="hidden" animate="show" className="flex flex-col gap-6">
          {/* ── Mock data banner ──────────────────────────────── */}
          <motion.div
            variants={item}
            className="flex items-center gap-3 px-4 py-3 border rounded-lg bg-amber-50 border-amber-200"
          >
            <span className="material-symbols-outlined text-amber-600 text-[20px]">science</span>
            <div>
              <p className="text-sm font-semibold text-amber-800">Dữ liệu mẫu — Demo</p>
              <p className="text-xs text-amber-600">
                Đây là dữ liệu mẫu để minh họa giao diện. Khi hệ thống có dữ liệu thật, trang sẽ tự động hiển thị dữ liệu
                thật.
              </p>
            </div>
          </motion.div>

          {/* ── Summary Cards ────────────────────────────────── */}
          <motion.div variants={item} className="grid grid-cols-2 gap-4 lg:grid-cols-5">
            <StatCard label="Tổng thao tác" count={stats.total} icon="analytics" color="text-primary" bg="bg-primary/10" />
            <StatCard label="Nộp đề tài" count={stats.submitted} icon="send" color="text-blue-600" bg="bg-blue-50" />
            <StatCard
              label="Đã duyệt"
              count={stats.approved}
              icon="check_circle"
              color="text-emerald-600"
              bg="bg-emerald-50"
            />
            <StatCard
              label="Yêu cầu sửa"
              count={stats.revision}
              icon="edit_note"
              color="text-amber-600"
              bg="bg-amber-50"
            />
            <StatCard label="Từ chối" count={stats.rejected} icon="cancel" color="text-red-600" bg="bg-red-50" />
          </motion.div>

          {/* ── Filters ──────────────────────────────────────── */}
          <motion.div variants={item} className="flex flex-col gap-3">
            {/* Action filter tabs */}
            <div className="flex p-1 overflow-x-auto bg-white border rounded-lg shadow-sm border-slate-200 scrollbar-hide">
              {ACTION_FILTERS.map((tab) => (
                <button
                  key={tab.key}
                  onClick={() => setActionFilter(tab.key)}
                  className={`flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md whitespace-nowrap transition-all ${
                    actionFilter === tab.key
                      ? "bg-primary text-white shadow-sm"
                      : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                  }`}
                >
                  <span className="material-symbols-outlined text-[18px]">{tab.icon}</span>
                  {tab.label}
                </button>
              ))}
            </div>

            {/* Search */}
            <div className="relative w-full">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-400 text-[18px]">
                search
              </span>
              <input
                className="w-full py-2.5 pr-9 text-sm bg-white border rounded-lg pl-9 border-slate-200 shadow-sm focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary placeholder-slate-400 text-slate-700"
                placeholder="Tìm theo mã đề tài, tên đề tài, người thao tác..."
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
          </motion.div>

          {/* ── Table ─────────────────────────────────────────── */}
          <motion.div
            variants={item}
            className="flex flex-col flex-1 min-h-0 overflow-hidden bg-white rounded-xl border border-slate-200 shadow-sm"
          >
            <div className="flex-1 overflow-auto">
              <table className="w-full text-sm text-left text-slate-600">
                <thead className="sticky top-0 z-10 text-xs font-bold uppercase border-b bg-slate-50 text-slate-500 border-slate-200">
                  <tr>
                    <th className="px-6 py-4 w-40">Thời gian</th>
                    <th className="px-6 py-4 min-w-[250px]">Đề tài</th>
                    <th className="px-6 py-4 w-56">Hành động</th>
                    <th className="px-6 py-4 w-48">Người thực hiện</th>
                    <th className="px-6 py-4 w-16 text-center">Chi tiết</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {pagedLogs.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="px-6 py-20 text-center text-slate-400">
                        <span className="material-symbols-outlined text-[40px] mb-2 block text-slate-300">
                          search_off
                        </span>
                        Không tìm thấy bản ghi nào
                      </td>
                    </tr>
                  ) : (
                    pagedLogs.map((log) => {
                      const ac = getActionConfig(log.action);
                      const isExpanded = expandedIds.includes(log.id);
                      
                      const toggleExpand = (id: string) => {
                        setExpandedIds((prev) =>
                          prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]
                        );
                      };

                      return (
                        <React.Fragment key={log.id}>
                          <tr
                            className={`transition-colors hover:bg-slate-50/80 group cursor-pointer ${isExpanded ? "bg-slate-50/50" : ""}`}
                            onClick={() => toggleExpand(log.id)}
                          >
                            {/* Timestamp */}
                            <td className="px-6 py-4">
                              <div className="text-xs text-slate-500">{formatTimestamp(log.timestamp)}</div>
                              <div className="text-[10px] text-slate-400 mt-0.5">
                                {new Date(log.timestamp).toLocaleTimeString("vi-VN", {
                                  hour: "2-digit",
                                  minute: "2-digit",
                                })}
                              </div>
                            </td>

                            {/* Project */}
                            <td className="px-6 py-4">
                              <div className="flex items-start gap-2.5">
                                <div className="mt-0.5 bg-primary/10 text-primary rounded p-1.5 flex-shrink-0">
                                  <span className="material-symbols-outlined text-[16px]">description</span>
                                </div>
                                <div className="min-w-0">
                                  <p className="text-sm font-semibold text-slate-800 truncate group-hover:text-primary transition-colors">
                                    {log.projectName}
                                  </p>
                                  <p className="text-xs font-mono text-slate-400 mt-0.5">{log.projectCode}</p>
                                </div>
                              </div>
                            </td>

                            {/* Action */}
                            <td className="px-6 py-4">
                              <span
                                className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border whitespace-nowrap ${ac.bg} ${ac.color} ${ac.border}`}
                              >
                                <span className="material-symbols-outlined text-[14px]">{ac.icon}</span>
                                {ac.label}
                              </span>
                            </td>

                            {/* Performer */}
                            <td className="px-6 py-4">
                              <div className="flex items-center gap-2">
                                <div className="flex items-center justify-center w-7 h-7 text-[10px] font-bold rounded-full bg-primary/10 text-primary shrink-0">
                                  {getInitials(log.performedByName)}
                                </div>
                                <span className="text-sm text-slate-700 font-medium truncate">
                                  {log.performedByName}
                                </span>
                              </div>
                            </td>

                            {/* Expand */}
                            <td className="px-6 py-4 text-center">
                              {log.metadata ? (
                                <span
                                  className={`material-symbols-outlined text-[20px] text-slate-400 transition-transform ${
                                    isExpanded ? "rotate-180" : ""
                                  }`}
                                >
                                  expand_more
                                </span>
                              ) : (
                                <span className="text-slate-300">—</span>
                              )}
                            </td>
                          </tr>

                          {/* Expanded Details Row */}
                          {isExpanded && log.metadata && (
                            <tr>
                              <td colSpan={5} className="p-0 border-b border-slate-100">
                                <motion.div
                                  initial={{ height: 0, opacity: 0 }}
                                  animate={{ height: "auto", opacity: 1 }}
                                  exit={{ height: 0, opacity: 0 }}
                                  className="overflow-hidden bg-slate-50/60"
                                >
                                  <div className="px-6 py-4 border-l-2 border-primary ml-2 my-2 bg-white rounded-r-lg shadow-sm">
                                    <div className="flex items-start gap-2">
                                      <span className="material-symbols-outlined text-primary text-[18px] mt-0.5 shrink-0">
                                        info
                                      </span>
                                      <div className="space-y-1.5 text-sm">
                                        {Object.entries(log.metadata).map(([key, value]) => (
                                          <div key={key} className="flex items-baseline gap-2">
                                            <span className="text-xs font-medium text-slate-500 min-w-[120px]">
                                              {formatMetadataKey(key)}:
                                            </span>
                                            <span className="text-slate-800 font-medium">{String(value)}</span>
                                          </div>
                                        ))}
                                      </div>
                                    </div>
                                  </div>
                                </motion.div>
                              </td>
                            </tr>
                          )}
                        </React.Fragment>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between p-4 bg-white border-t border-slate-200 shrink-0">
                <span className="text-sm text-slate-500">
                  Hiển thị{" "}
                  <span className="font-medium text-slate-900">{(page - 1) * PAGE_SIZE + 1}</span> đến{" "}
                  <span className="font-medium text-slate-900">{Math.min(page * PAGE_SIZE, totalCount)}</span> trong số{" "}
                  <span className="font-medium text-slate-900">{totalCount}</span> bản ghi
                </span>
                <div className="flex items-center gap-1">
                  <button
                    disabled={page <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    className="px-3 py-1.5 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-md hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Trước
                  </button>
                  {getPageNumbers(page, totalPages).map((p, i) =>
                    p === "..." ? (
                      <span key={`dots-${i}`} className="px-2 py-1 text-slate-400">
                        ...
                      </span>
                    ) : (
                      <button
                        key={p}
                        onClick={() => setPage(p as number)}
                        className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                          p === page ? "bg-primary text-white" : "text-slate-600 border border-slate-200 hover:bg-slate-50"
                        }`}
                      >
                        {p}
                      </button>
                    )
                  )}
                  <button
                    disabled={page >= totalPages}
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    className="px-3 py-1.5 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-md hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Sau
                  </button>
                </div>
              </div>
            )}
          </motion.div>
        </motion.div>
      </div>
    </>
  );
}

// ── Helper Components ───────────────────────────────────────────────────────
function StatCard({
  label,
  count,
  icon,
  color,
  bg,
}: {
  label: string;
  count: number;
  icon: string;
  color: string;
  bg: string;
}) {
  return (
    <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-4 flex items-center gap-4">
      <div className={`${bg} p-2.5 rounded-lg`}>
        <span className={`material-symbols-outlined ${color} text-[22px]`}>{icon}</span>
      </div>
      <div>
        <p className="text-xl font-bold text-slate-800">{count}</p>
        <p className="text-xs text-slate-500">{label}</p>
      </div>
    </div>
  );
}

// ── Utilities ───────────────────────────────────────────────────────────────
function getInitials(name: string): string {
  return name
    .split(" ")
    .filter(Boolean)
    .map((w) => w[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
}

function formatTimestamp(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

function formatMetadataKey(key: string): string {
  const map: Record<string, string> = {
    submissionNumber: "Lần nộp",
    feedback: "Phản hồi",
    comment: "Ghi chú",
    reason: "Lý do",
    mentorName: "Giảng viên",
    documentType: "Loại tài liệu",
    fileName: "Tên file",
    deletedBy: "Người xóa",
    evaluatorName: "Evaluator",
    phaseId: "Giai đoạn (Phase)",
  };
  return map[key] ?? key;
}

function getPageNumbers(current: number, total: number): (number | "...")[] {
  if (total <= 5) return Array.from({ length: total }, (_, i) => i + 1);
  const pages: (number | "...")[] = [1];
  if (current > 3) pages.push("...");
  for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
    pages.push(i);
  }
  if (current < total - 2) pages.push("...");
  pages.push(total);
  return pages;
}
