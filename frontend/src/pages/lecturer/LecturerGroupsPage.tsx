import { motion } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { useEffect, useMemo, useState } from "react";
import { NotificationDropdown } from "@/components/layout";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { studentGroupService } from "@/lib/groups/studentGroupService";
import type { MentorGroupDto } from "@/types";

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.08 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

export function LecturerGroupsPage() {
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

  // Group by semester, newest semester first, and newest group first within each semester.
  const semesterSections = useMemo(() => {
    const map = new Map<
      number,
      { semesterId: number; semesterName: string; semesterStartDate: string; groups: MentorGroupDto[] }
    >();
    for (const g of groups) {
      const sec = map.get(g.semesterId) ?? {
        semesterId: g.semesterId,
        semesterName: g.semesterName,
        semesterStartDate: g.semesterStartDate,
        groups: [],
      };
      sec.groups.push(g);
      map.set(g.semesterId, sec);
    }
    const sections = Array.from(map.values());
    sections.sort((a, b) => new Date(b.semesterStartDate).getTime() - new Date(a.semesterStartDate).getTime());
    for (const sec of sections) {
      sec.groups.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    }
    return sections;
  }, [groups]);

  return (
    <>
      {/* Header */}
      <header className="z-10 px-8 py-6 shadow-lg bg-primary shrink-0">
        <div className="flex flex-col justify-between w-full gap-4 md:flex-row md:items-center">
          <div className="flex flex-col gap-1">
            <h2 className="flex items-center gap-2 text-2xl font-bold tracking-tight text-white">
              <span className="material-symbols-outlined">groups</span>{" "}
              Danh sách nhóm hướng dẫn
            </h2>
            <p className="text-sm text-blue-100/80">Quản lý tiến độ và theo dõi các nhóm sinh viên</p>
          </div>
          <div className="flex items-center gap-4">
            <NotificationDropdown role="mentor" isNavy={true} />
          </div>
        </div>
      </header>

      {/* Content */}
      <div className="flex-1 p-8 overflow-y-auto bg-slate-100">
        <motion.div variants={container} initial="hidden" animate="show" className="space-y-8">
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

          {/* Groups grouped by semester (newest first) */}
          {!loading &&
            semesterSections.map((sec) => (
              <motion.section key={sec.semesterId} variants={item} className="p-6 space-y-4 bg-white border shadow-sm rounded-xl border-slate-200">
                <div className="flex items-center gap-3 pb-3 border-b border-slate-200">
                  <span className="material-symbols-outlined text-primary">calendar_month</span>
                  <h2 className="text-lg font-bold text-slate-800">{sec.semesterName || "Chưa xác định học kỳ"}</h2>
                  <span className="px-2 py-0.5 text-xs font-semibold rounded-full bg-slate-200 text-slate-600">
                    {sec.groups.length} nhóm
                  </span>
                </div>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-3">
                  {sec.groups.map((group) => (
                    <div
                      key={group.groupId}
                      className="flex flex-col overflow-hidden transition-shadow duration-200 bg-white border shadow-sm rounded-xl border-slate-200 hover:shadow-md"
                    >
                      <div className="flex-1 p-5">
                    <div className="flex items-start justify-between mb-3">
                      <div>
                        <h3 className="text-lg font-bold text-slate-900">
                          {group.displayName || group.groupName || group.groupCode}
                        </h3>
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
                        {group.members.slice(0, 3).map((member) => (
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
                          onClick={() => navigate(`/lecturer/groups/${group.groupId}`)}
                          className="flex items-center gap-1 text-sm font-semibold text-primary hover:text-primary/80"
                        >
                          Chi tiết <span className="material-symbols-outlined text-[16px]">arrow_forward</span>
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </motion.section>
            ))}
        </motion.div>
      </div>
    </>
  );
}
