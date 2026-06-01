import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { RegisterTopicModal } from '@/components/mentor/RegisterTopicModal'
import { NotificationDropdown } from '@/components/layout'
import { SemesterTimeline } from '@/components/shared/SemesterTimeline'
import { useAuth } from '@/contexts/AuthContext'
import { useSystemError } from '@/contexts/SystemErrorContext'
import { dashboardService, type MentorDashboardData, type RecentProject } from '@/lib/dashboardService'

// ── Animation variants ──────────────────────────────────────────────────────

const container = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { staggerChildren: 0.08 } },
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 },
}

// ── Status helpers ──────────────────────────────────────────────────────────

const projectStatusMap: Record<number, { label: string; className: string }> = {
    0: { label: 'Nháp', className: 'bg-slate-100 text-slate-600 border-slate-200' },
    1: { label: 'Chờ thẩm định', className: 'bg-amber-50 text-amber-700 border-amber-200' },
    2: { label: 'Cần chỉnh sửa', className: 'bg-rose-50 text-rose-700 border-rose-200' },
    3: { label: 'Đã duyệt', className: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
    4: { label: 'Bị từ chối', className: 'bg-red-50 text-red-700 border-red-200' },
    5: { label: 'Đang triển khai', className: 'bg-blue-50 text-blue-700 border-blue-200' },
    6: { label: 'Hoàn thành', className: 'bg-teal-50 text-teal-700 border-teal-200' },
    7: { label: 'Đã hủy', className: 'bg-gray-100 text-gray-500 border-gray-200' },
}

function StatusBadge({ status }: { status: number }) {
    const info = projectStatusMap[status] ?? projectStatusMap[0]
    return (
        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${info.className}`}>
            {info.label}
        </span>
    )
}

function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

// ── Page Component ──────────────────────────────────────────────────────────

export function MentorDashboardPage() {
    const [isModalOpen, setIsModalOpen] = useState(false)
    const [data, setData] = useState<MentorDashboardData | null>(null)
    const [loading, setLoading] = useState(true)
    const { user } = useAuth()
    const { showError } = useSystemError()
    const navigate = useNavigate()

    const fetchDashboard = () => {
        dashboardService.getMentorDashboard()
            .then(setData)
            .catch((err) => showError(err instanceof Error ? err.message : 'Không thể tải dữ liệu dashboard.'))
            .finally(() => setLoading(false))
    }

    useEffect(() => {
        fetchDashboard()
    }, [])

    const mentorName = data?.mentorName || user?.name || 'Mentor'
    const stats = data?.stats
    const semester = data?.semesterProgress

    const statCards = [
        { label: 'Nhóm hướng dẫn', value: stats?.totalGroups ?? 0, icon: 'groups', gradient: 'from-blue-500 to-blue-600' },
        { label: 'Sinh viên', value: stats?.totalStudents ?? 0, icon: 'school', gradient: 'from-indigo-500 to-indigo-600' },
        { label: 'Chờ thẩm định', value: stats?.pendingEvaluation ?? 0, icon: 'pending_actions', gradient: 'from-amber-500 to-orange-500' },
        { label: 'Đang triển khai', value: stats?.inProgressProjects ?? 0, icon: 'rocket_launch', gradient: 'from-emerald-500 to-emerald-600' },
    ]

    return (
        <>
            {/* Header */}
            <header className="h-16 flex items-center justify-between px-8 bg-white border-b border-slate-200 flex-shrink-0 z-50 shadow-sm">
                <h2 className="text-slate-800 text-lg font-bold">Tổng quan</h2>
                <div className="flex items-center gap-4">
                    <div className="relative hidden md:block">
                        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                            <span className="material-symbols-outlined text-slate-400 text-[20px]">search</span>
                        </div>
                        <input
                            type="text"
                            className="block w-64 pl-10 pr-3 py-2 border-none rounded-lg bg-slate-100 text-slate-900 placeholder-slate-500 focus:outline-none focus:bg-white focus:ring-1 focus:ring-primary sm:text-sm transition-all"
                            placeholder="Tìm kiếm sinh viên, đề tài..."
                        />
                    </div>
                    <NotificationDropdown role="mentor" />
                </div>
            </header>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-8">
                <motion.div variants={container} initial="hidden" animate="show" className="space-y-8">
                    {/* Welcome & Action */}
                    <motion.div variants={item} className="flex flex-col md:flex-row md:items-end justify-between gap-4">
                        <div>
                            <h2 className="text-3xl font-extrabold text-slate-900 tracking-tight">
                                {loading ? (
                                    <span className="inline-block bg-slate-200 rounded-lg h-9 w-80 animate-pulse" />
                                ) : (
                                    `Chào mừng trở lại, ${mentorName}!`
                                )}
                            </h2>
                            {semester && (
                                <p className="text-slate-500 mt-1 text-base">
                                    Học kỳ hiện tại: <span className="font-semibold text-slate-700">{semester.semesterName}</span>
                                    {stats && stats.totalProjects > 0 && (
                                        <> &middot; <span className="text-primary font-semibold">{stats.totalProjects} đề tài</span></>
                                    )}
                                </p>
                            )}
                        </div>
                        <button
                            onClick={() => setIsModalOpen(true)}
                            className="inline-flex items-center justify-center gap-2 bg-primary hover:bg-primary/90 text-white px-5 py-2.5 rounded-lg font-semibold shadow-md shadow-blue-900/10 transition-all active:scale-95"
                        >
                            <span className="material-symbols-outlined text-[20px]">add_circle</span>
                            Đăng ký đề tài mới
                        </button>
                    </motion.div>

                    {/* Stats Grid */}
                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
                        {loading
                            ? Array.from({ length: 4 }).map((_, i) => (
                                <motion.div key={i} variants={item} className="bg-white p-5 rounded-xl border border-slate-100 shadow-sm h-32 animate-pulse">
                                    <div className="h-4 bg-slate-200 rounded w-24 mb-3" />
                                    <div className="h-8 bg-slate-200 rounded w-16" />
                                </motion.div>
                            ))
                            : statCards.map((stat) => (
                                <motion.div
                                    key={stat.label}
                                    variants={item}
                                    whileHover={{ y: -2, boxShadow: '0 8px 25px -5px rgba(0,0,0,0.1)' }}
                                    className="bg-white p-5 rounded-xl border border-slate-100 shadow-sm flex items-start justify-between h-32 relative overflow-hidden group"
                                >
                                    <div className="z-10">
                                        <p className="text-slate-500 text-sm font-medium">{stat.label}</p>
                                        <h3 className="text-3xl font-bold text-slate-800 mt-2">{String(stat.value).padStart(2, '0')}</h3>
                                    </div>
                                    <div className={`bg-gradient-to-br ${stat.gradient} text-white p-2.5 rounded-xl shadow-lg`}>
                                        <span className="material-symbols-outlined text-[22px]">{stat.icon}</span>
                                    </div>
                                    <div className="absolute -right-6 -bottom-6 bg-gradient-to-br from-blue-50 to-transparent size-28 rounded-full opacity-40 group-hover:scale-125 transition-transform duration-300" />
                                </motion.div>
                            ))}
                    </div>

                    {/* Recent Projects Table */}
                    <motion.div variants={item} className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
                        <div className="px-6 py-5 border-b border-slate-100 flex items-center justify-between">
                            <h3 className="text-slate-800 font-bold text-lg">Đề tài gần đây</h3>
                            <button
                                onClick={() => navigate('/mentor/topics')}
                                className="text-primary text-sm font-medium hover:underline flex items-center gap-1"
                            >
                                Xem tất cả <span className="material-symbols-outlined text-[16px]">arrow_forward</span>
                            </button>
                        </div>
                        {loading ? (
                            <div className="p-6 space-y-4">
                                {Array.from({ length: 3 }).map((_, i) => (
                                    <div key={i} className="flex items-center gap-4 animate-pulse">
                                        <div className="size-10 bg-slate-200 rounded-lg" />
                                        <div className="flex-1">
                                            <div className="h-4 bg-slate-200 rounded w-48 mb-2" />
                                            <div className="h-3 bg-slate-200 rounded w-32" />
                                        </div>
                                        <div className="h-6 bg-slate-200 rounded-full w-24" />
                                    </div>
                                ))}
                            </div>
                        ) : data && data.recentProjects.length > 0 ? (
                            <div className="overflow-x-auto">
                                <table className="w-full text-left border-collapse">
                                    <thead className="bg-slate-50/50 text-slate-500 text-xs uppercase font-semibold">
                                        <tr>
                                            <th className="px-6 py-4">Tên đề tài</th>
                                            <th className="px-6 py-4">Nhóm</th>
                                            <th className="px-6 py-4 text-center">Trạng thái</th>
                                            <th className="px-6 py-4 text-right">Ngày tạo</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-slate-100">
                                        {data.recentProjects.map((project: RecentProject) => (
                                            <tr key={project.id} className="hover:bg-slate-50/50 transition-colors">
                                                <td className="px-6 py-4">
                                                    <div>
                                                        <p className="font-semibold text-slate-900 text-sm">{project.nameVi}</p>
                                                        <p className="text-xs text-slate-500">Mã: {project.code}</p>
                                                    </div>
                                                </td>
                                                <td className="px-6 py-4">
                                                    {project.leaderName ? (
                                                        <div className="flex items-center gap-2">
                                                            <div className="bg-primary/10 text-primary rounded-full size-7 flex items-center justify-center text-xs font-bold">
                                                                {project.leaderName.charAt(0)}
                                                            </div>
                                                            <div>
                                                                <p className="text-sm text-slate-700">{project.leaderName}</p>
                                                                <p className="text-[10px] text-slate-400">{project.memberCount} thành viên</p>
                                                            </div>
                                                        </div>
                                                    ) : (
                                                        <span className="text-xs text-slate-400 italic">Chưa có nhóm</span>
                                                    )}
                                                </td>
                                                <td className="px-6 py-4 text-center">
                                                    <StatusBadge status={project.status} />
                                                </td>
                                                <td className="px-6 py-4 text-right text-sm text-slate-500">
                                                    {formatDate(project.createdAt)}
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        ) : (
                            <div className="flex flex-col items-center justify-center py-16">
                                <span className="material-symbols-outlined text-[48px] text-slate-200 mb-3">folder_open</span>
                                <p className="text-slate-500 font-medium">Chưa có đề tài nào</p>
                                <p className="text-slate-400 text-sm mt-1">Bắt đầu bằng cách đăng ký đề tài mới.</p>
                            </div>
                        )}
                    </motion.div>

                    {/* Semester Timeline + Quick Actions — same row */}
                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
                        {/* Semester Timeline */}
                        {semester && semester.phases.length > 0 ? (
                            <motion.div variants={item} className="lg:col-span-2 bg-white rounded-xl border border-slate-100 shadow-sm p-6">
                                <div className="flex items-center gap-2 mb-2">
                                    <span className="material-symbols-outlined text-primary">timeline</span>
                                    <h3 className="text-slate-800 font-bold text-lg">Tiến trình học kỳ</h3>
                                </div>
                                <SemesterTimeline phases={semester.phases} />
                            </motion.div>
                        ) : (
                            <div className="lg:col-span-2" />
                        )}

                        {/* Quick Actions — stacked in 1 column */}
                        <motion.div variants={item} className="space-y-3">
                            <button
                                onClick={() => setIsModalOpen(true)}
                                className="w-full group bg-white rounded-xl border border-slate-100 shadow-sm p-4 flex items-center gap-4 hover:border-primary/30 hover:shadow-md transition-all text-left"
                            >
                                <div className="bg-primary/10 text-primary p-2.5 rounded-xl group-hover:bg-primary group-hover:text-white transition-colors">
                                    <span className="material-symbols-outlined text-[22px]">add_circle</span>
                                </div>
                                <div>
                                    <p className="font-bold text-slate-800 text-sm">Đăng ký đề tài mới</p>
                                    <p className="text-xs text-slate-500 mt-0.5">Đề xuất đề tài vào kho đề tài</p>
                                </div>
                            </button>
                            <button
                                onClick={() => navigate('/mentor/topic-pools')}
                                className="w-full group bg-white rounded-xl border border-slate-100 shadow-sm p-4 flex items-center gap-4 hover:border-indigo-300/50 hover:shadow-md transition-all text-left"
                            >
                                <div className="bg-indigo-50 text-indigo-600 p-2.5 rounded-xl group-hover:bg-indigo-600 group-hover:text-white transition-colors">
                                    <span className="material-symbols-outlined text-[22px]">library_books</span>
                                </div>
                                <div>
                                    <p className="font-bold text-slate-800 text-sm">Kho đề tài</p>
                                    <p className="text-xs text-slate-500 mt-0.5">Xem và quản lý kho đề tài</p>
                                </div>
                            </button>
                            <button
                                onClick={() => navigate('/mentor/groups')}
                                className="w-full group bg-white rounded-xl border border-slate-100 shadow-sm p-4 flex items-center gap-4 hover:border-emerald-300/50 hover:shadow-md transition-all text-left"
                            >
                                <div className="bg-emerald-50 text-emerald-600 p-2.5 rounded-xl group-hover:bg-emerald-600 group-hover:text-white transition-colors">
                                    <span className="material-symbols-outlined text-[22px]">groups</span>
                                </div>
                                <div>
                                    <p className="font-bold text-slate-800 text-sm">Nhóm sinh viên</p>
                                    <p className="text-xs text-slate-500 mt-0.5">Quản lý nhóm đang hướng dẫn</p>
                                </div>
                            </button>
                        </motion.div>
                    </div>
                </motion.div>
            </div>

            {/* Modal */}
            <RegisterTopicModal isOpen={isModalOpen} onClose={(success) => { setIsModalOpen(false); if (success) fetchDashboard(); }} />
        </>
    )
}
