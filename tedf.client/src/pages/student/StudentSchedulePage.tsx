import { motion } from "framer-motion";
import { useEffect, useState } from "react";
import { Header } from "@/components/layout";
import { studentGroupService, type StudentGroupDto } from "@/lib/studentGroupService";

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.05 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

const calendarDays = [
  // Previous month (grayed out)
  { day: 27, isCurrentMonth: false },
  { day: 28, isCurrentMonth: false },
  { day: 29, isCurrentMonth: false },
  { day: 30, isCurrentMonth: false },
  // Current month
  { day: 1, isCurrentMonth: true },
  { day: 2, isCurrentMonth: true },
  { day: 3, isCurrentMonth: true },
  { day: 4, isCurrentMonth: true },
  { day: 5, isCurrentMonth: true },
  { day: 6, isCurrentMonth: true },
  { day: 7, isCurrentMonth: true },
  { day: 8, isCurrentMonth: true },
  { day: 9, isCurrentMonth: true },
  { day: 10, isCurrentMonth: true },
  { day: 11, isCurrentMonth: true, event: { type: "deadline", label: "Deadline nộp Slide" } },
  { day: 12, isCurrentMonth: true },
  { day: 13, isCurrentMonth: true },
  { day: 14, isCurrentMonth: true },
  { day: 15, isCurrentMonth: true, event: { type: "defense", label: "08:00 - Bảo vệ" } },
  { day: 16, isCurrentMonth: true },
  { day: 17, isCurrentMonth: true },
  { day: 18, isCurrentMonth: true },
  { day: 19, isCurrentMonth: true },
  { day: 20, isCurrentMonth: true },
  { day: 21, isCurrentMonth: true },
  { day: 22, isCurrentMonth: true },
  { day: 23, isCurrentMonth: true },
  { day: 24, isCurrentMonth: true, isSunday: true },
];

const upcomingEvents = [
  { type: "deadline", label: "Deadline", title: "Nộp Slide Thuyết trình", time: "23:59, 11/12/2023" },
  {
    type: "defense",
    label: "Bảo vệ",
    title: "Bảo vệ chính thức Đồ án",
    location: "Phòng 502, Tòa nhà A1",
    time: "08:00, 15/12/2023",
  },
];

export function StudentSchedulePage() {
  const [myGroup, setMyGroup] = useState<StudentGroupDto | null>(null);

  useEffect(() => {
    studentGroupService
      .getMyGroup()
      .then((data) => setMyGroup(data))
      .catch(() => {
        /* Student may not have a group yet */
      });
  }, []);

  return (
    <>
      <Header
        variant="primary"
        title="Lịch trình chung"
        showSearch={false}
        role="student"
        actions={
          <div className="flex p-1 bg-white/10 rounded-lg">
            <button className="px-3 py-1.5 rounded-md text-sm font-bold bg-white/20 shadow-sm text-white flex items-center gap-2">
              <span className="text-lg material-symbols-outlined">calendar_view_month</span>
              Lịch
            </button>
            <button className="px-3 py-1.5 rounded-md text-sm font-medium text-blue-100/80 hover:text-white flex items-center gap-2">
              <span className="text-lg material-symbols-outlined">format_list_bulleted</span>
              Danh sách
            </button>
          </div>
        }
      />

      {/* Content */}
      <div className="flex-1 p-8 overflow-y-auto">
        <motion.div
          variants={container}
          initial="hidden"
          animate="show"
          className="grid grid-cols-1 gap-8 xl:grid-cols-3"
        >
          {/* Calendar Section */}
          <div className="space-y-6 xl:col-span-2">
            {/* Legend */}
            <motion.div
              variants={item}
              className="bg-white p-4 rounded-xl border border-[#e9ecf1] flex gap-6 text-xs font-bold uppercase tracking-wider"
            >
              <div className="flex items-center gap-2">
                <span className="w-3 h-3 bg-red-500 rounded-full" />
                <span className="text-[#58698d]">Bảo vệ chính thức</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="w-3 h-3 rounded-full bg-amber-400" />
                <span className="text-[#58698d]">Deadline nộp bài</span>
              </div>
            </motion.div>

            {/* Calendar */}
            <motion.div
              variants={item}
              className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm overflow-hidden"
            >
              <div className="p-6 border-b border-[#e9ecf1] flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <h3 className="text-lg font-bold text-primary">Tháng 12, 2023</h3>
                  <div className="flex gap-1">
                    <button className="p-1 transition-colors rounded hover:bg-gray-100">
                      <span className="material-symbols-outlined">chevron_left</span>
                    </button>
                    <button className="p-1 transition-colors rounded hover:bg-gray-100">
                      <span className="material-symbols-outlined">chevron_right</span>
                    </button>
                  </div>
                </div>
                <button className="px-4 py-2 text-sm font-bold border rounded-lg text-primary border-primary/20 hover:bg-primary/5">
                  Hôm nay
                </button>
              </div>

              {/* Weekday Headers */}
              <div className="grid grid-cols-7 border-b border-[#e9ecf1] bg-gray-50/50">
                {["Th 2", "Th 3", "Th 4", "Th 5", "Th 6", "Th 7", "CN"].map((day, idx) => (
                  <div
                    key={day}
                    className={`p-3 text-center text-xs font-bold uppercase tracking-wider ${idx === 6 ? "text-red-500" : "text-[#58698d]"}`}
                  >
                    {day}
                  </div>
                ))}
              </div>

              {/* Calendar Grid */}
              <div className="grid grid-cols-7 bg-white">
                {calendarDays.map((d, idx) => (
                  <div
                    key={idx}
                    className={`h-28 border-r border-b border-[#e9ecf1] p-2 text-sm font-medium ${
                      !d.isCurrentMonth ? "bg-gray-50/30 text-slate-300" : ""
                    } ${d.isSunday ? "text-red-500" : ""} ${d.event?.type === "defense" ? "border-red-200 bg-red-50 text-red-600 font-bold" : ""}`}
                  >
                    {d.day}
                    {d.event && (
                      <div
                        className={`mt-1 text-[9px] p-1 rounded shadow-sm leading-tight font-bold ${
                          d.event.type === "deadline" ? "bg-amber-400 text-amber-800" : "bg-red-500 text-white"
                        }`}
                      >
                        {d.event.label}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </motion.div>
          </div>

          {/* Sidebar */}
          <motion.div variants={item} className="space-y-6 xl:col-span-1">
            {/* Upcoming Events */}
            <div className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm overflow-hidden">
              <div className="p-5 text-white bg-primary">
                <h3 className="flex items-center gap-2 font-bold">
                  <span className="material-symbols-outlined text-secondary">event_upcoming</span>
                  Sự Kiện Sắp Tới
                </h3>
              </div>
              <div className="p-6 space-y-4">
                {upcomingEvents.map((event, idx) => (
                  <div
                    key={idx}
                    className={`relative pl-4 border-l-4 py-1 ${event.type === "deadline" ? "border-amber-400" : "border-red-500"}`}
                  >
                    <p className="text-[10px] text-[#58698d] font-bold uppercase tracking-wider mb-0.5">
                      {event.label}
                    </p>
                    <p className="font-bold text-[#101319] text-sm">{event.title}</p>
                    {event.location && (
                      <div className="flex items-center gap-2 mt-1 text-xs text-[#58698d]">
                        <span className="text-sm material-symbols-outlined">location_on</span>
                        {event.location}
                      </div>
                    )}
                    <div
                      className={`flex items-center gap-2 mt-1 text-xs ${event.type === "defense" ? "text-primary font-bold" : "text-[#58698d]"}`}
                    >
                      <span className="text-sm material-symbols-outlined">schedule</span>
                      {event.time}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Group Info */}
            <div className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm p-5">
              <h3 className="font-bold text-[#101319] mb-4">Thông tin nhóm</h3>
              {myGroup ? (
                <div className="space-y-4">
                  {myGroup.mentorName && (
                    <div className="flex items-center gap-3">
                      <div className="flex items-center justify-center w-8 h-8 text-xs font-bold rounded-full bg-primary/10 text-primary">
                        GV
                      </div>
                      <div className="flex-1">
                        <p className="text-sm font-bold text-[#101319]">{myGroup.mentorName}</p>
                        <p className="text-[11px] text-[#58698d]">Mentor hướng dẫn</p>
                      </div>
                    </div>
                  )}
                  <div className="flex items-center gap-3">
                    <div className="flex items-center justify-center w-8 h-8 text-xs font-bold bg-gray-100 rounded-full text-primary">
                      {myGroup.groupCode.slice(-2)}
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-bold text-[#101319]">{myGroup.groupName ?? myGroup.groupCode}</p>
                      <p className="text-[11px] text-[#58698d]">{myGroup.members.length} thành viên</p>
                    </div>
                  </div>
                  {myGroup.members.map((member) => (
                    <div key={member.studentId} className="flex items-center gap-3 pl-2">
                      <div className="w-6 h-6 rounded-full bg-gray-100 flex items-center justify-center text-[10px] font-bold text-gray-500">
                        {member.fullName.charAt(0)}
                      </div>
                      <div className="flex-1">
                        <p className="text-xs font-medium text-[#101319]">
                          {member.fullName}
                          {member.role === "Leader" && (
                            <span className="ml-1 text-[10px] text-primary font-bold">(Trưởng nhóm)</span>
                          )}
                        </p>
                        <p className="text-[10px] text-[#58698d]">{member.studentCode}</p>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-[#58698d] text-center py-4">Chưa tham gia nhóm</p>
              )}
            </div>
          </motion.div>
        </motion.div>

        {/* Footer */}
        <div className="mt-12 pt-6 border-t border-[#e9ecf1] flex flex-col md:flex-row justify-between items-center text-[#58698d] text-sm pb-8">
          <p>© 2023 University Thesis Management System.</p>
          <div className="flex gap-4 mt-2 md:mt-0">
            <a className="hover:text-primary" href="#">
              Quy định bảo mật
            </a>
            <a className="hover:text-primary" href="#">
              Điều khoản sử dụng
            </a>
          </div>
        </div>
      </div>
    </>
  );
}
