import { motion } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import { Header } from "@/components/layout";
import { useAuth } from "@/contexts/AuthContext";
import { studentGroupService } from "@/lib/groups/studentGroupService";
import type { StudentGroupDto } from "@/types";

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.08 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

// Static class maps — Tailwind purges interpolated class names (`bg-${color}-50`),
// so each colour variant must be spelled out in full to actually render.
const quickAccessStyles: Record<string, { hoverBorder: string; iconWrap: string; labelHover: string }> = {
  blue: { hoverBorder: "hover:border-blue-200", iconWrap: "bg-blue-50 text-blue-600", labelHover: "group-hover:text-blue-700" },
  green: { hoverBorder: "hover:border-green-200", iconWrap: "bg-green-50 text-green-600", labelHover: "group-hover:text-green-700" },
  pink: { hoverBorder: "hover:border-pink-200", iconWrap: "bg-pink-50 text-pink-600", labelHover: "group-hover:text-pink-700" },
};

const quickAccess = [
  { label: "Kho đề tài", icon: "folder_shared", color: "blue", path: "/student/topics" },
  { label: "Nhóm", icon: "group", color: "green", path: "/student/groups" },
  { label: "Hỗ trợ", icon: "live_help", color: "pink", path: "/student/support" },
];

interface Deadline {
  day: string;
  month: string;
  type: string;
  typeColor: string;
  title: string;
  time: string;
  location: string;
}

const deadlineStyles: Record<string, { badge: string; date: string }> = {
  red: { badge: "text-red-600 bg-red-50 border-red-100", date: "bg-red-50 text-red-600 border-red-100" },
  blue: { badge: "text-blue-600 bg-blue-50 border-blue-100", date: "bg-blue-50 text-blue-600 border-blue-100" },
  gray: { badge: "text-gray-600 bg-gray-50 border-gray-100", date: "bg-gray-50 text-gray-600 border-gray-100" },
};

// No deadlines feed exists yet — render an empty state instead of fabricated rows.
const deadlines: Deadline[] = [];

export function StudentDashboardPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [myGroup, setMyGroup] = useState<StudentGroupDto | null>(null);
  const [loadingGroup, setLoadingGroup] = useState(true);

  useEffect(() => {
    studentGroupService
      .getMyGroup()
      .then((data) => setMyGroup(data))
      .catch((error) => {
        console.error("Error fetching student group:", error);
      })
      .finally(() => setLoadingGroup(false));
  }, []);

  return (
    <>
      <Header
        variant="primary"
        title="Trang chủ"
        searchPlaceholder="Tìm kiếm đề tài, giảng viên, tài liệu..."
        role="student"
      />

      {/* Content */}
      <div className="flex-1 p-8 overflow-y-auto">
        <motion.div variants={container} initial="hidden" animate="show" className="flex flex-col gap-6">
          {/* Welcome Section */}
          <motion.section
            variants={item}
            className="bg-gradient-to-br from-primary to-[#1a56e8] rounded-xl p-6 shadow-md text-white overflow-hidden relative"
          >
            <div
              className="absolute inset-0 opacity-5"
              style={{
                backgroundImage: "radial-gradient(circle at 80% 20%, white 1px, transparent 1px)",
                backgroundSize: "24px 24px",
              }}
            />
            <div className="relative flex flex-col items-start justify-between gap-6 md:flex-row md:items-center">
              <div className="flex flex-col gap-1.5">
                <p className="text-xs font-bold tracking-widest text-blue-200 uppercase">Tổng quan</p>
                <h2 className="text-2xl font-bold">Chào mừng {user?.name ?? "bạn"} trở lại</h2>
                <p className="max-w-sm text-sm text-blue-100">
                  {myGroup?.projectName ? (
                    <>
                      <span className="text-blue-200">Đề tài:</span>{" "}
                      <span className="font-bold text-white">{myGroup.projectName}</span>
                    </>
                  ) : myGroup ? (
                    <>
                      Nhóm <span className="font-bold">{myGroup.groupName ?? myGroup.groupCode}</span> chưa được gán đề
                      tài.
                    </>
                  ) : (
                    "Hãy tham gia hoặc tạo nhóm để bắt đầu hành trình của bạn."
                  )}
                </p>
              </div>
              <div className="flex flex-wrap gap-3 shrink-0">
                {loadingGroup ? (
                  <>
                    {[1, 2, 3].map((i) => (
                      <div key={i} className="h-16 bg-white/10 rounded-xl w-28 animate-pulse" />
                    ))}
                  </>
                ) : myGroup ? (
                  <>
                    <div className="bg-white/10 backdrop-blur-sm rounded-xl px-5 py-3 text-center border border-white/20 min-w-[88px]">
                      <p className="text-xl font-bold">
                        {myGroup.members?.length ?? 0}
                        <span className="text-base text-blue-200">/{myGroup.maxMembers}</span>
                      </p>
                      <p className="text-blue-200 text-xs mt-0.5">Thành viên</p>
                    </div>
                    <div className="bg-white/10 backdrop-blur-sm rounded-xl px-5 py-3 text-center border border-white/20 min-w-[110px]">
                      <p className="text-sm font-bold">
                        {myGroup.projectStatus === "InProgress"
                          ? "Đang thực hiện"
                          : myGroup.projectStatus === "Completed"
                            ? "Hoàn thành"
                            : "Chưa có đề tài"}
                      </p>
                      <p className="text-blue-200 text-xs mt-0.5">Trạng thái</p>
                    </div>
                    {myGroup.mentorName && (
                      <div className="bg-white/10 backdrop-blur-sm rounded-xl px-5 py-3 text-center border border-white/20 max-w-[160px]">
                        <p className="text-sm font-bold truncate">{myGroup.mentorName}</p>
                        <p className="text-blue-200 text-xs mt-0.5">GVHD</p>
                      </div>
                    )}
                  </>
                ) : (
                  <>
                    {[
                      { icon: "group_add", label: "Tạo / tham gia nhóm" },
                      { icon: "folder_shared", label: "Chọn đề tài" },
                      { icon: "school", label: "Bắt đầu thực hiện" },
                    ].map((step, i) => (
                      <div
                        key={i}
                        className="flex items-center gap-2 px-4 py-3 border bg-white/10 backdrop-blur-sm rounded-xl border-white/20"
                      >
                        <span className="material-symbols-outlined text-[18px] text-blue-200">{step.icon}</span>
                        <span className="text-xs font-medium text-blue-100">{step.label}</span>
                      </div>
                    ))}
                  </>
                )}
              </div>
            </div>
          </motion.section>

          {/* Main Grid */}
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
            {/* Topic Overview */}
            <motion.div
              variants={item}
              className="lg:col-span-8 bg-white rounded-xl border border-[#e9ecf1] shadow-sm p-6 flex flex-col"
            >
              <div className="flex items-center justify-between mb-6">
                <h3 className="font-bold text-lg text-[#101319] flex items-center gap-2">
                  <span className="material-symbols-outlined text-primary">donut_large</span>
                  Tổng quan đề tài
                </h3>
                {myGroup?.projectName && (
                  <button
                    onClick={() => navigate("/student/my-topic")}
                    className="text-sm text-[#58698d] hover:text-primary font-medium flex items-center gap-1 transition-colors"
                  >
                    Chi tiết
                    <span className="material-symbols-outlined text-[18px]">arrow_right_alt</span>
                  </button>
                )}
              </div>

              {loadingGroup ? (
                <div className="flex items-center justify-center py-8">
                  <div className="w-6 h-6 border-b-2 rounded-full animate-spin border-primary" />
                </div>
              ) : myGroup?.projectName ? (
                <div className="flex flex-col gap-5">
                  <div>
                    <h4 className="text-xl font-bold text-[#101319] leading-tight mb-2">{myGroup.projectName}</h4>
                    {myGroup.mentorName && (
                      <p className="text-sm text-[#58698d] mb-1">
                        GVHD: <span className="font-semibold text-gray-700">{myGroup.mentorName}</span>
                      </p>
                    )}
                    {myGroup.projectCode && <p className="text-xs text-[#58698d]">Mã: {myGroup.projectCode}</p>}
                  </div>
                  <div className="flex flex-wrap items-center gap-3 mt-auto">
                    <span
                      className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border ${
                        myGroup.projectStatus === "InProgress"
                          ? "bg-green-50 text-green-700 border-green-100"
                          : myGroup.projectStatus === "Completed"
                            ? "bg-blue-50 text-blue-700 border-blue-100"
                            : "bg-gray-50 text-gray-600 border-gray-200"
                      }`}
                    >
                      <span className="w-1.5 h-1.5 rounded-full bg-current" />
                      {myGroup.projectStatus === "InProgress"
                        ? "Đang thực hiện"
                        : myGroup.projectStatus === "Completed"
                          ? "Hoàn thành"
                          : (myGroup.projectStatus ?? "Chưa xác định")}
                    </span>
                    <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium text-gray-600 border border-gray-200">
                      <span className="material-symbols-outlined text-[14px]">group</span>
                      {myGroup.members?.length ?? 0}/{myGroup.maxMembers} thành viên
                    </span>
                  </div>
                </div>
              ) : myGroup ? (
                <div className="flex flex-col items-center justify-center py-8 text-center">
                  <span className="material-symbols-outlined text-4xl text-[#58698d] mb-2">topic</span>
                  <p className="text-[#58698d] text-sm">
                    Nhóm <strong>{myGroup.groupName ?? myGroup.groupCode}</strong> chưa được gán đề tài.
                  </p>
                  <p className="text-xs text-[#58698d] mt-1">
                    {myGroup.members?.length ?? 0}/{myGroup.maxMembers} thành viên
                  </p>
                </div>
              ) : (
                <div className="flex flex-col items-center justify-center py-8 text-center">
                  <span className="material-symbols-outlined text-4xl text-[#58698d] mb-2">group_add</span>
                  <p className="text-[#58698d] text-sm mb-3">Bạn chưa tham gia nhóm nào.</p>
                  <button
                    onClick={() => navigate("/student/groups")}
                    className="px-4 py-2 text-sm font-bold text-white transition-colors rounded-lg bg-primary hover:bg-primary-light"
                  >
                    Tìm nhóm / Tạo nhóm
                  </button>
                </div>
              )}
            </motion.div>

            {/* Quick Access */}
            <motion.div
              variants={item}
              className="lg:col-span-4 bg-white rounded-xl border border-[#e9ecf1] shadow-sm p-6 flex flex-col"
            >
              <h3 className="font-bold text-lg text-[#101319] mb-4 flex items-center gap-2">
                <span className="material-symbols-outlined text-primary">bolt</span>
                Truy cập nhanh
              </h3>
              <div className="grid h-full grid-cols-2 gap-4">
                {quickAccess.map((qa) => {
                  const style = quickAccessStyles[qa.color];
                  return (
                    <button
                      key={qa.label}
                      onClick={() => qa.path !== "#" && navigate(qa.path)}
                      className={`group flex flex-col items-center justify-center p-4 rounded-xl border border-[#e9ecf1] bg-white ${style.hoverBorder} hover:shadow-md hover:-translate-y-1 transition-all`}
                    >
                      <div
                        className={`w-12 h-12 mb-3 rounded-full ${style.iconWrap} flex items-center justify-center group-hover:scale-110 transition-transform`}
                      >
                        <span className="material-symbols-outlined text-[24px]">{qa.icon}</span>
                      </div>
                      <span className={`text-sm font-semibold text-gray-700 ${style.labelHover}`}>
                        {qa.label}
                      </span>
                    </button>
                  );
                })}
              </div>
            </motion.div>
          </div>

          {/* Deadlines Section */}
          <motion.section
            variants={item}
            className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm overflow-hidden"
          >
            <div className="p-5 border-b border-[#e9ecf1] flex items-center justify-between bg-gray-50/50">
              <div className="flex items-center gap-3">
                <div className="bg-secondary/10 p-1.5 rounded-lg text-secondary">
                  <span className="material-symbols-outlined text-[20px]">calendar_clock</span>
                </div>
                <h3 className="font-bold text-[#101319]">Sắp tới (Deadlines)</h3>
              </div>
              <div className="flex gap-2">
                <button className="p-1 hover:bg-gray-200 rounded transition-colors text-[#58698d]">
                  <span className="material-symbols-outlined text-[20px]">filter_list</span>
                </button>
                <button className="p-1 hover:bg-gray-200 rounded transition-colors text-[#58698d]">
                  <span className="material-symbols-outlined text-[20px]">more_horiz</span>
                </button>
              </div>
            </div>
            <div className="flex flex-col">
              {deadlines.length > 0 ? (
                deadlines.map((dl, idx) => {
                  const style = deadlineStyles[dl.typeColor] ?? deadlineStyles.gray;
                  return (
                    <div
                      key={idx}
                      className="flex items-center gap-4 p-4 border-b border-[#e9ecf1] last:border-0 hover:bg-gray-50 transition-colors group cursor-pointer"
                    >
                      <div
                        className={`w-14 h-14 rounded-xl ${style.date} flex flex-col items-center justify-center border shrink-0`}
                      >
                        <span className="text-[10px] font-bold uppercase tracking-wider">{dl.month}</span>
                        <span className="text-xl font-bold leading-none">{dl.day}</span>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <span className={`text-xs font-bold px-2 py-0.5 rounded border ${style.badge}`}>
                            {dl.type}
                          </span>
                          <span className="text-xs text-[#58698d] flex items-center gap-1">
                            <span className="material-symbols-outlined text-[14px]">schedule</span>
                            {dl.time}
                          </span>
                        </div>
                        <h4 className="font-bold text-[#101319] text-sm truncate group-hover:text-primary transition-colors">
                          {dl.title}
                        </h4>
                      </div>
                      <div className="flex-col items-end hidden gap-1 text-right sm:flex shrink-0">
                        <span className="text-xs text-[#58698d] font-medium flex items-center gap-1">
                          <span className="material-symbols-outlined text-[16px]">location_on</span>
                          {dl.location}
                        </span>
                        <button className="text-xs font-bold transition-opacity opacity-0 text-primary group-hover:opacity-100">
                          Chi tiết
                        </button>
                      </div>
                    </div>
                  );
                })
              ) : (
                <div className="flex flex-col items-center justify-center py-12 text-center">
                  <span className="material-symbols-outlined text-4xl text-[#c4ccdb] mb-2">event_available</span>
                  <p className="text-sm text-[#58698d]">Chưa có deadline sắp tới.</p>
                </div>
              )}
            </div>
          </motion.section>

          {/* Footer */}
          <div className="mt-12 pt-6 border-t border-[#e9ecf1] flex flex-col md:flex-row justify-between items-center text-[#58698d] text-sm pb-8">
            <p>&copy; 2025 University Thesis Management System.</p>
            <div className="flex gap-4 mt-2 md:mt-0">
              <a className="hover:text-primary" href="#">
                Quy định bảo mật
              </a>
              <a className="hover:text-primary" href="#">
                Điều khoản sử dụng
              </a>
            </div>
          </div>
        </motion.div>
      </div>
    </>
  );
}
