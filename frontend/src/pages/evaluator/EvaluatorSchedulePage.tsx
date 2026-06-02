import { useState } from "react";
import { motion } from "framer-motion";

const container = { hidden: { opacity: 0 }, show: { opacity: 1, transition: { staggerChildren: 0.05 } } };
const item = { hidden: { opacity: 0, y: 20 }, show: { opacity: 1, y: 0 } };

type ViewMode = "calendar" | "list";

interface CalendarEvent {
  day: number;
  title: string;
  type: "deadline" | "meeting";
}

const events: CalendarEvent[] = [
  { day: 2, title: "Hết hạn nộp", type: "deadline" },
  { day: 5, title: "Họp khoa", type: "meeting" },
  { day: 10, title: "Giai đoạn TĐ 1", type: "deadline" },
  { day: 16, title: "Họp hội đồng", type: "meeting" },
  { day: 24, title: "Hạn chấm điểm", type: "deadline" },
  { day: 25, title: "Họp tổng kết", type: "meeting" },
];

const weekDays = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];

export function EvaluatorSchedulePage() {
  const [viewMode, setViewMode] = useState<ViewMode>("calendar");
  const [selectedDay, setSelectedDay] = useState(24);
  const today = 24;

  const getDaysInMonth = () => {
    const prevMonthDays = [25, 26, 27, 28, 29, 30];
    const currentMonthDays = Array.from({ length: 31 }, (_, i) => i + 1);
    const nextMonthDays = [1, 2, 3, 4];
    return { prevMonthDays, currentMonthDays, nextMonthDays };
  };

  const { prevMonthDays, currentMonthDays, nextMonthDays } = getDaysInMonth();
  const getEventsForDay = (day: number) => events.filter((e) => e.day === day);

  return (
    <div className="max-w-screen-2xl mx-auto p-6 md:p-8 flex flex-col gap-6 h-full">
      {/* Header */}
      <motion.header
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        className="flex flex-col md:flex-row md:items-center justify-between gap-6 shrink-0"
      >
        <div className="flex flex-col gap-1">
          <h2 className="text-slate-900 text-2xl md:text-3xl font-bold tracking-tight">Lịch thẩm định</h2>
          <p className="text-slate-500 text-sm md:text-base">Quản lý lịch trình thẩm định, lịch họp và bảo vệ dự án.</p>
        </div>
        <div className="flex gap-3">
          <div className="flex items-center bg-white border border-gray-200 rounded-lg p-1">
            <button
              onClick={() => setViewMode("calendar")}
              className={`px-3 py-1.5 rounded-md text-xs font-semibold transition-all ${viewMode === "calendar" ? "bg-gray-100 text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900"}`}
            >
              Lịch
            </button>
            <button
              onClick={() => setViewMode("list")}
              className={`px-3 py-1.5 rounded-md text-xs font-medium transition-all ${viewMode === "list" ? "bg-gray-100 text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900"}`}
            >
              Danh sách
            </button>
          </div>
          <button className="flex items-center justify-center gap-2 h-10 px-4 rounded-lg bg-primary text-white text-sm font-semibold hover:bg-primary-dark transition-colors shadow-lg shadow-primary/20">
            <span className="material-symbols-outlined text-[20px]">add</span>
            <span>Đồng bộ lịch</span>
          </button>
        </div>
      </motion.header>

      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="grid grid-cols-1 xl:grid-cols-12 gap-6 flex-1 min-h-0"
      >
        {/* Calendar */}
        <div className="xl:col-span-8 2xl:col-span-9 flex flex-col gap-4">
          {/* Month Navigation */}
          <motion.div
            variants={item}
            className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-4 rounded-2xl border border-gray-200 shadow-sm"
          >
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-1">
                <button className="p-1 rounded-full hover:bg-gray-100 text-slate-500">
                  <span className="material-symbols-outlined">chevron_left</span>
                </button>
                <h3 className="text-lg font-bold text-slate-900 w-36 text-center">Tháng 10, 2023</h3>
                <button className="p-1 rounded-full hover:bg-gray-100 text-slate-500">
                  <span className="material-symbols-outlined">chevron_right</span>
                </button>
              </div>
              <button className="text-xs font-semibold text-primary border border-primary/20 bg-primary/5 px-3 py-1 rounded-md hover:bg-primary/10 transition-colors">
                Hôm nay
              </button>
            </div>
            <div className="flex flex-wrap items-center gap-4 text-xs font-medium text-slate-500">
              <div className="flex items-center gap-1.5">
                <span className="size-2.5 rounded-full bg-red-500" /> Hạn chót
              </div>
              <div className="flex items-center gap-1.5">
                <span className="size-2.5 rounded-full bg-blue-500" /> Họp hội đồng
              </div>
            </div>
          </motion.div>

          {/* Calendar Grid */}
          <motion.div
            variants={item}
            className="flex-1 bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden flex flex-col"
          >
            {/* Week Header */}
            <div className="grid grid-cols-7 border-b border-gray-200 bg-gray-50/50">
              {weekDays.map((day, i) => (
                <div
                  key={day}
                  className={`py-3 text-center text-xs font-bold uppercase tracking-wider ${i >= 5 ? "text-red-500" : "text-slate-500"}`}
                >
                  {day}
                </div>
              ))}
            </div>
            {/* Days Grid */}
            <div className="flex-1 grid grid-cols-7 grid-rows-5 divide-x divide-gray-100 border-b border-gray-100">
              {/* Previous Month */}
              {prevMonthDays.map((day) => (
                <div key={`prev-${day}`} className="bg-gray-50/30 p-2 min-h-[90px] border-b border-gray-100">
                  <span className="text-sm font-medium text-slate-400 opacity-50">{day}</span>
                </div>
              ))}
              {/* Current Month */}
              {currentMonthDays.map((day) => {
                const dayEvents = getEventsForDay(day);
                const isToday = day === today;
                const isSelected = day === selectedDay;
                return (
                  <div
                    key={day}
                    onClick={() => setSelectedDay(day)}
                    className={`p-2 min-h-[90px] border-b border-gray-100 cursor-pointer transition-colors relative group ${isToday ? "bg-primary/5 ring-inset ring-2 ring-primary" : "hover:bg-gray-50"}`}
                  >
                    {isToday ? (
                      <span className="absolute top-2 left-2 flex h-6 w-6 items-center justify-center rounded-full bg-primary text-xs font-bold text-white">
                        {day}
                      </span>
                    ) : (
                      <span
                        className={`text-sm font-medium ${isSelected ? "text-primary font-bold" : "text-slate-900"}`}
                      >
                        {day}
                      </span>
                    )}
                    <div className={`${isToday ? "mt-7" : "mt-1"} flex flex-col gap-1`}>
                      {dayEvents.map((evt, i) => (
                        <div
                          key={i}
                          className={`px-2 py-1 rounded border text-[10px] font-semibold truncate ${evt.type === "deadline" ? "bg-red-50 text-red-700 border-red-100" : "bg-blue-50 text-blue-700 border-blue-100"}`}
                        >
                          {evt.title}
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })}
              {/* Next Month */}
              {nextMonthDays.map((day) => (
                <div key={`next-${day}`} className="bg-gray-50/30 p-2 min-h-[90px] border-b border-gray-100">
                  <span className="text-sm font-medium text-slate-400 opacity-50">{day}</span>
                </div>
              ))}
            </div>
          </motion.div>
        </div>

        {/* Right Panel */}
        <motion.div variants={item} className="xl:col-span-4 2xl:col-span-3 flex flex-col gap-6">
          {/* Selected Date Card */}
          <div className="bg-white rounded-2xl border border-gray-200 p-6 shadow-sm">
            <div className="flex items-start justify-between">
              <div>
                <h3 className="text-lg font-bold text-slate-900">Ngày đã chọn</h3>
                <p className="text-sm text-slate-500">Ngày {selectedDay} tháng 10, 2023</p>
              </div>
              <div className="flex items-center justify-center size-10 rounded-lg bg-primary/10 text-primary">
                <span className="material-symbols-outlined">today</span>
              </div>
            </div>
          </div>

          {/* Upcoming Events */}
          <div className="flex-1 flex flex-col gap-4 overflow-y-auto pr-1">
            <div className="relative py-2">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-gray-200" />
              </div>
              <div className="relative flex justify-center">
                <span className="bg-slate-50 px-2 text-xs text-slate-500 font-medium">Ngày mai, 25/10</span>
              </div>
            </div>

            <div className="group relative bg-white rounded-xl border border-gray-200 p-5 shadow-sm hover:shadow-md transition-all overflow-hidden">
              <div className="absolute top-0 left-0 w-1.5 h-full bg-blue-500" />
              <div className="flex justify-between items-start mb-3">
                <div>
                  <h4 className="font-bold text-slate-900 text-base">Họp thẩm định cuối kỳ</h4>
                  <p className="text-xs font-semibold text-blue-600 mt-0.5">14:00 - 15:30</p>
                </div>
              </div>
              <div className="space-y-3">
                <div className="flex items-center gap-2.5 text-xs text-slate-500">
                  <span className="material-symbols-outlined text-[16px] text-gray-400">videocam</span>
                  <span>Họp trực tuyến (Zoom)</span>
                </div>
                <div className="flex items-center gap-2.5 text-xs text-primary font-medium hover:underline cursor-pointer">
                  <span className="material-symbols-outlined text-[16px]">link</span>
                  <span>zoom.us/j/99283...</span>
                </div>
                <div className="border-t border-gray-100 pt-3">
                  <p className="text-[10px] font-bold text-slate-500 uppercase tracking-wider mb-2">Hội đồng</p>
                  <div className="flex items-center -space-x-2">
                    <div className="size-7 rounded-full bg-primary/10 text-primary flex items-center justify-center text-[10px] font-bold ring-2 ring-white">
                      Bạn
                    </div>
                    <div
                      className="size-7 rounded-full bg-cover ring-2 ring-white"
                      style={{
                        backgroundImage: `url('https://lh3.googleusercontent.com/aida-public/AB6AXuDEerg6rn9OS0-PIJhlnxTIkKgBsLwpYTrc_fp6TVs4hoqr1Bjt0uaJb_VluPBB5RpGvl9w2xzs2pzbNgnpD72rd0wG6vaHc9LLYNBZRWKSRas0tkFkuyTmn9rlQ1nwVKL7JEr09TX070bMca9S63Xt9l3viD2uoKpYXSGhMB3TkYqaNQ9wAZhwwMv9TTuQ9v2ZfO4BpxmKRqzgJabib1NYjdgfAEy9Bb0izgZpTBAocD7TS8pEghQ0EoHp5skX8G4zEQTLXQLTi3xL')`,
                      }}
                    />
                    <div className="size-7 rounded-full bg-gray-200 text-gray-500 flex items-center justify-center text-[10px] font-bold ring-2 ring-white">
                      +3
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </motion.div>
      </motion.div>
    </div>
  );
}
