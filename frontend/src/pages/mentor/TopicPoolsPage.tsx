import { useEffect, useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '@/lib/apiClient'
import { NotificationDropdown } from '@/components/layout'

// ─── Types ───────────────────────────────────────────────────────────────────

interface TopicPoolSummaryDto {
    id: string
    code: string
    name: string
    statusName: string
    totalTopics: number
}

interface MajorWithPoolDto {
    majorId: number
    majorCode: string
    majorName: string
    pool: TopicPoolSummaryDto | null
}

interface DepartmentWithPoolsDto {
    departmentId: number
    departmentCode: string
    departmentName: string
    majors: MajorWithPoolDto[]
}

// ─── Animation variants ───────────────────────────────────────────────────────

const container = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { staggerChildren: 0.07 } },
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 },
}

// ─── Component ───────────────────────────────────────────────────────────────

export function TopicPoolsPage() {
    const navigate = useNavigate()
    const [departments, setDepartments] = useState<DepartmentWithPoolsDto[]>([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)
    const [search, setSearch] = useState('')
    const [expandedDepts, setExpandedDepts] = useState<Set<number>>(new Set())

    useEffect(() => {
        setLoading(true)
        apiClient
            .get<DepartmentWithPoolsDto[]>('/api/topic-pools/by-department')
            .then(data => {
                setDepartments(data)
                // Auto-expand all departments if only a few
                if (data.length <= 3) {
                    setExpandedDepts(new Set(data.map(d => d.departmentId)))
                }
            })
            .catch((err: Error) => setError(err.message))
            .finally(() => setLoading(false))
    }, [])

    const toggleDept = (id: number) => {
        setExpandedDepts(prev => {
            const next = new Set(prev)
            if (next.has(id)) next.delete(id)
            else next.add(id)
            return next
        })
    }

    // Filter search across depts/majors/pools
    const filtered = search.trim()
        ? departments
            .map(d => ({
                ...d,
                majors: d.majors.filter(m =>
                    m.majorName.toLowerCase().includes(search.toLowerCase()) ||
                    m.majorCode.toLowerCase().includes(search.toLowerCase()) ||
                    m.pool?.name.toLowerCase().includes(search.toLowerCase()) ||
                    d.departmentName.toLowerCase().includes(search.toLowerCase())
                ),
            }))
            .filter(d => d.majors.length > 0)
        : departments

    return (
        <>
            {/* Header */}
            <header className="h-16 flex items-center justify-between px-8 bg-white border-b border-slate-200 flex-shrink-0 z-50 shadow-sm">
                <div className="flex items-center gap-2 text-slate-800">
                    <span className="text-slate-400 font-medium text-sm">Quản lý</span>
                    <span className="material-symbols-outlined text-sm text-slate-400">chevron_right</span>
                    <h2 className="text-lg font-bold">Kho đề tài</h2>
                </div>
                <div className="flex items-center gap-4">
                    <div className="relative hidden md:block">
                        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                            <span className="material-symbols-outlined text-slate-400 text-[20px]">search</span>
                        </div>
                        <input
                            type="text"
                            value={search}
                            onChange={e => setSearch(e.target.value)}
                            className="block w-64 pl-10 pr-3 py-2 rounded-lg bg-slate-100 text-slate-900 placeholder-slate-400 focus:outline-none focus:bg-white focus:ring-1 focus:ring-primary sm:text-sm transition-all border-none"
                            placeholder="Tìm khoa, chuyên ngành..."
                        />
                    </div>
                    <NotificationDropdown />
                </div>
            </header>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-8 bg-slate-50">
                <motion.div variants={container} initial="hidden" animate="show" className="space-y-8">
                    {/* Title */}
                    <motion.div variants={item} className="flex flex-col gap-1">
                        <h1 className="text-2xl font-bold text-slate-900">Kho đề tài theo khoa</h1>
                        <p className="text-slate-500 text-sm">
                            Chọn khoa → chuyên ngành để xem kho đề tài tương ứng.
                        </p>
                    </motion.div>

                    {/* Error */}
                    {error && (
                        <motion.div variants={item} className="flex items-center gap-3 bg-red-50 border border-red-200 rounded-xl p-4 text-red-700">
                            <span className="material-symbols-outlined">error</span>
                            <p className="text-sm font-medium">{error}</p>
                        </motion.div>
                    )}

                    {/* Loading skeleton */}
                    {loading ? (
                        <div className="space-y-4">
                            {[1, 2, 3].map(i => (
                                <div key={i} className="bg-white rounded-xl border border-slate-200 p-5 animate-pulse space-y-4">
                                    <div className="h-5 bg-slate-200 rounded w-1/3" />
                                    <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
                                        {[1, 2].map(j => (
                                            <div key={j} className="h-24 bg-slate-100 rounded-lg" />
                                        ))}
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : filtered.length === 0 ? (
                        <motion.div variants={item} className="flex flex-col items-center justify-center py-20 text-center">
                            <span className="material-symbols-outlined text-5xl text-slate-300">school</span>
                            <p className="text-slate-400 font-medium mt-3">
                                {search ? 'Không tìm thấy kết quả nào' : 'Chưa có dữ liệu khoa nào'}
                            </p>
                        </motion.div>
                    ) : (
                        <div className="space-y-4">
                            {filtered.map(dept => (
                                <motion.div key={dept.departmentId} variants={item} className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
                                    {/* Department header — clickable to expand */}
                                    <button
                                        onClick={() => toggleDept(dept.departmentId)}
                                        className="w-full flex items-center justify-between px-6 py-4 hover:bg-slate-50 transition-colors"
                                    >
                                        <div className="flex items-center gap-3">
                                            <div className="size-10 rounded-lg bg-primary/10 text-primary flex items-center justify-center flex-shrink-0">
                                                <span className="material-symbols-outlined">school</span>
                                            </div>
                                            <div className="text-left">
                                                <p className="font-bold text-slate-900">{dept.departmentName}</p>
                                                <p className="text-xs font-mono text-slate-400">{dept.departmentCode} · {dept.majors.length} chuyên ngành</p>
                                            </div>
                                        </div>
                                        <span className={`material-symbols-outlined text-slate-400 transition-transform duration-200 ${expandedDepts.has(dept.departmentId) ? 'rotate-180' : ''}`}>
                                            expand_more
                                        </span>
                                    </button>

                                    {/* Majors grid — collapsible */}
                                    <AnimatePresence initial={false}>
                                        {expandedDepts.has(dept.departmentId) && (
                                            <motion.div
                                                key="content"
                                                initial={{ height: 0, opacity: 0 }}
                                                animate={{ height: 'auto', opacity: 1 }}
                                                exit={{ height: 0, opacity: 0 }}
                                                transition={{ duration: 0.2 }}
                                                className="overflow-hidden"
                                            >
                                                <div className="px-6 pb-5 pt-1 grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 border-t border-slate-100">
                                                    {dept.majors.map(major => (
                                                        <div
                                                            key={major.majorId}
                                                            onClick={() => major.pool && navigate(`/mentor/topic-pools/${major.pool.id}`)}
                                                            className={`rounded-xl border p-4 transition-all duration-200 flex flex-col gap-2 ${major.pool
                                                                ? 'border-slate-200 hover:border-primary/40 hover:shadow-md cursor-pointer group'
                                                                : 'border-dashed border-slate-200 opacity-60 cursor-default'
                                                                }`}
                                                        >
                                                            <div className="flex items-start justify-between gap-2">
                                                                <div>
                                                                    <p className={`text-sm font-semibold leading-snug ${major.pool ? 'text-slate-800 group-hover:text-primary transition-colors' : 'text-slate-500'}`}>
                                                                        {major.majorName}
                                                                    </p>
                                                                    <p className="text-xs font-mono text-slate-400 mt-0.5">{major.majorCode}</p>
                                                                </div>
                                                                {major.pool ? (
                                                                    <span className={`shrink-0 px-2 py-0.5 rounded-full text-xs font-bold border ${major.pool.statusName === 'Active'
                                                                        ? 'bg-green-50 text-green-700 border-green-100'
                                                                        : 'bg-amber-50 text-amber-700 border-amber-100'
                                                                        }`}>
                                                                        {major.pool.statusName === 'Active' ? 'Đang mở' : 'Tạm dừng'}
                                                                    </span>
                                                                ) : (
                                                                    <span className="shrink-0 px-2 py-0.5 rounded-full text-xs border border-slate-200 text-slate-400">
                                                                        Chưa có kho
                                                                    </span>
                                                                )}
                                                            </div>
                                                            {major.pool && (
                                                                <div className="flex items-center justify-between mt-1">
                                                                    <div className="flex items-center gap-1 text-xs text-slate-500">
                                                                        <span className="material-symbols-outlined text-[14px] text-slate-400">description</span>
                                                                        <span><strong className="text-slate-700">{major.pool.totalTopics}</strong> đề tài</span>
                                                                    </div>
                                                                    <span className="text-primary text-xs font-semibold flex items-center gap-0.5 group-hover:gap-1.5 transition-all">
                                                                        Xem kho
                                                                        <span className="material-symbols-outlined text-[13px]">arrow_forward</span>
                                                                    </span>
                                                                </div>
                                                            )}
                                                        </div>
                                                    ))}
                                                </div>
                                            </motion.div>
                                        )}
                                    </AnimatePresence>
                                </motion.div>
                            ))}
                        </div>
                    )}
                </motion.div>
            </div>
        </>
    )
}
