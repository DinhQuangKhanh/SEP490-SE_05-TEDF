import { useState, useEffect, useCallback } from 'react'
import { motion } from 'framer-motion'
import { Header } from '@/components/layout'
import { useSystemError } from '@/contexts/SystemErrorContext'
import { apiClient } from '@/lib/apiClient'
import { projectService, type ProjectListItem, type ProjectListResponse } from '@/lib/projectService'
import { ProjectDetailDrawer } from '@/components/admin/ProjectDetailDrawer'

const PAGE_SIZE = 20

const container = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { staggerChildren: 0.05 } }
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 }
}

const statusOptions = [
    { key: '', label: 'Tất cả trạng thái' },
    { key: 'Draft', label: 'Bản nháp' },
    { key: 'PendingMentorReview', label: 'Chờ GVHD duyệt' },
    { key: 'PendingEvaluation', label: 'Chờ đánh giá' },
    { key: 'NeedsModification', label: 'Cần chỉnh sửa' },
    { key: 'Approved', label: 'Đã duyệt' },
    { key: 'Rejected', label: 'Từ chối' },
    { key: 'InProgress', label: 'Đang thực hiện' },
    { key: 'Completed', label: 'Đã hoàn thành' },
    { key: 'Cancelled', label: 'Đã hủy' },
]

const statusConfig: Record<string, { label: string; color: string; icon: string }> = {
    Draft: { label: 'Bản nháp', color: 'bg-slate-100 text-slate-600 border-slate-200', icon: 'edit_note' },
    PendingMentorReview: { label: 'Chờ GVHD duyệt', color: 'bg-orange-50 text-orange-700 border-orange-200', icon: 'hourglass_top' },
    PendingEvaluation: { label: 'Chờ đánh giá', color: 'bg-yellow-50 text-yellow-700 border-yellow-200', icon: 'schedule' },
    NeedsModification: { label: 'Cần chỉnh sửa', color: 'bg-amber-50 text-amber-700 border-amber-200', icon: 'edit' },
    Approved: { label: 'Đã duyệt', color: 'bg-blue-50 text-blue-700 border-blue-200', icon: 'check' },
    Rejected: { label: 'Từ chối', color: 'bg-red-50 text-red-700 border-red-200', icon: 'close' },
    InProgress: { label: 'Đang thực hiện', color: 'bg-indigo-50 text-indigo-700 border-indigo-200', icon: 'play_arrow' },
    Completed: { label: 'Đã hoàn thành', color: 'bg-success/10 text-success border-success/20', icon: 'verified' },
    Cancelled: { label: 'Đã hủy', color: 'bg-slate-100 text-slate-500 border-slate-200', icon: 'block' },
}

const majorColorPalette = [
    'bg-blue-50 text-blue-700 border-blue-100',
    'bg-purple-50 text-purple-700 border-purple-100',
    'bg-emerald-50 text-emerald-700 border-emerald-100',
    'bg-orange-50 text-orange-700 border-orange-100',
    'bg-pink-50 text-pink-700 border-pink-100',
    'bg-cyan-50 text-cyan-700 border-cyan-100',
    'bg-amber-50 text-amber-700 border-amber-100',
    'bg-red-50 text-red-700 border-red-100',
]

function getMajorColor(code: string): string {
    let hash = 0
    for (let i = 0; i < code.length; i++) hash = code.charCodeAt(i) + ((hash << 5) - hash)
    return majorColorPalette[Math.abs(hash) % majorColorPalette.length]
}

function getPageNumbers(currentPage: number, totalPages: number): (number | '...')[] {
    if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1)
    const pages: (number | '...')[] = [1]
    if (currentPage > 3) pages.push('...')
    const start = Math.max(2, currentPage - 1)
    const end = Math.min(totalPages - 1, currentPage + 1)
    for (let i = start; i <= end; i++) pages.push(i)
    if (currentPage < totalPages - 2) pages.push('...')
    if (totalPages > 1) pages.push(totalPages)
    return pages
}

interface SemesterOption { id: number; name: string }
interface MajorOption { id: number; name: string; code: string }

export function ProjectsPage() {
    const [search, setSearch] = useState('')
    const [debouncedSearch, setDebouncedSearch] = useState('')
    const [page, setPage] = useState(1)
    const [semesterId, setSemesterId] = useState<number | undefined>()
    const [status, setStatus] = useState('')
    const [majorId, setMajorId] = useState<number | undefined>()
    const [data, setData] = useState<ProjectListResponse | null>(null)
    const [loading, setLoading] = useState(false)
    const [semesters, setSemesters] = useState<SemesterOption[]>([])
    const [majors, setMajors] = useState<MajorOption[]>([])
    const [selectedProject, setSelectedProject] = useState<ProjectListItem | null>(null)
    const [drawerOpen, setDrawerOpen] = useState(false)
    const { showError } = useSystemError()

    const handleOpenDetail = useCallback((project: ProjectListItem) => {
        setSelectedProject(project)
        setDrawerOpen(true)
    }, [])

    const handleCloseDetail = useCallback(() => {
        setDrawerOpen(false)
    }, [])

    // Load filter options
    useEffect(() => {
        apiClient.get<SemesterOption[]>('/api/semesters').then(setSemesters).catch(() => { })
        apiClient.get<MajorOption[]>('/api/majors').then(setMajors).catch(() => { })
    }, [])

    // Debounced search (400ms)
    useEffect(() => {
        const timer = setTimeout(() => setDebouncedSearch(search), 400)
        return () => clearTimeout(timer)
    }, [search])

    // Reset page when filters change
    useEffect(() => {
        setPage(1)
    }, [semesterId, status, majorId, debouncedSearch])

    // Fetch projects
    const fetchProjects = useCallback(async () => {
        setLoading(true)
        try {
            const result = await projectService.getProjects({
                search: debouncedSearch || undefined,
                semesterId,
                status: status || undefined,
                majorId,
                page,
                pageSize: PAGE_SIZE,
            })
            setData(result)
        } catch (err) {
            showError(err instanceof Error ? err.message : 'Không thể tải danh sách đề tài.')
        } finally {
            setLoading(false)
        }
    }, [debouncedSearch, semesterId, status, majorId, page, showError])

    useEffect(() => {
        fetchProjects()
    }, [fetchProjects])

    const totalPages = data?.totalPages ?? 0
    const pageNumbers = getPageNumbers(page, totalPages)

    return (
        <>
            <Header title="Quản Lý Tổng Đề Tài" showSearch={false} />

            <ProjectDetailDrawer
                project={selectedProject}
                isOpen={drawerOpen}
                onClose={handleCloseDetail}
            />

            <div className="flex-1 overflow-y-auto p-8 scrollbar-hide bg-slate-50">
                <motion.div
                    variants={container}
                    initial="hidden"
                    animate="show"
                    className="flex flex-col h-full"
                >
                    {/* Admin Notice */}
                    <motion.div variants={item} className="bg-blue-50 border border-blue-100 rounded-lg p-4 mb-6 flex items-start gap-3">
                        <span className="material-symbols-outlined text-blue-600 shrink-0">info</span>
                        <p className="text-sm text-blue-700">
                            <span className="font-semibold">Lưu ý:</span> Admin chỉ có quyền xem và xuất báo cáo. Quyền phê duyệt, chỉnh sửa đề tài thuộc về Hội đồng và GVHD.{' '}
                            <a href="#" className="text-primary font-semibold hover:underline">Tìm hiểu thêm</a>
                        </p>
                    </motion.div>

                    {/* Filters */}
                    <motion.div variants={item} className="flex flex-wrap items-center gap-4 mb-6">
                        <div className="flex-1 min-w-0 flex flex-wrap gap-3">
                            <select
                                value={semesterId ?? ''}
                                onChange={(e) => setSemesterId(e.target.value ? Number(e.target.value) : undefined)}
                                className="bg-white border border-slate-200 text-sm rounded-md pl-3 pr-8 py-2 focus:ring-1 focus:ring-primary focus:border-primary"
                            >
                                <option value="">Tất cả kỳ học</option>
                                {semesters.map((s) => (
                                    <option key={s.id} value={s.id}>{s.name}</option>
                                ))}
                            </select>
                            <select
                                value={status}
                                onChange={(e) => setStatus(e.target.value)}
                                className="bg-white border border-slate-200 text-sm rounded-md pl-3 pr-8 py-2 focus:ring-1 focus:ring-primary focus:border-primary"
                            >
                                {statusOptions.map((opt) => (
                                    <option key={opt.key} value={opt.key}>{opt.label}</option>
                                ))}
                            </select>
                            <select
                                value={majorId ?? ''}
                                onChange={(e) => setMajorId(e.target.value ? Number(e.target.value) : undefined)}
                                className="bg-white border border-slate-200 text-sm rounded-md pl-3 pr-8 py-2 focus:ring-1 focus:ring-primary focus:border-primary"
                            >
                                <option value="">Tất cả chuyên ngành</option>
                                {majors.map((m) => (
                                    <option key={m.id} value={m.id}>{m.name}</option>
                                ))}
                            </select>
                            <div className="relative flex-1 min-w-[200px]">
                                <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-400 text-[18px]">search</span>
                                <input
                                    className="w-full pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary bg-white placeholder-slate-400 text-slate-700"
                                    placeholder="Tìm theo tên đề tài, mã đề tài..."
                                    type="text"
                                    value={search}
                                    onChange={(e) => setSearch(e.target.value)}
                                />
                            </div>
                        </div>
                        <div className="flex items-center gap-2">
                            <button className="flex items-center gap-2 bg-primary hover:bg-primary-light text-white px-4 py-2 rounded-md text-sm font-medium transition-colors shadow-sm">
                                <span className="material-symbols-outlined text-[18px]">download</span>
                                Xuất Báo Cáo
                            </button>
                        </div>
                    </motion.div>

                    {/* Table */}
                    <motion.div variants={item} className="bento-card rounded-md overflow-hidden bg-white flex flex-col flex-1 min-h-0">
                        <div className="overflow-auto flex-1">
                            <table className="w-full text-left text-sm text-slate-600">
                                <thead className="bg-slate-50 text-xs uppercase font-bold text-slate-500 border-b border-slate-200 sticky top-0 z-10">
                                    <tr>
                                        <th className="px-6 py-4 w-24">Mã</th>
                                        <th className="px-6 py-4">Tên Đề Tài & Sinh Viên</th>
                                        <th className="px-6 py-4">Chuyên Ngành</th>
                                        <th className="px-6 py-4">Mentor / GVHD</th>
                                        <th className="px-6 py-4">Trạng thái</th>
                                        <th className="px-6 py-4">Kỳ học</th>
                                        <th className="px-6 py-4 text-right w-20">Chi tiết</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100">
                                    {loading ? (
                                        Array.from({ length: 8 }).map((_, i) => (
                                            <tr key={i} className="animate-pulse">
                                                <td className="px-6 py-4"><div className="h-4 w-20 bg-slate-200 rounded" /></td>
                                                <td className="px-6 py-4">
                                                    <div className="space-y-2">
                                                        <div className="h-4 w-64 bg-slate-200 rounded" />
                                                        <div className="h-3 w-40 bg-slate-100 rounded" />
                                                    </div>
                                                </td>
                                                <td className="px-6 py-4"><div className="h-5 w-16 bg-slate-200 rounded-full" /></td>
                                                <td className="px-6 py-4"><div className="h-4 w-32 bg-slate-200 rounded" /></td>
                                                <td className="px-6 py-4"><div className="h-5 w-24 bg-slate-200 rounded-full" /></td>
                                                <td className="px-6 py-4"><div className="h-4 w-20 bg-slate-200 rounded" /></td>
                                                <td className="px-6 py-4"><div className="h-4 w-8 bg-slate-200 rounded float-right" /></td>
                                            </tr>
                                        ))
                                    ) : !data || data.items.length === 0 ? (
                                        <tr>
                                            <td colSpan={7} className="px-6 py-16 text-center">
                                                <span className="material-symbols-outlined text-[48px] text-slate-300 block mb-2">library_books</span>
                                                <p className="text-slate-500 font-medium">Không tìm thấy đề tài nào</p>
                                                <p className="text-slate-400 text-xs mt-1">Thử thay đổi bộ lọc hoặc từ khóa tìm kiếm</p>
                                            </td>
                                        </tr>
                                    ) : (
                                        data.items.map((project) => (
                                            <ProjectRow key={project.id} project={project} onViewDetail={handleOpenDetail} />
                                        ))
                                    )}
                                </tbody>
                            </table>
                        </div>

                        {/* Pagination */}
                        {data && data.totalCount > 0 && (
                            <div className="p-4 border-t border-slate-200 flex items-center justify-between bg-white shrink-0">
                                <span className="text-sm text-slate-500 hidden sm:inline">
                                    Hiển thị{' '}
                                    <span className="font-medium text-slate-900">
                                        {(data.page - 1) * data.pageSize + 1}-{Math.min(data.page * data.pageSize, data.totalCount)}
                                    </span>{' '}
                                    trên <span className="font-medium text-slate-900">{data.totalCount.toLocaleString('vi-VN')}</span> đề tài
                                </span>
                                <div className="flex gap-1 w-full sm:w-auto justify-center sm:justify-end">
                                    <button
                                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                                        disabled={page <= 1}
                                        className="px-3 py-1 border border-slate-200 rounded hover:bg-slate-50 text-slate-600 text-sm disabled:opacity-50 transition-colors"
                                    >
                                        Trước
                                    </button>
                                    {pageNumbers.map((p, i) =>
                                        p === '...' ? (
                                            <span key={`dots-${i}`} className="px-2 py-1 text-slate-400 hidden sm:inline">...</span>
                                        ) : (
                                            <button
                                                key={p}
                                                onClick={() => setPage(p)}
                                                className={`px-3 py-1 rounded text-sm transition-colors ${p === page
                                                    ? 'bg-primary text-white hover:bg-primary-light'
                                                    : 'border border-slate-200 hover:bg-slate-50 text-slate-600'
                                                    }`}
                                            >
                                                {p}
                                            </button>
                                        )
                                    )}
                                    <button
                                        onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                                        disabled={page >= totalPages}
                                        className="px-3 py-1 border border-slate-200 rounded hover:bg-slate-50 text-slate-600 text-sm disabled:opacity-50 transition-colors"
                                    >
                                        Sau
                                    </button>
                                </div>
                            </div>
                        )}
                    </motion.div>
                </motion.div>
            </div>
        </>
    )
}

function ProjectRow({ project, onViewDetail }: { project: ProjectListItem; onViewDetail: (project: ProjectListItem) => void }) {
    const s = statusConfig[project.status] ?? statusConfig.Draft
    const majorColor = getMajorColor(project.majorCode)

    return (
        <motion.tr whileHover={{ backgroundColor: 'rgb(248 250 252)' }} className="transition-colors group">
            <td className="px-6 py-4 font-mono text-xs font-bold text-primary">{project.code}</td>
            <td className="px-6 py-4">
                <div>
                    <span className="text-slate-800 font-bold line-clamp-2 leading-snug">{project.nameVi}</span>
                    {project.studentNames.length > 0 && (
                        <p className="text-xs text-slate-500 mt-1 flex items-center gap-1">
                            <span className="material-symbols-outlined text-[14px]">group</span>
                            {project.studentNames.join(', ')}
                        </p>
                    )}
                </div>
            </td>
            <td className="px-6 py-4">
                <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium border ${majorColor}`}>
                    {project.majorCode}
                </span>
            </td>
            <td className="px-6 py-4">
                {project.mentorNames.length > 0 ? (
                    <div className="flex flex-col gap-1">
                        {project.mentorNames.map((name, idx) => (
                            <div key={idx} className="flex items-center gap-2">
                                <div className="w-6 h-6 rounded-full bg-slate-100 flex items-center justify-center shrink-0">
                                    <span className="material-symbols-outlined text-slate-400 text-[14px]">person</span>
                                </div>
                                <span className="text-sm">{name}</span>
                            </div>
                        ))}
                    </div>
                ) : (
                    <span className="text-slate-400 text-sm">Chưa có</span>
                )}
            </td>
            <td className="px-6 py-4">
                <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${s.color}`}>
                    <span className="material-symbols-outlined text-[14px]">{s.icon}</span>
                    {s.label}
                </span>
            </td>
            <td className="px-6 py-4 text-xs text-slate-600">{project.semesterName}</td>
            <td className="px-6 py-4 text-right">
                <button
                    onClick={() => onViewDetail(project)}
                    className="p-1.5 text-slate-400 hover:text-primary hover:bg-primary/5 rounded transition-colors opacity-0 group-hover:opacity-100"
                    title="Xem chi tiết"
                >
                    <span className="material-symbols-outlined text-[20px]">visibility</span>
                </button>
            </td>
        </motion.tr>
    )
}
