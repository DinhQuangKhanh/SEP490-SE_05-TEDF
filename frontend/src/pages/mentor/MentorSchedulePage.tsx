import { motion } from 'framer-motion'
import { NotificationDropdown } from '@/components/layout'

const container = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { staggerChildren: 0.05 } }
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 }
}

const events = [
    { day: 12, month: 'TH 10', title: 'Bảo vệ đề tài cấp Khoa', time: '08:00 - 11:30', type: 'defense', label: 'Quan trọng' },
    { day: 5, month: 'TH 10', title: 'Hạn nộp báo cáo GĐ1', time: '23:59', type: 'deadline', label: 'Deadline' },
    { day: 20, month: 'TH 10', title: 'Họp hội đồng thẩm định', location: 'Phòng 302, Tòa A', type: 'meeting', label: 'Hội đồng' },
]

const groups = [
    { id: 'G1', name: 'AI Y Khoa', leader: 'Trần Văn B', topic: 'Ứng dụng AI trong chẩn đoán ung thư sớm qua ảnh X-Quang.', color: 'blue' },
    { id: 'G2', name: 'E-Commerce', leader: 'Lê Thị C', topic: 'Xây dựng sàn TMĐT kiến trúc Microservices & Kubernetes.', color: 'emerald' },
    { id: 'G3', name: 'Smart Home IoT', leader: 'Phạm Văn D', topic: 'Hệ thống điều khiển nhà thông minh qua giọng nói tiếng Việt.', color: 'purple' },
    { id: 'G4', name: 'Fintech Blockchain', leader: 'Nguyễn Thị E', topic: 'Ví điện tử phi tập trung trên nền tảng Ethereum.', color: 'orange' },
]

const days = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7']

const calendarDays = [
    { day: 24, prev: true }, { day: 25, prev: true }, { day: 26, prev: true }, { day: 27, prev: true }, { day: 28, prev: true }, { day: 29, prev: true }, { day: 30, prev: true },
    { day: 1 }, { day: 2 }, { day: 3 }, { day: 4 },
    { day: 5, event: { type: 'deadline', title: 'HẠN NỘP BÁO CÁO', desc: 'Giai đoạn 1 - All Groups' } },
    { day: 6 }, { day: 7 },
    { day: 8 }, { day: 9 }, { day: 10 }, { day: 11 },
    { day: 12, event: { type: 'defense', title: 'BẢO VỆ ĐỀ TÀI', desc: 'Hội đồng cấp khoa' } },
    { day: 13 }, { day: 14 },
    { day: 15 }, { day: 16 }, { day: 17 }, { day: 18 }, { day: 19 },
    { day: 20, event: { type: 'meeting', title: 'HỌP HỘI ĐỒNG', desc: 'Phòng 302 - Tòa A' } },
    { day: 21 },
    { day: 22 }, { day: 23 }, { day: 24 }, { day: 25 },
    { day: 26, event: { type: 'deadline', title: 'HẠN NỘP BÁO CÁO', desc: 'Báo cáo cuối kỳ' } },
    { day: 27 }, { day: 28 },
    { day: 29 }, { day: 30 }, { day: 31 },
    { day: 1, next: true }, { day: 2, next: true }, { day: 3, next: true }, { day: 4, next: true },
]

export function MentorSchedulePage() {
    return (
        <>
            {/* Header */}
            <header className="h-16 flex items-center justify-between px-8 bg-white border-b border-slate-200 flex-shrink-0 z-50 shadow-sm">
                <h2 className="text-slate-800 text-lg font-bold">Lịch Trình Hướng Dẫn</h2>
                <div className="flex items-center gap-4">
                    <div className="flex p-1 bg-slate-100 rounded-lg">
                        <button className="px-3 py-1 bg-white rounded shadow-sm text-sm font-semibold text-slate-800 flex items-center gap-2">
                            <span className="material-symbols-outlined text-[18px] filled">calendar_month</span>
                            Lịch
                        </button>
                        <button className="px-3 py-1 text-sm font-medium text-slate-500 hover:text-slate-800 flex items-center gap-2 transition-colors">
                            <span className="material-symbols-outlined text-[18px]">format_list_bulleted</span>
                            Danh sách
                        </button>
                    </div>
                    <NotificationDropdown role="mentor" />
                </div>
            </header>

            {/* Content */}
            <div className="flex-1 overflow-hidden flex flex-col xl:flex-row">
                {/* Calendar */}
                <div className="flex-1 overflow-y-auto p-6 min-w-[600px]">
                    <motion.div
                        variants={container}
                        initial="hidden"
                        animate="show"
                        className="bg-white rounded-xl shadow-sm border border-slate-200 h-full flex flex-col min-h-[700px]"
                    >
                        {/* Calendar Header */}
                        <motion.div variants={item} className="flex flex-col sm:flex-row items-start sm:items-center justify-between px-6 py-4 border-b border-slate-100 gap-4">
                            <div className="flex items-center gap-4">
                                <h3 className="text-xl font-bold text-slate-800">Tháng 10, 2023</h3>
                                <div className="flex items-center bg-slate-50 rounded-lg border border-slate-200 p-0.5">
                                    <button className="p-1 hover:bg-white hover:shadow-sm rounded text-slate-500 hover:text-slate-800">
                                        <span className="material-symbols-outlined text-[20px]">chevron_left</span>
                                    </button>
                                    <button className="p-1 hover:bg-white hover:shadow-sm rounded text-slate-500 hover:text-slate-800">
                                        <span className="material-symbols-outlined text-[20px]">chevron_right</span>
                                    </button>
                                </div>
                                <button className="px-3 py-1 text-xs font-semibold text-slate-600 hover:bg-slate-50 rounded border border-slate-200 uppercase tracking-wide">
                                    Hôm nay
                                </button>
                            </div>
                            <div className="flex flex-wrap gap-4 text-xs font-medium">
                                <div className="flex items-center gap-2">
                                    <span className="size-2.5 rounded-full bg-red-500" />
                                    <span className="text-slate-600">BẢO VỆ ĐỀ TÀI</span>
                                </div>
                                <div className="flex items-center gap-2">
                                    <span className="size-2.5 rounded-full bg-amber-400" />
                                    <span className="text-slate-600">HẠN NỘP BÁO CÁO</span>
                                </div>
                                <div className="flex items-center gap-2">
                                    <span className="size-2.5 rounded-full bg-indigo-900" />
                                    <span className="text-slate-600">HỌP HỘI ĐỒNG</span>
                                </div>
                            </div>
                        </motion.div>

                        {/* Days Header */}
                        <div className="grid grid-cols-7 border-b border-slate-200 bg-slate-50 flex-shrink-0">
                            {days.map((day) => (
                                <div key={day} className="py-3 text-center text-xs font-bold text-slate-500 uppercase tracking-wide">
                                    {day}
                                </div>
                            ))}
                        </div>

                        {/* Calendar Grid */}
                        <motion.div variants={item} className="grid grid-cols-7 flex-1 auto-rows-fr divide-x divide-y divide-slate-100 bg-slate-50/50">
                            {calendarDays.map((d, idx) => (
                                <div
                                    key={idx}
                                    className={`min-h-[100px] p-2 ${d.prev || d.next ? 'bg-slate-50/30' : 'bg-white hover:bg-slate-50 transition-colors'
                                        } relative group`}
                                >
                                    <span className={`text-sm font-${d.prev || d.next ? 'medium' : 'bold'} block mb-1 ${d.prev || d.next ? 'text-slate-400' : 'text-slate-700'}`}>
                                        {d.day}
                                    </span>
                                    {d.event && (
                                        <div
                                            className={`p-1.5 rounded border-l-2 mb-1 cursor-pointer transition-colors shadow-sm ${d.event.type === 'defense'
                                                ? 'bg-red-50 border-red-500 hover:bg-red-100'
                                                : d.event.type === 'deadline'
                                                    ? 'bg-amber-50 border-amber-400 hover:bg-amber-100'
                                                    : 'bg-indigo-50 border-indigo-900 hover:bg-indigo-100'
                                                }`}
                                        >
                                            <p className={`text-[10px] font-bold leading-tight ${d.event.type === 'defense' ? 'text-red-700' : d.event.type === 'deadline' ? 'text-amber-700' : 'text-indigo-900'
                                                }`}>
                                                {d.event.title}
                                            </p>
                                            <p className={`text-[10px] truncate mt-0.5 ${d.event.type === 'defense' ? 'text-red-600' : d.event.type === 'deadline' ? 'text-amber-600' : 'text-indigo-800'
                                                }`}>
                                                {d.event.desc}
                                            </p>
                                        </div>
                                    )}
                                </div>
                            ))}
                        </motion.div>
                    </motion.div>
                </div>

                {/* Sidebar */}
                <motion.div
                    variants={container}
                    initial="hidden"
                    animate="show"
                    className="w-full xl:w-[380px] bg-white border-l border-slate-200 overflow-y-auto p-6 space-y-6 flex-shrink-0"
                >
                    {/* Upcoming Events */}
                    <motion.div variants={item}>
                        <div className="flex items-center justify-between mb-4">
                            <h3 className="font-bold text-slate-800 text-base">Sự Kiện Sắp Tới</h3>
                            <a href="#" className="text-xs font-semibold text-primary hover:underline">Xem tất cả</a>
                        </div>
                        <div className="space-y-3">
                            {events.map((event, idx) => (
                                <div
                                    key={idx}
                                    className="flex gap-3 items-start p-3 rounded-lg bg-slate-50 border border-slate-100 hover:border-slate-200 hover:shadow-sm transition-all cursor-pointer group"
                                >
                                    <div className={`flex-shrink-0 flex flex-col items-center justify-center bg-white border border-slate-200 rounded-lg w-12 h-12 shadow-sm group-hover:border-${event.type === 'defense' ? 'red' : event.type === 'deadline' ? 'amber' : 'indigo'}-200`}>
                                        <span className={`text-[10px] font-bold uppercase ${event.type === 'defense' ? 'text-red-500' : event.type === 'deadline' ? 'text-amber-500' : 'text-indigo-700'}`}>
                                            {event.month}
                                        </span>
                                        <span className="text-lg font-bold text-slate-800">{event.day}</span>
                                    </div>
                                    <div className="flex-1">
                                        <h4 className="text-sm font-bold text-slate-800 leading-snug group-hover:text-primary transition-colors">
                                            {event.title}
                                        </h4>
                                        <p className="text-xs text-slate-500 mt-1 flex items-center gap-1">
                                            <span className="material-symbols-outlined text-[14px]">{event.location ? 'location_on' : 'schedule'}</span>
                                            {event.time || event.location}
                                        </p>
                                        <div className="mt-2 flex items-center gap-1">
                                            <span className={`inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium border ${event.type === 'defense'
                                                ? 'bg-red-100 text-red-700 border-red-200'
                                                : event.type === 'deadline'
                                                    ? 'bg-amber-100 text-amber-700 border-amber-200'
                                                    : 'bg-indigo-100 text-indigo-700 border-indigo-200'
                                                }`}>
                                                {event.label}
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </motion.div>

                    <div className="w-full h-px bg-slate-200" />

                    {/* Groups List */}
                    <motion.div variants={item}>
                        <div className="flex items-center justify-between mb-4">
                            <h3 className="font-bold text-slate-800 text-base">Danh sách nhóm hướng dẫn</h3>
                            <button className="text-slate-400 hover:text-primary">
                                <span className="material-symbols-outlined">more_horiz</span>
                            </button>
                        </div>
                        <div className="space-y-3">
                            {groups.map((group) => (
                                <div
                                    key={group.id}
                                    className="p-3 bg-white border border-slate-200 rounded-lg hover:border-primary/50 hover:shadow-md transition-all group cursor-pointer"
                                >
                                    <div className="flex items-center gap-3 mb-2">
                                        <div className={`size-8 rounded-full bg-${group.color}-100 text-${group.color}-700 flex items-center justify-center font-bold text-xs ring-2 ring-white shadow-sm`}>
                                            {group.id}
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <p className="text-sm font-bold text-slate-800 truncate group-hover:text-primary transition-colors">{group.name}</p>
                                            <p className="text-[11px] text-slate-500 truncate">Nhóm trưởng: {group.leader}</p>
                                        </div>
                                    </div>
                                    <div className="text-[11px] text-slate-600 bg-slate-50 p-2 rounded border border-slate-100">
                                        <span className="font-semibold text-slate-700">Đề tài:</span> {group.topic}
                                    </div>
                                </div>
                            ))}
                        </div>
                    </motion.div>
                </motion.div>
            </div>
        </>
    )
}
