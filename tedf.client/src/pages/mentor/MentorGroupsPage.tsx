import { motion } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import { NotificationDropdown } from "@/components/layout";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { studentGroupService, type MentorGroupDto } from "@/lib/studentGroupService";

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.08 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

export function MentorGroupsPage() {
  const navigate = useNavigate();
  const { showError } = useSystemError();
  const [groups, setGroups] = useState<MentorGroupDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    studentGroupService
      .getMentorGroups()
      .then(setGroups)
      .catch((err) => showError(err instanceof Error ? err.message : "Không thể tải danh sách nhóm"))
      .finally(() => setLoading(false));
  }, []);

  return (
    <>
      {/* Header */}
      <header className="z-50 flex items-center justify-between flex-shrink-0 h-16 px-8 border-b shadow-md bg-slate-800 border-slate-700">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2 text-white">
            <span className="text-sm font-medium text-slate-400">Quản lý</span>
            <span className="text-sm material-symbols-outlined text-slate-500">chevron_right</span>
            <h2 className="text-lg font-bold">Nhóm của tôi</h2>
          </div>
        </div>
        <div className="flex items-center gap-4">
          <div className="relative hidden md:block">
            <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
              <span className="material-symbols-outlined text-slate-400 text-[20px]">search</span>
            </div>
            <input
              type="text"
              className="block w-64 py-2 pl-10 pr-3 text-white transition-all border-none rounded-lg bg-slate-700 placeholder-slate-400 focus:outline-none focus:bg-slate-600 focus:ring-1 focus:ring-primary/50 sm:text-sm"
              placeholder="Tìm kiếm nhóm, đề tài..."
            />
          </div>
          <NotificationDropdown role="mentor" isNavy={true} />
        </div>
      </header>

      {/* Content */}
      <div className="flex-1 p-8 overflow-y-auto bg-slate-100">
        <motion.div variants={container} initial="hidden" animate="show" className="space-y-8">
          {/* Title */}
          <motion.div variants={item} className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Danh sách nhóm hướng dẫn</h1>
              <p className="mt-1 text-sm text-slate-500">Quản lý tiến độ và theo dõi các nhóm sinh viên</p>
            </div>
          </motion.div>

          {/* Loading State */}
          {loading && (
            <div className="flex items-center justify-center py-12">
              <div className="w-8 h-8 border-b-2 rounded-full animate-spin border-primary" />
            </div>
          )}

          {/* Empty State */}
          {!loading && groups.length === 0 && (
            <div className="p-12 text-center bg-white border rounded-xl border-slate-200">
              <span className="mb-3 text-5xl material-symbols-outlined text-slate-300">group_off</span>
              <h3 className="mb-1 text-lg font-bold text-slate-700">Chưa có nhóm nào</h3>
              <p className="text-sm text-slate-500">Bạn chưa được phân công hướng dẫn nhóm nào trong học kỳ này.</p>
            </div>
          )}

          {/* Groups Grid */}
          {!loading && groups.length > 0 && (
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-3">
              {groups.map((group) => (
                <motion.div
                  key={group.groupId}
                  variants={item}
                  className="flex flex-col overflow-hidden transition-shadow duration-200 bg-white border shadow-sm rounded-xl border-slate-200 hover:shadow-md"
                >
                  <div className="flex-1 p-5">
                    <div className="flex items-start justify-between mb-3">
                      <div>
                        <h3 className="text-lg font-bold text-slate-900">{group.groupName ?? group.groupCode}</h3>
                        <span className="inline-flex items-center gap-1 mt-1 text-xs text-slate-500">
                          <span className="material-symbols-outlined text-[14px]">tag</span>
                          {group.groupCode}
                        </span>
                      </div>
                      <span
                        className={`text-xs font-bold px-2 py-1 rounded-full ${
                          group.groupStatus === "Active"
                            ? "bg-green-50 text-green-700"
                            : group.groupStatus === "Completed"
                              ? "bg-blue-50 text-blue-700"
                              : "bg-gray-50 text-gray-700"
                        }`}
                      >
                        {group.groupStatus === "Active"
                          ? "Hoạt động"
                          : group.groupStatus === "Completed"
                            ? "Hoàn thành"
                            : group.groupStatus}
                      </span>
                    </div>
                    {group.projectName && (
                      <h4 className="h-10 mb-4 text-sm font-medium text-slate-800 line-clamp-2">{group.projectName}</h4>
                    )}
                    {!group.projectName && <p className="h-10 mb-4 text-sm italic text-slate-400">Chưa có đề tài</p>}
                    <div className="flex items-center justify-between">
                      <div className="flex -space-x-2 overflow-hidden">
                        {group.members.slice(0, 3).map((member, i) => (
                          <div
                            key={member.studentId}
                            className="flex items-center justify-center inline-block text-xs font-bold rounded-full size-8 ring-2 ring-white bg-slate-200 text-slate-500"
                            title={member.fullName}
                          >
                            {member.fullName.charAt(0)}
                          </div>
                        ))}
                        {group.members.length > 3 && (
                          <div className="flex items-center justify-center inline-block text-xs font-bold rounded-full size-8 ring-2 ring-white bg-slate-100 text-slate-500">
                            +{group.members.length - 3}
                          </div>
                        )}
                      </div>
                      <span className="text-xs font-medium text-slate-500">{group.members.length} thành viên</span>
                    </div>
                  </div>
                  <div className="flex items-center justify-end px-5 py-3 border-t bg-slate-50 border-slate-100">
                    <button
                      onClick={() => navigate(`/mentor/groups/${group.groupId}`)}
                      className="flex items-center gap-1 text-sm font-semibold text-primary hover:text-primary/80"
                    >
                      Chi tiết <span className="material-symbols-outlined text-[16px]">arrow_forward</span>
                    </button>
                  </div>
                </motion.div>
              ))}
            </div>
          )}

          {/* Pagination */}
          {!loading && groups.length > 0 && (
            <motion.div variants={item} className="flex items-center justify-between pt-4 border-t border-slate-200">
              <p className="text-sm text-slate-500">
                Hiển thị <span className="font-medium text-slate-900">1-{groups.length}</span> trên{" "}
                <span className="font-medium text-slate-900">{groups.length}</span> nhóm
              </p>
            </motion.div>
          )}
        </motion.div>
      </div>
    </>
  );
}
