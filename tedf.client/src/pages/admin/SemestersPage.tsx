import { useState, useEffect, useCallback } from "react";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { motion, AnimatePresence } from "framer-motion";
import { Header } from "@/components/layout";
import { CreateSemesterModal } from "@/components/admin/CreateSemesterModal";
import { EditSemesterModal } from "@/components/admin/EditSemesterModal";
import { apiClient } from "@/lib/apiClient";
import { SemesterDto, SemesterPhaseDto } from "@/types/admin.types";

// ---- Helpers ----
function formatDate(iso: string) {
  if (!iso) return "";
  return format(new Date(iso), "dd/MM/yyyy", { locale: vi });
}

function statusBadge(status: string) {
  switch (status) {
    case "Ongoing":
      return { bg: "bg-green-100 text-green-700 border-green-200", dot: "bg-green-600", label: "Đang diễn ra" };
    case "Ended":
      return { bg: "bg-slate-100 text-slate-600 border-slate-200", dot: "", label: "Đã kết thúc" };
    case "Upcoming":
      return { bg: "bg-blue-100 text-blue-700 border-blue-200", dot: "bg-blue-600", label: "Sắp tới" };
    default:
      return { bg: "bg-amber-100 text-amber-700 border-amber-200", dot: "bg-amber-500", label: status };
  }
}

function phaseIcon(type: string) {
  switch (type) {
    case "Registration":
      return "edit_note";
    case "Evaluation":
      return "fact_check";
    case "Implementation":
      return "science";
    case "Defense":
      return "school";
    default:
      return "event";
  }
}

function phaseStatus(status: string): "completed" | "current" | "pending" {
  if (status === "Completed") return "completed";
  if (status === "Active" || status === "InProgress") return "current";
  return "pending";
}

// ---- Animations ----
const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.1 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

// ---- Page ----
export function SemestersPage() {
  const [semesters, setSemesters] = useState<SemesterDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [selectedSemester, setSelectedSemester] = useState<SemesterDto | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [openDropdownId, setOpenDropdownId] = useState<number | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<{ id: number; name: string } | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const fetchSemesters = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      const query = statusFilter ? "?status=" + statusFilter : "";
      const data = await apiClient.get<SemesterDto[]>("/api/admin/semesters" + query);
      setSemesters(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tải danh sách kỳ học.");
    } finally {
      setIsLoading(false);
    }
  }, [statusFilter]);

  useEffect(() => {
    fetchSemesters();
  }, [fetchSemesters]);

  const openDeleteConfirm = (semester: SemesterDto) => {
    setDeleteError(null);
    setDeleteConfirm({ id: semester.id, name: semester.name });
    setOpenDropdownId(null);
  };

  const handleDelete = async () => {
    if (!deleteConfirm) return;
    setIsDeleting(true);
    setDeleteError(null);
    try {
      await apiClient.delete("/api/admin/semesters/" + deleteConfirm.id);
      setDeleteConfirm(null);
      fetchSemesters();
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } catch (err: any) {
      setDeleteError(err.message || "Xóa thất bại");
    } finally {
      setIsDeleting(false);
    }
  };

  const handleEdit = (semester: SemesterDto) => {
    setSelectedSemester(semester);
    setIsEditModalOpen(true);
    setOpenDropdownId(null);
  };

  const toggleDropdown = (id: number) => {
    if (openDropdownId === id) setOpenDropdownId(null);
    else setOpenDropdownId(id);
  };

  return (
    <div className="flex-1 overflow-y-auto p-8">
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 mb-8">
        <div>
          <h1 className="text-[28px] font-bold tracking-tight text-slate-800">Danh Sách Kỳ Học</h1>
          <p className="text-sm text-slate-500 mt-1">Quản lý các kỳ bảo vệ đồ án, tiến độ và mốc thời gian.</p>
        </div>

        <div className="flex flex-wrap items-center gap-3 w-full md:w-auto">
          {/* Status Filter */}
          <div className="relative flex-1 md:flex-none">
            <select
              title="Lọc trạng thái"
              className="w-full appearance-none bg-white border border-slate-200 text-slate-700 text-sm font-semibold rounded-lg pl-4 pr-10 py-2.5 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all cursor-pointer shadow-sm hover:border-slate-300"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
            >
              <option value="">Tất cả trạng thái</option>
              <option value="Upcoming">Sắp tới</option>
              <option value="Ongoing">Đang diễn ra</option>
              <option value="Ended">Đã kết thúc</option>
            </select>
            <span className="material-symbols-outlined absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none text-[20px]">
              expand_more
            </span>
          </div>

          <button className="flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-all shadow-sm hover:border-slate-300 flex-1 md:flex-none">
            <span className="material-symbols-outlined text-[18px]">filter_list</span>
            Báo cáo
          </button>

          <button
            onClick={() => setIsCreateModalOpen(true)}
            className="flex items-center justify-center gap-2 px-5 py-2.5 text-sm font-bold text-white bg-slate-700 border border-slate-800 rounded-lg hover:bg-slate-800 transition-all shadow-md shadow-slate-900/10 flex-1 md:flex-none"
          >
            <span className="material-symbols-outlined text-[20px]">add</span>
            Tạo kỳ học mới
          </button>
        </div>
      </div>

      {/* Loading */}
      {isLoading && (
        <motion.div variants={item} className="flex flex-col items-center justify-center gap-3 py-16">
          <span className="text-4xl material-symbols-outlined animate-spin text-primary">progress_activity</span>
          <p className="text-sm text-slate-500">Đang tải danh sách kỳ học...</p>
        </motion.div>
      )}

      {/* Error */}
      {error && !isLoading && (
        <motion.div variants={item} className="flex items-start gap-3 p-4 border border-red-200 rounded-lg bg-red-50">
          <span className="material-symbols-outlined text-red-600 text-[20px] mt-0.5">error</span>
          <div>
            <p className="text-sm font-semibold text-red-800">Lỗi tải dữ liệu</p>
            <p className="mt-1 text-xs text-red-600">{error}</p>
            <button
              onClick={fetchSemesters}
              className="mt-2 text-xs font-semibold text-red-700 underline hover:text-red-900"
            >
              Thử lại
            </button>
          </div>
        </motion.div>
      )}

      {/* Empty state */}
      {!isLoading && !error && semesters.length === 0 && (
        <motion.div variants={item} className="flex flex-col items-center justify-center gap-4 py-16 text-center">
          <div className="flex items-center justify-center w-16 h-16 rounded-full bg-slate-100">
            <span className="text-4xl material-symbols-outlined text-slate-400">calendar_month</span>
          </div>
          <div>
            <p className="text-lg font-bold text-slate-700">Chưa có kỳ học nào</p>
            <p className="mt-1 text-sm text-slate-500">Bấm "Tạo kỳ học mới" để bắt đầu.</p>
          </div>
        </motion.div>
      )}

      {/* Semester Cards */}
      {!isLoading && !error && semesters.length > 0 && (
        <div className="space-y-6">
          {semesters.map((semester) => {
            const badge = statusBadge(semester.status);
            const isActive = semester.status === "Ongoing";
            const currentPhase = semester.phases.find(
              (p: SemesterPhaseDto) => p.status === "Ongoing" || p.status === "InProgress",
            );

            return (
              <motion.div
                key={semester.id}
                variants={item}
                className={`bento-card rounded-lg overflow-hidden transition-all hover:shadow-md ${isActive ? "border-l-4 border-l-primary" : "opacity-90 hover:opacity-100"}`}
              >
                <div className="p-6">
                  {/* Card Header */}
                  <div className="flex flex-col justify-between gap-4 mb-6 lg:flex-row lg:items-start">
                    <div className="flex items-start gap-4">
                      <div
                        className={`w-12 h-12 rounded-lg flex items-center justify-center shrink-0 border ${isActive ? "bg-primary/10 text-primary border-primary/20" : "bg-slate-100 text-slate-500 border-slate-200"}`}
                      >
                        <span className="material-symbols-outlined text-[28px]">
                          {isActive ? "calendar_month" : "history"}
                        </span>
                      </div>
                      <div>
                        <div className="flex flex-wrap items-center gap-3">
                          <h3 className={`text-xl font-bold ${isActive ? "text-slate-800" : "text-slate-700"}`}>
                            {semester.name}
                          </h3>
                          <span
                            className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold border ${badge.bg}`}
                          >
                            {badge.dot && <span className={`w-1.5 h-1.5 rounded-full ${badge.dot}`}></span>}
                            {badge.label}
                          </span>
                          <span className="font-mono text-xs text-slate-400">{semester.code}</span>
                        </div>
                        <p className="mt-1 text-sm text-slate-500">
                          Thời gian:{" "}
                          <span className="font-medium text-slate-700">
                            {formatDate(semester.startDate)} - {formatDate(semester.endDate)}
                          </span>
                          {semester.academicYear && (
                            <span className="ml-2 text-xs text-slate-400">• Năm học: {semester.academicYear}</span>
                          )}
                        </p>
                        {semester.description && <p className="mt-1 text-xs text-slate-400">{semester.description}</p>}
                      </div>
                    </div>
                    <div className="relative flex items-center gap-2">
                      <button
                        onClick={() => toggleDropdown(semester.id)}
                        className="p-2 transition-colors rounded-full text-slate-400 hover:text-slate-600 hover:bg-slate-100"
                      >
                        <span className="material-symbols-outlined text-[20px]">more_vert</span>
                      </button>

                      {openDropdownId === semester.id && (
                        <div className="absolute right-0 z-10 w-48 py-1 mt-1 bg-white border rounded-md shadow-lg top-10 border-slate-200">
                          {semester.status === "Upcoming" && (
                            <>
                              <button
                                onClick={() => handleEdit(semester)}
                                className="flex items-center w-full gap-2 px-4 py-2 text-sm text-left text-slate-700 hover:bg-slate-50"
                              >
                                <span className="material-symbols-outlined text-[18px]">edit</span> Sửa thông tin
                              </button>
                              <button
                                onClick={() => openDeleteConfirm(semester)}
                                className="flex items-center w-full gap-2 px-4 py-2 text-sm text-left text-red-600 hover:bg-red-50"
                              >
                                <span className="material-symbols-outlined text-[18px]">delete</span> Xóa kỳ học
                              </button>
                            </>
                          )}
                          {semester.status !== "Upcoming" && (
                            <p className="px-4 py-2 text-xs italic text-slate-400">
                              Không thể chỉnh sửa kỳ học đã bắt đầu hoặc kết thúc.
                            </p>
                          )}
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Phases Timeline */}
                  {semester.phases.length > 0 && (
                    <div className="pt-6 border-t border-slate-100">
                      <div className="flex items-end justify-between mb-4">
                        <p className="flex items-center gap-2 text-xs font-semibold tracking-wider uppercase text-slate-400">
                          <span className="material-symbols-outlined text-[16px]">timeline</span>
                          Tiến độ giai đoạn
                        </p>
                        {currentPhase && (
                          <span className="px-3 py-1 text-sm font-bold border rounded text-primary bg-primary/5 border-primary/10">
                            Giai đoạn: {currentPhase.name}
                          </span>
                        )}
                      </div>
                      <div className="relative px-2 pt-6 pb-2">
                        {/* Track background */}
                        <div className="absolute left-0 z-0 w-full h-1 rounded-full top-[34px] bg-slate-100"></div>
                        {/* Track progress */}
                        {(() => {
                          const total = semester.phases.length;
                          let pct = 0;
                          if (total > 1) {
                            const currentIndex = semester.phases.findIndex(
                              (p: SemesterPhaseDto) => p.status === "Active" || p.status === "InProgress",
                            );
                            if (currentIndex !== -1) {
                              pct = (currentIndex / (total - 1)) * 100;
                            } else {
                              const completedCount = semester.phases.filter(
                                (p: SemesterPhaseDto) => p.status === "Completed",
                              ).length;
                              if (completedCount === total) pct = 100;
                              else if (completedCount > 0) pct = Math.round(((completedCount - 1) / (total - 1)) * 100);
                            }
                          } else if (total === 1) {
                            const completedCount = semester.phases.filter(
                              (p: SemesterPhaseDto) => p.status === "Completed",
                            ).length;
                            pct = completedCount === 1 ? 100 : 0;
                          }
                          return (
                            <div
                              className={`absolute top-[34px] left-0 h-1 rounded-full z-0 transition-all duration-1000 ease-in-out ${isActive ? "bg-green-600 shadow-[0_0_8px_rgba(22,163,74,0.5)]" : "bg-green-600"}`}
                              style={{ width: `${pct}%` }}
                            ></div>
                          );
                        })()}
                        <div className="relative z-10 flex justify-between w-full">
                          {semester.phases.map((phase: SemesterPhaseDto) => (
                            <TimelineStep
                              key={phase.id}
                              icon={phaseIcon(phase.type)}
                              label={phase.name}
                              status={phaseStatus(phase.status)}
                              info={
                                phase.status === "Completed"
                                  ? "Hoàn tất " + formatDate(phase.endDate)
                                  : phaseStatus(phase.status) === "current"
                                    ? "Đang diễn ra"
                                    : "Dự kiến " + formatDate(phase.startDate)
                              }
                            />
                          ))}
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              </motion.div>
            );
          })}
        </div>
      )}
      {/* Create/Edit Modals */}
      <CreateSemesterModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onCreated={fetchSemesters}
      />

      <EditSemesterModal
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        onUpdated={fetchSemesters}
        initialData={selectedSemester}
      />

      {/* Delete Confirmation Modal */}
      <AnimatePresence>
        {deleteConfirm && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm"
            onClick={() => !isDeleting && setDeleteConfirm(null)}
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.95, y: 20 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.95, y: 20 }}
              transition={{ type: "spring", damping: 25, stiffness: 300 }}
              onClick={(e) => e.stopPropagation()}
              className="w-full max-w-md overflow-hidden bg-white shadow-2xl rounded-xl"
            >
              <div className="p-6">
                <div className="flex items-center gap-4 mb-4">
                  <div className="flex items-center justify-center w-12 h-12 bg-red-100 rounded-full shrink-0">
                    <span className="material-symbols-outlined text-red-600 text-[28px]">delete_forever</span>
                  </div>
                  <div>
                    <h3 className="text-lg font-bold text-slate-800">Xóa kỳ học</h3>
                    <p className="text-sm text-slate-500 mt-0.5">Hành động này không thể hoàn tác.</p>
                  </div>
                </div>
                <p className="mb-1 text-sm text-slate-600">
                  Bạn có chắc chắn muốn xóa kỳ học <strong className="text-slate-800">{deleteConfirm.name}</strong>?
                </p>
                <p className="text-xs text-slate-400">Chỉ có thể xóa kỳ học chưa bắt đầu.</p>
                {deleteError && (
                  <div className="flex items-start gap-2 p-3 mt-4 border border-red-200 rounded-md bg-red-50">
                    <span className="material-symbols-outlined text-red-600 text-[18px] mt-0.5">error</span>
                    <p className="text-sm text-red-800">{deleteError}</p>
                  </div>
                )}
              </div>
              <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-slate-100 bg-slate-50/50">
                <button
                  onClick={() => setDeleteConfirm(null)}
                  disabled={isDeleting}
                  className="px-4 py-2 text-sm font-semibold transition-colors text-slate-600 hover:text-slate-800 disabled:opacity-50"
                >
                  Hủy bỏ
                </button>
                <button
                  onClick={handleDelete}
                  disabled={isDeleting}
                  className="flex items-center gap-2 px-5 py-2 text-sm font-bold text-white transition-all bg-red-600 rounded-md shadow-lg shadow-red-600/20 hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isDeleting ? (
                    <>
                      <span className="material-symbols-outlined animate-spin text-[18px]">progress_activity</span>Đang
                      xóa...
                    </>
                  ) : (
                    <>
                      <span className="material-symbols-outlined text-[18px]">delete</span>Xóa kỳ học
                    </>
                  )}
                </button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

function TimelineStep({
  icon,
  label,
  status,
  info,
}: {
  icon: string;
  label: string;
  status: "completed" | "current" | "pending";
  info: string;
}) {
  const isCurrent = status === "current";
  const isPending = status === "pending";
  const isCompleted = status === "completed";

  return (
    <div className={`flex flex-col items-center gap-2 group cursor-pointer ${isPending ? "opacity-60" : ""}`}>
      <div className="relative flex items-center justify-center">
        {/* Pulsing ring for current phase */}
        {isCurrent && <div className="absolute inset-0 rounded-full bg-green-500/40 animate-ping"></div>}

        <div
          className={`relative z-10 rounded-full flex items-center justify-center ring-4 ring-white transition-all duration-300 group-hover:scale-110 ${
            isCurrent
              ? "w-10 h-10 bg-white border-[3px] border-green-600 text-green-600 shadow-[0_0_15px_rgba(22,163,74,0.3)]"
              : isCompleted
                ? "w-8 h-8 bg-gradient-to-br from-green-500 to-green-700 text-white shadow-md"
                : "w-8 h-8 bg-slate-50 border-2 border-slate-200 text-slate-400"
          }`}
        >
          {isCompleted ? (
            <span className="material-symbols-outlined text-[16px] font-bold">check</span>
          ) : (
            <span className={`material-symbols-outlined ${isCurrent ? "text-[20px] font-bold" : "text-[16px]"}`}>
              {icon}
            </span>
          )}
        </div>
      </div>
      <div className="text-center mt-1">
        <p
          className={`text-xs font-bold uppercase ${isCurrent ? "text-sm text-green-700" : isPending ? "text-slate-500" : "text-slate-700"}`}
        >
          {label}
        </p>
        {isCurrent ? (
          <p className="text-[10px] text-green-700 font-medium bg-green-50 px-2 py-0.5 rounded mt-1 border border-green-200">
            {info}
          </p>
        ) : (
          <p className="text-[10px] text-slate-400 mt-0.5">{info}</p>
        )}
      </div>
    </div>
  );
}
