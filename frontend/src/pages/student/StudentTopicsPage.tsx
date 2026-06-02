import { useCallback, useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { Header } from '@/components/layout'
import { TopicCard, TopicCardSkeleton } from '@/components/student/TopicCard'
import { TopicDetailDrawer } from '@/components/student/TopicDetailDrawer'
import { WishlistDrawer } from '@/components/student/WishlistDrawer'
import { useWishlist } from '@/hooks/useWishlist'
import { useSystemError } from '@/contexts/SystemErrorContext'
import { useAuth } from '@/contexts/AuthContext'
import {
    topicPoolService,
    type TopicFilters,
    type TopicsInPoolResponse,
    type MajorOption,
} from '@/lib/topicPoolService'
import { studentGroupService, type StudentGroupDto } from '@/lib/studentGroupService'

// ── Animation variants ───────────────────────────────────────────────────────

const container = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { staggerChildren: 0.04 } },
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 },
}

// ── Pagination helper ────────────────────────────────────────────────────────

function getPageNumbers(currentPage: number, totalPages: number): (number | '...')[] {
    if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1)
    const pages: (number | '...')[] = [1]
    if (currentPage > 3) pages.push('...')
    for (let i = Math.max(2, currentPage - 1); i <= Math.min(totalPages - 1, currentPage + 1); i++)
        pages.push(i)
    if (currentPage < totalPages - 2) pages.push('...')
    if (totalPages > 1) pages.push(totalPages)
    return pages
}

// ── Sort options ─────────────────────────────────────────────────────────────

const sortOptions = [
    { value: 'newest', label: 'Mới nhất' },
    { value: 'name', label: 'Tên A → Z' },
    { value: 'mentor', label: 'Theo mentor' },
]

// ── Page Component ───────────────────────────────────────────────────────────

export function StudentTopicsPage() {
    // State
    const [filters, setFilters] = useState<TopicFilters>({ page: 1, pageSize: 12 })
    const [search, setSearch] = useState('')
    const [data, setData] = useState<TopicsInPoolResponse | null>(null)
    const [majors, setMajors] = useState<MajorOption[]>([])
    const [loading, setLoading] = useState(true)
    const [selectedTopicId, setSelectedTopicId] = useState<string | null>(null)
    const [showWishlist, setShowWishlist] = useState(false)
    const wishlist = useWishlist()
    const { showError } = useSystemError()
    const { user } = useAuth()
    const [myGroup, setMyGroup] = useState<StudentGroupDto | null>(null)
    const [registerTopicId, setRegisterTopicId] = useState<string | null>(null)
    const [registerNote, setRegisterNote] = useState('')
    const [registering, setRegistering] = useState(false)

    useEffect(() => {
        studentGroupService.getMyGroup()
            .then((g) => setMyGroup(g))
            .catch(() => {})
    }, [])

    const myGroupHasProject = !!myGroup?.projectId
    const isLeader = myGroup?.members.some(m => m.studentId === user?.id && m.role === 'Leader') ?? false

    const handleRegister = async () => {
        if (!registerTopicId || !myGroup?.groupId) return
        setRegistering(true)
        try {
            await topicPoolService.registerTopic({ projectId: registerTopicId, groupId: myGroup.groupId, note: registerNote.trim() || undefined })
            setRegisterTopicId(null)
            setRegisterNote('')
            setSelectedTopicId(null)
            // Refresh group state and re-trigger topic fetch
            const g = await studentGroupService.getMyGroup()
            setMyGroup(g)
            setFilters(prev => ({ ...prev }))
        } catch (err) {
            showError(err instanceof Error ? err.message : 'Đăng ký thất bại.')
        } finally {
            setRegistering(false)
        }
    }

    // Debounced search
    useEffect(() => {
        const timer = setTimeout(() => {
            setFilters((prev) => ({ ...prev, search: search || undefined, page: 1 }))
        }, 400)
        return () => clearTimeout(timer)
    }, [search])

    // Fetch majors on mount
    useEffect(() => {
        topicPoolService.getMajors().then(setMajors).catch((err) => {
            showError(err instanceof Error ? err.message : 'Không thể tải danh sách chuyên ngành.')
        })
    }, [])

    // Fetch topics when filters change
    useEffect(() => {
        setLoading(true)
        topicPoolService
            .getTopics(filters)
            .then(setData)
            .catch((err) => {
                setData(null)
                showError(err instanceof Error ? err.message : 'Không thể tải danh sách đề tài. Vui lòng thử lại sau.')
            })
            .finally(() => setLoading(false))
    }, [filters])

    // Handlers
    const updateFilter = useCallback(
        <K extends keyof TopicFilters>(key: K, value: TopicFilters[K]) => {
            setFilters((prev) => ({ ...prev, [key]: value, page: 1 }))
        },
        []
    )

    const setPage = useCallback((page: number) => {
        setFilters((prev) => ({ ...prev, page }))
    }, [])

    const clearFilters = () => {
        setSearch('')
        setFilters({ page: 1, pageSize: 12 })
    }

    const hasActiveFilters = !!(filters.majorId || filters.poolStatus != null || filters.search || (filters.sortBy && filters.sortBy !== 'newest'))

    const pageNumbers = data ? getPageNumbers(data.page, data.totalPages) : []

    return (
        <>
            <Header
                variant="primary"
                title="Kho đề tài đề xuất"
                showSearch={false}
                role="student"
                actions={
                    <div className="relative hidden md:block w-64">
                        <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-[20px] text-blue-200">search</span>
                        <input
                            className="w-full text-sm rounded-md py-2 pl-10 pr-4 focus:outline-none transition-all bg-white/10 border border-white/20 text-white placeholder-blue-200/70 focus:ring-1 focus:ring-white/40 focus:border-white/40 focus:bg-white/20"
                            placeholder="Tìm kiếm đề tài hoặc mentor..."
                            type="text"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>
                }
            />

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-8 scrollbar-hide">
                <motion.div variants={container} initial="hidden" animate="show" className="flex flex-col gap-6">
                    {/* Page Header */}
                    <motion.div variants={item} className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                        <div>
                            <h2 className="text-2xl font-bold text-primary">Kho Đề Tài Mentor Đề Xuất</h2>
                            <p className="text-[#58698d] text-sm mt-1">
                                Danh sách các đề tài do Giảng viên đề xuất cho sinh viên đăng ký thực hiện.
                            </p>
                        </div>
                        <div className="flex items-center gap-3">
                            <motion.button
                                whileTap={{ scale: 0.95 }}
                                onClick={() => setShowWishlist(true)}
                                className="flex items-center gap-2 bg-white border border-[#e9ecf1] px-4 py-2 rounded-lg text-sm font-semibold text-[#101319] hover:bg-gray-50 transition-colors relative"
                            >
                                <span className="material-symbols-outlined text-xl">bookmark</span>
                                Quan tâm
                                {wishlist.count > 0 && (
                                    <span className="bg-red-500 text-white text-[10px] font-bold w-5 h-5 rounded-full flex items-center justify-center">
                                        {wishlist.count}
                                    </span>
                                )}
                            </motion.button>
                        </div>
                    </motion.div>

                    {/* Filters */}
                    <motion.div variants={item} className="bg-white p-5 rounded-xl border border-[#e9ecf1] shadow-sm">
                        <div className="flex flex-col gap-4">
                            {/* Major chips */}
                            <div className="flex flex-col gap-1.5">
                                <label className="text-xs font-bold text-[#58698d] uppercase tracking-wider">Chuyên ngành</label>
                                <div className="flex flex-wrap gap-2">
                                    <button
                                        onClick={() => updateFilter('majorId', undefined)}
                                        className={`px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all ${!filters.majorId
                                            ? 'bg-primary text-white shadow-sm'
                                            : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                                            }`}
                                    >
                                        Tất cả
                                    </button>
                                    {majors.map((m) => (
                                        <button
                                            key={m.id}
                                            onClick={() => updateFilter('majorId', filters.majorId === m.id ? undefined : m.id)}
                                            className={`px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all ${filters.majorId === m.id
                                                ? 'bg-primary text-white shadow-sm'
                                                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                                                }`}
                                        >
                                            {m.code}
                                        </button>
                                    ))}
                                </div>
                            </div>

                            {/* Status chips + Sort + Clear */}
                            <div className="flex flex-wrap items-end gap-4">
                                <div className="flex flex-col gap-1.5">
                                    <label className="text-xs font-bold text-[#58698d] uppercase tracking-wider">Trạng thái</label>
                                    <div className="flex gap-2">
                                        {[
                                            { value: undefined as number | undefined, label: 'Tất cả' },
                                            { value: 0, label: 'Còn trống' },
                                            { value: 2, label: 'Đã có nhóm' },
                                        ].map((opt) => (
                                            <button
                                                key={String(opt.value)}
                                                onClick={() => updateFilter('poolStatus', filters.poolStatus === opt.value ? undefined : opt.value)}
                                                className={`px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all ${filters.poolStatus === opt.value || (opt.value === undefined && filters.poolStatus == null)
                                                    ? 'bg-primary text-white shadow-sm'
                                                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                                                    }`}
                                            >
                                                {opt.label}
                                            </button>
                                        ))}
                                    </div>
                                </div>

                                <div className="flex flex-col gap-1.5">
                                    <label className="text-xs font-bold text-[#58698d] uppercase tracking-wider">Sắp xếp</label>
                                    <select
                                        value={filters.sortBy ?? 'newest'}
                                        onChange={(e) => updateFilter('sortBy', e.target.value)}
                                        className="border border-slate-200 rounded-lg text-xs font-medium px-3 py-1.5 focus:ring-1 focus:ring-primary focus:border-primary bg-white"
                                    >
                                        {sortOptions.map((opt) => (
                                            <option key={opt.value} value={opt.value}>{opt.label}</option>
                                        ))}
                                    </select>
                                </div>

                                {hasActiveFilters && (
                                    <button
                                        onClick={clearFilters}
                                        className="bg-[#f6f7f8] hover:bg-gray-200 text-[#101319] font-bold py-1.5 px-4 rounded-lg text-xs transition-colors flex items-center gap-1.5"
                                    >
                                        <span className="material-symbols-outlined text-sm">filter_alt_off</span>
                                        Xóa bộ lọc
                                    </button>
                                )}
                            </div>
                        </div>
                    </motion.div>

                    {/* Topics Grid */}
                    {loading ? (
                        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
                            {Array.from({ length: 6 }).map((_, i) => (
                                <TopicCardSkeleton key={i} />
                            ))}
                        </div>
                    ) : data && data.items.length > 0 ? (
                        <motion.div
                            variants={container}
                            initial="hidden"
                            animate="show"
                            className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6"
                        >
                            {data.items.map((topic) => (
                                <TopicCard
                                    key={topic.id}
                                    topic={topic}
                                    isFavorite={wishlist.has(topic.id)}
                                    onToggleFavorite={() => wishlist.toggle(topic.id, topic)}
                                    onViewDetail={() => setSelectedTopicId(topic.id)}
                                    groupHasProject={myGroupHasProject}
                                />
                            ))}
                        </motion.div>
                    ) : (
                        <motion.div variants={item} className="flex flex-col items-center justify-center py-20">
                            <span className="material-symbols-outlined text-[56px] text-slate-200 mb-4">search_off</span>
                            <p className="text-slate-500 font-medium mb-1">Không tìm thấy đề tài nào</p>
                            <p className="text-slate-400 text-sm">Thử thay đổi bộ lọc hoặc từ khóa tìm kiếm.</p>
                            {hasActiveFilters && (
                                <button
                                    onClick={clearFilters}
                                    className="mt-4 text-primary text-sm font-medium hover:underline flex items-center gap-1"
                                >
                                    <span className="material-symbols-outlined text-sm">filter_alt_off</span>
                                    Xóa bộ lọc
                                </button>
                            )}
                        </motion.div>
                    )}

                    {/* Pagination */}
                    {data && data.totalPages > 1 && (
                        <motion.div variants={item} className="flex items-center justify-between bg-white px-6 py-4 rounded-xl border border-[#e9ecf1] shadow-sm">
                            <div className="text-sm text-[#58698d]">
                                Hiển thị{' '}
                                <span className="font-bold text-[#101319]">
                                    {(data.page - 1) * data.pageSize + 1}-{Math.min(data.page * data.pageSize, data.totalCount)}
                                </span>{' '}
                                trên <span className="font-bold text-[#101319]">{data.totalCount}</span> đề tài
                            </div>
                            <div className="flex gap-1.5">
                                <button
                                    onClick={() => setPage(data.page - 1)}
                                    disabled={data.page <= 1}
                                    className="p-2 rounded-lg border border-[#e9ecf1] hover:bg-gray-50 text-[#58698d] transition-colors disabled:opacity-40"
                                >
                                    <span className="material-symbols-outlined text-xl">chevron_left</span>
                                </button>
                                {pageNumbers.map((p, i) =>
                                    p === '...' ? (
                                        <span key={`ellipsis-${i}`} className="px-2 self-center text-[#58698d]">...</span>
                                    ) : (
                                        <button
                                            key={p}
                                            onClick={() => setPage(p)}
                                            className={`h-10 w-10 rounded-lg font-bold text-sm transition-colors ${p === data.page
                                                ? 'bg-primary text-white'
                                                : 'border border-[#e9ecf1] hover:bg-gray-50 text-[#58698d]'
                                                }`}
                                        >
                                            {p}
                                        </button>
                                    )
                                )}
                                <button
                                    onClick={() => setPage(data.page + 1)}
                                    disabled={data.page >= data.totalPages}
                                    className="p-2 rounded-lg border border-[#e9ecf1] hover:bg-gray-50 text-[#58698d] transition-colors disabled:opacity-40"
                                >
                                    <span className="material-symbols-outlined text-xl">chevron_right</span>
                                </button>
                            </div>
                        </motion.div>
                    )}

                    {/* Footer */}
                    <div className="mt-8 pt-6 border-t border-[#e9ecf1] flex flex-col md:flex-row justify-between items-center text-[#58698d] text-sm pb-8">
                        <p>&copy; 2023 University Thesis Management System.</p>
                        <div className="flex gap-4 mt-2 md:mt-0">
                            <a className="hover:text-primary transition-colors" href="#">Quy định bảo mật</a>
                            <a className="hover:text-primary transition-colors" href="#">Điều khoản sử dụng</a>
                        </div>
                    </div>
                </motion.div>
            </div>

            {/* Drawers */}
            <TopicDetailDrawer
                projectId={selectedTopicId}
                isOpen={!!selectedTopicId}
                onClose={() => setSelectedTopicId(null)}
                isFavorite={selectedTopicId ? wishlist.has(selectedTopicId) : false}
                onToggleFavorite={() => {
                    if (selectedTopicId) {
                        const topic = data?.items.find((t) => t.id === selectedTopicId)
                        wishlist.toggle(selectedTopicId, topic)
                    }
                }}
                groupHasProject={myGroupHasProject}
                hasGroup={!!myGroup}
                isLeader={isLeader}
                onRegister={(topicId) => setRegisterTopicId(topicId)}
            />

            <WishlistDrawer
                isOpen={showWishlist}
                onClose={() => setShowWishlist(false)}
                wishlist={wishlist}
                onViewDetail={(id) => { setShowWishlist(false); setSelectedTopicId(id) }}
            />

            {/* Registration Confirmation Modal */}
            {registerTopicId && (
                <div className="fixed inset-0 z-[60] bg-black/50 flex items-center justify-center p-4" onClick={() => { setRegisterTopicId(null); setRegisterNote('') }}>
                    <div className="bg-white rounded-xl shadow-2xl w-full max-w-md p-6" onClick={e => e.stopPropagation()}>
                        <h3 className="text-lg font-bold text-[#101319] mb-3 flex items-center gap-2">
                            <span className="material-symbols-outlined text-primary">app_registration</span>
                            Xác nhận đăng ký đề tài
                        </h3>
                        <p className="text-sm text-[#58698d] mb-4">
                            Nhóm <span className="font-semibold text-primary">{myGroup?.groupName ?? myGroup?.groupCode}</span> sẽ đăng ký đề tài này.
                            Sau khi đăng ký, giảng viên sẽ xem xét và xác nhận.
                        </p>
                        <textarea
                            value={registerNote}
                            onChange={e => setRegisterNote(e.target.value)}
                            rows={3}
                            className="w-full px-3 py-2.5 border border-[#e9ecf1] rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none mb-4"
                            placeholder="Ghi chú cho giảng viên (tùy chọn)..."
                        />
                        <div className="flex items-center gap-3">
                            <button
                                onClick={handleRegister}
                                disabled={registering}
                                className="flex-1 py-2.5 bg-primary text-white rounded-lg text-sm font-bold hover:bg-primary-light transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
                            >
                                {registering ? (
                                    <div className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent" />
                                ) : (
                                    <span className="material-symbols-outlined text-lg">check</span>
                                )}
                                Xác nhận
                            </button>
                            <button
                                onClick={() => { setRegisterTopicId(null); setRegisterNote('') }}
                                className="px-5 py-2.5 border border-[#e9ecf1] text-[#58698d] rounded-lg text-sm font-semibold hover:bg-gray-50 transition-colors"
                            >
                                Hủy
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </>
    )
}
