import { motion } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import { Header } from "@/components/layout";
import { useAuth } from "@/contexts/AuthContext";
import { studentGroupService, type StudentGroupDto } from "@/lib/studentGroupService";

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.08 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

const quickAccess = [
  { label: "Kho đề tài", icon: "folder_shared", color: "blue", path: "/student/topics" },
  { label: "Nhóm", icon: "group", color: "green", path: "/student/groups" },
  { label: "Lịch trình", icon: "calendar_month", color: "indigo", path: "/student/schedule" },
  { label: "Hỗ trợ", icon: "live_help", color: "pink", path: "/student/support" },
];

const deadlines = [
  {
    day: "15",
    month: "T10",
    type: "Quan trọng",
    typeColor: "red",
    title: "Nộp bản thảo Chương 3 - Cơ sở lý thuyết",
    time: "10:00 AM",
    location: "Hệ thống LMS",
  },
  {
    day: "20",
    month: "T10",
    type: "Họp nhóm",
    typeColor: "blue",
    title: "Báo cáo tiến độ với Giảng viên hướng dẫn",
    time: "08:30 AM",
    location: "Phòng 302",
  },
  {
    day: "05",
    month: "T11",
    type: "Nộp bài",
    typeColor: "gray",
    title: "Nộp báo cáo giữa kỳ (Soft copy)",
    time: "23:59 PM",
    location: "Email GV",
  },
];

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
      <div className="flex-1 overflow-y-auto p-8">
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
            <div className="relative flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
              <div className="flex flex-col gap-1.5">
                <p className="text-blue-200 text-xs font-bold uppercase tracking-widest">Tổng quan</p>
                <h2 className="text-2xl font-bold">Chào mừng {user?.name ?? "bạn"} trở lại</h2>
                <p className="text-blue-100 text-sm max-w-sm">
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
              <div className="flex gap-3 flex-wrap shrink-0">
                {loadingGroup ? (
                  <>
                    {[1, 2, 3].map((i) => (
                      <div key={i} className="bg-white/10 rounded-xl w-28 h-16 animate-pulse" />
                    ))}
                  </>
                ) : myGroup ? (
                  <>
                    <div className="bg-white/10 backdrop-blur-sm rounded-xl px-5 py-3 text-center border border-white/20 min-w-[88px]">
                      <p className="text-xl font-bold">
                        {myGroup.members?.length ?? 0}
                        <span className="text-blue-200 text-base">/{myGroup.maxMembers}</span>
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
                        className="bg-white/10 backdrop-blur-sm rounded-xl px-4 py-3 flex items-center gap-2 border border-white/20"
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
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
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
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary" />
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
                    className="px-4 py-2 bg-primary text-white rounded-lg text-sm font-bold hover:bg-primary-light transition-colors"
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
              <div className="grid grid-cols-2 gap-4 h-full">
                {quickAccess.map((qa) => (
                  <button
                    key={qa.label}
                    onClick={() => qa.path !== "#" && navigate(qa.path)}
                    className={`group flex flex-col items-center justify-center p-4 rounded-xl border border-[#e9ecf1] bg-white hover:border-${qa.color}-200 hover:shadow-md hover:-translate-y-1 transition-all`}
                  >
                    <div
                      className={`w-12 h-12 mb-3 rounded-full bg-${qa.color}-50 text-${qa.color}-600 flex items-center justify-center group-hover:scale-110 transition-transform`}
                    >
                      <span className="material-symbols-outlined text-[24px]">{qa.icon}</span>
                    </div>
                    <span className={`text-sm font-semibold text-gray-700 group-hover:text-${qa.color}-700`}>
                      {qa.label}
                    </span>
                  </button>
                ))}
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
              {deadlines.map((dl, idx) => (
                <div
                  key={idx}
                  className="flex items-center gap-4 p-4 border-b border-[#e9ecf1] last:border-0 hover:bg-gray-50 transition-colors group cursor-pointer"
                >
                  <div
                    className={`w-14 h-14 rounded-xl bg-${dl.typeColor}-50 text-${dl.typeColor}-600 flex flex-col items-center justify-center border border-${dl.typeColor}-100 shrink-0`}
                  >
                    <span className="text-[10px] font-bold uppercase tracking-wider">{dl.month}</span>
                    <span className="text-xl font-bold leading-none">{dl.day}</span>
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <span
                        className={`text-xs font-bold text-${dl.typeColor}-600 bg-${dl.typeColor}-50 px-2 py-0.5 rounded border border-${dl.typeColor}-100`}
                      >
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
                  <div className="hidden sm:flex flex-col items-end gap-1 text-right shrink-0">
                    <span className="text-xs text-[#58698d] font-medium flex items-center gap-1">
                      <span className="material-symbols-outlined text-[16px]">location_on</span>
                      {dl.location}
                    </span>
                    <button className="text-xs font-bold text-primary opacity-0 group-hover:opacity-100 transition-opacity">
                      Chi tiết
                    </button>
                  </div>
                </div>
              ))}
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
