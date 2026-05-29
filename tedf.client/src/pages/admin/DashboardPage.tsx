import { useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Header } from '@/components/layout'
import { SemesterTimeline } from '@/components/shared/SemesterTimeline'
import { useSystemError } from '@/contexts/SystemErrorContext'
import { dashboardService, type AdminDashboardData, type RecentTicket } from '@/lib/dashboardService'

const container = {
    hidden: { opacity: 0 },
    show: {
        opacity: 1,
        transition: {
            staggerChildren: 0.1
        }
    }
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 }
}

export function DashboardPage() {
    const [data, setData] = useState<AdminDashboardData | null>(null)
    const [loading, setLoading] = useState(true)
    const { showError } = useSystemError()

    useEffect(() => {
        const fetchDashboard = async () => {
            try {
                const result = await dashboardService.getAdminDashboard()
                setData(result)
            } catch (err) {
                showError(err instanceof Error ? err.message : 'Không thể tải dữ liệu dashboard.')
            } finally {
                setLoading(false)
            }
        }
        fetchDashboard()
    }, [showError])

    if (loading) {
        return (
            <>
                <Header title="Tổng Quan Hệ Thống" />
                <div className="flex-1 overflow-y-auto p-8 scrollbar-hide bg-slate-50">
                    <div className="space-y-6 animate-pulse">
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                            {[...Array(4)].map((_, i) => (
                                <div key={i} className="bento-card p-5 rounded-md h-28 bg-slate-100" />
                            ))}
                        </div>
                        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                            <div className="lg:col-span-2 bento-card p-6 rounded-md h-56 bg-slate-100" />
                            <div className="bento-card p-6 rounded-md h-56 bg-slate-100" />
                        </div>
                        <div className="bento-card rounded-md h-48 bg-slate-100" />
                    </div>
                </div>
            </>
        )
    }

    if (!data) return null

    const { stats, semesterProgress, approvalRate, recentTickets } = data

    // Compute donut chart
    const total = approvalRate.total || 1
    const inProgressPct = Math.round((approvalRate.inProgress / total) * 100)
    const approvedPct = Math.round((approvalRate.approved / total) * 100)
    const rejectedPct = Math.round((approvalRate.rejected / total) * 100)

    // conic-gradient segments: InProgress (green), Approved (blue), Rejected (red), Pending (gray)
    const seg1 = inProgressPct
    const seg2 = seg1 + approvedPct
    const seg3 = seg2 + rejectedPct
    const gradient = `conic-gradient(#5F8F61 0% ${seg1}%, #3b82f6 ${seg1}% ${seg2}%, #A64B4B ${seg2}% ${seg3}%, #94a3b8 ${seg3}% 100%)`

    const currentPhase = semesterProgress?.phases.find(p => p.status === 1)

    return (
        <>
            <Header title="Tổng Quan Hệ Thống" />

            <div className="flex-1 overflow-y-auto p-8 scrollbar-hide bg-slate-50">
                <motion.div
                    variants={container}
                    initial="hidden"
                    animate="show"
                    className="space-y-6"
                >
                    {/* Stats Cards */}
                    <motion.div variants={item} className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                        <StatsCard
                            icon="group"
                            iconColor="text-blue-600"
                            iconBg="bg-blue-500/10"
                            value={stats.totalStudents.toLocaleString()}
                            label="Tổng Sinh Viên"
                            change="Toàn hệ thống"
                        />
                        <StatsCard
                            icon="school"
                            iconColor="text-green-600"
                            iconBg="bg-green-500/10"
                            value={stats.totalMentors.toLocaleString()}
                            label="Giảng Viên Hướng Dẫn"
                            change="Đang hoạt động"
                        />
                        <StatsCard
                            icon="folder_open"
                            iconColor="text-purple-600"
                            iconBg="bg-purple-500/10"
                            value={stats.totalRegisteredTopics.toLocaleString()}
                            label="Đề Tài Đăng Ký"
                            change="Kỳ hiện tại"
                        />
                        <StatsCard
                            icon="pending_actions"
                            iconColor="text-orange-600"
                            iconBg="bg-orange-500/10"
                            value={stats.highPriorityPending.toLocaleString()}
                            label="Hỗ Trợ Cần Xử Lý"
                            change="Ưu tiên cao"
                            changeColor="text-error"
                        />
                    </motion.div>

                    {/* Progress & Chart Row */}
                    <motion.div variants={item} className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                        {/* Semester Progress */}
                        <div className="lg:col-span-2 bento-card p-6 rounded-md flex flex-col h-full">
                            {semesterProgress ? (
                                <>
                                    <div className="flex justify-between items-center mb-6">
                                        <div>
                                            <h3 className="text-slate-800 text-lg font-bold">
                                                Tiến Độ Kỳ Học: {semesterProgress.semesterName}
                                            </h3>
                                            <p className="text-slate-500 text-sm">
                                                Giai đoạn hiện tại:{' '}
                                                <span className="text-primary font-semibold">
                                                    {currentPhase?.name ?? 'Hoàn thành'}
                                                </span>
                                            </p>
                                        </div>
                                    </div>

                                    {/* Timeline */}
                                    <SemesterTimeline phases={semesterProgress.phases} className="mt-auto" />
                                </>
                            ) : (
                                <div className="flex-1 flex items-center justify-center text-slate-400">
                                    <p>Không có kỳ học đang hoạt động</p>
                                </div>
                            )}
                        </div>

                        {/* Approval Rate Chart */}
                        <div className="bento-card p-6 rounded-md flex flex-col overflow-hidden">
                            <h3 className="text-slate-800 text-lg font-bold mb-4">Tỷ Lệ Đề Tài Kỳ Này</h3>
                            <div className="flex-1 flex flex-col items-center justify-center relative min-h-0">
                                <div
                                    className="relative w-40 h-40 rounded-full bg-slate-100 flex items-center justify-center"
                                    style={{ background: approvalRate.total > 0 ? gradient : undefined }}
                                >
                                    <div className="w-28 h-28 bg-white rounded-full flex flex-col items-center justify-center z-10">
                                        <span className="text-3xl font-bold text-slate-800">{inProgressPct}%</span>
                                        <span className="text-[10px] text-slate-400 uppercase tracking-wide">Đang triển khai</span>
                                    </div>
                                </div>
                                <div className="w-full mt-6 space-y-3">
                                    <LegendItem color="bg-success" label="Đang triển khai" value={approvalRate.inProgress.toLocaleString()} />
                                    <LegendItem color="bg-blue-500" label="Đã duyệt" value={approvalRate.approved.toLocaleString()} />
                                    <LegendItem color="bg-error" label="Từ chối" value={approvalRate.rejected.toLocaleString()} />
                                    {approvalRate.pending > 0 && (
                                        <LegendItem color="bg-slate-400" label="Chờ duyệt" value={approvalRate.pending.toLocaleString()} />
                                    )}
                                </div>
                            </div>
                        </div>
                    </motion.div>

                    {/* Support Table */}
                    <motion.div variants={item} className="bento-card rounded-md overflow-hidden flex flex-col">
                        <div className="p-6 border-b border-slate-200 flex justify-between items-center">
                            <div className="flex items-center gap-2">
                                <span className="material-symbols-outlined text-slate-500">health_and_safety</span>
                                <h3 className="text-slate-800 text-lg font-bold">Hệ Thống & Yêu Cầu Hỗ Trợ</h3>
                            </div>
                            <a
                                href="/admin/support"
                                className="text-xs font-medium text-primary hover:text-primary-light transition-colors"
                            >
                                Xem tất cả
                            </a>
                        </div>
                        <div className="overflow-x-auto">
                            <table className="w-full text-left text-sm text-slate-500">
                                <thead className="bg-slate-50 text-xs uppercase font-semibold text-slate-600 border-b border-slate-200">
                                    <tr>
                                        <th className="px-6 py-4">ID</th>
                                        <th className="px-6 py-4">Vấn đề / Yêu cầu</th>
                                        <th className="px-6 py-4">Người báo cáo</th>
                                        <th className="px-6 py-4">Thời gian</th>
                                        <th className="px-6 py-4">Trạng thái</th>
                                        <th className="px-6 py-4 text-right">Hành động</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100">
                                    {recentTickets.length > 0 ? (
                                        recentTickets.map((ticket) => (
                                            <SupportRow key={ticket.code} ticket={ticket} />
                                        ))
                                    ) : (
                                        <tr>
                                            <td colSpan={6} className="px-6 py-8 text-center text-slate-400">
                                                Không có yêu cầu hỗ trợ nào
                                            </td>
                                        </tr>
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </motion.div>
                </motion.div>
            </div>
        </>
    )
}

// ── Helpers ──────────────────────────────────────────

const ticketStatusLabels: Record<number, string> = {
    0: 'Chưa xử lý',
    1: 'Đang xem xét',
    2: 'Đã giải quyết',
    3: 'Đã đóng',
}

const ticketStatusColors: Record<number, string> = {
    0: 'bg-error/10 text-error border-error/20',
    1: 'bg-blue-500/10 text-blue-600 border-blue-500/20',
    2: 'bg-success/10 text-success border-success/20',
    3: 'bg-slate-100 text-slate-500 border-slate-200',
}

function ticketIcon(priority: number): { icon: string; color: string } {
    switch (priority) {
        case 3: return { icon: 'error', color: 'text-error' }           // Urgent
        case 2: return { icon: 'warning', color: 'text-yellow-500' }    // High
        case 1: return { icon: 'help', color: 'text-blue-500' }        // Medium
        default: return { icon: 'info', color: 'text-slate-400' }      // Low
    }
}

function formatRelativeTime(dateString: string): string {
    const now = new Date()
    const date = new Date(dateString)
    const diffMs = now.getTime() - date.getTime()
    const diffMinutes = Math.floor(diffMs / (1000 * 60))
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60))
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

    if (diffMinutes < 1) return 'Vừa xong'
    if (diffMinutes < 60) return `${diffMinutes} phút trước`
    if (diffHours < 24) return `${diffHours} giờ trước`
    if (diffDays < 30) return `${diffDays} ngày trước`
    return date.toLocaleDateString('vi-VN')
}

// ── Components ──────────────────────────────────────

function StatsCard({
    icon,
    iconColor,
    iconBg,
    value,
    label,
    change,
    changeColor = 'text-slate-500',
}: {
    icon: string
    iconColor: string
    iconBg: string
    value: string
    label: string
    change: string
    changeColor?: string
}) {
    return (
        <motion.div
            whileHover={{ scale: 1.02, boxShadow: '0 4px 12px rgba(0,0,0,0.1)' }}
            className="bento-card p-5 rounded-md flex flex-col justify-between group transition-shadow"
        >
            <div className="flex justify-between items-start">
                <div className={`p-2 ${iconBg} rounded-md ${iconColor}`}>
                    <span className="material-symbols-outlined">{icon}</span>
                </div>
                <span className={`text-xs font-medium ${changeColor}`}>{change}</span>
            </div>
            <div className="mt-4">
                <h3 className="text-2xl font-bold text-slate-800 tracking-tight">{value}</h3>
                <p className="text-sm text-slate-500 font-medium">{label}</p>
            </div>
        </motion.div>
    )
}

function LegendItem({ color, label, value }: { color: string; label: string; value: string }) {
    return (
        <div className="flex justify-between items-center text-sm">
            <div className="flex items-center gap-2">
                <div className={`w-2 h-2 rounded-full ${color}`} />
                <span className="text-slate-600">{label}</span>
            </div>
            <span className="font-medium text-slate-800">{value}</span>
        </div>
    )
}

function SupportRow({ ticket }: { ticket: RecentTicket }) {
    const { icon, color } = ticketIcon(ticket.priority)

    return (
        <tr className="hover:bg-slate-50 transition-colors">
            <td className="px-6 py-4 font-mono text-xs text-slate-600">#{ticket.code}</td>
            <td className="px-6 py-4">
                <div className="flex items-center gap-2">
                    <span className={`material-symbols-outlined ${color} text-[18px]`}>{icon}</span>
                    <span className="text-slate-800 font-medium">{ticket.title}</span>
                </div>
            </td>
            <td className="px-6 py-4 text-slate-600">{ticket.reporterName}</td>
            <td className="px-6 py-4 text-slate-600">{formatRelativeTime(ticket.createdAt)}</td>
            <td className="px-6 py-4">
                <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded text-xs font-medium border ${ticketStatusColors[ticket.status] ?? ''}`}>
                    {ticketStatusLabels[ticket.status] ?? 'Không rõ'}
                </span>
            </td>
            <td className="px-6 py-4 text-right">
                <button className="text-slate-400 hover:text-slate-600 transition-colors">
                    <span className="material-symbols-outlined text-[20px]">more_vert</span>
                </button>
            </td>
        </tr>
    )
}
