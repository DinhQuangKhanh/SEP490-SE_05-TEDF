import { useState, useEffect, useCallback } from 'react'
import { Header } from '@/components/layout'
import { projectService } from '@/lib/projects/projectService'
import { SupervisedProject } from '@/types'
import { projectStatusLabel, projectStatusColor, getDefenseResult } from '@/lib/projects/projectStatus'
import { SupervisedProjectDetailModal } from '@/components/lecturer'

const PAGE_SIZE = 10

const sortOptions = [
    { value: '', label: 'Mới nhất' },
    { value: 'oldest', label: 'Cũ nhất' },
    { value: 'name', label: 'Tên đề tài (A→Z)' },
    { value: 'status', label: 'Trạng thái' },
]

export function SupervisedProjectsPage() {
    const [items, setItems] = useState<SupervisedProject[]>([])
    const [totalCount, setTotalCount] = useState(0)
    const [totalPages, setTotalPages] = useState(0)
    const [page, setPage] = useState(1)
    const [sort, setSort] = useState('')
    const [search, setSearch] = useState('')
    const [debouncedSearch, setDebouncedSearch] = useState('')
    const [isLoading, setIsLoading] = useState(true)
    const [selected, setSelected] = useState<SupervisedProject | null>(null)

    // Debounce the search box; reset to page 1 whenever the term changes.
    useEffect(() => {
        const t = setTimeout(() => {
            setDebouncedSearch(search)
            setPage(1)
        }, 350)
        return () => clearTimeout(t)
    }, [search])

    const fetchData = useCallback(async () => {
        setIsLoading(true)
        try {
            const res = await projectService.getMySupervised({ search: debouncedSearch, sort, page, pageSize: PAGE_SIZE })
            setItems(res.items)
            setTotalCount(res.totalCount)
            setTotalPages(res.totalPages)
        } catch (e) {
            console.error('Failed to load supervised projects', e)
        } finally {
            setIsLoading(false)
        }
    }, [debouncedSearch, sort, page])

    useEffect(() => {
        fetchData()
    }, [fetchData])

    return (
        <>
            <Header title="Lịch Sử Hướng Dẫn Đồ Án" />

            <div className="flex-1 overflow-y-auto p-8 scrollbar-hide bg-slate-50">
                <div className="max-w-5xl mx-auto space-y-5">
                    {/* Toolbar: search + sort */}
                    <div className="bento-card p-4 rounded-xl bg-white border border-slate-200/60 flex flex-col sm:flex-row gap-3 sm:items-center">
                        <div className="relative flex-1">
                            <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-[20px]">search</span>
                            <input
                                value={search}
                                onChange={(e) => setSearch(e.target.value)}
                                placeholder="Tìm theo tên đề tài hoặc mã..."
                                className="w-full pl-10 pr-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                            />
                        </div>
                        <select
                            value={sort}
                            onChange={(e) => { setSort(e.target.value); setPage(1) }}
                            className="px-3 py-2 border border-slate-200 rounded-lg text-sm bg-white focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                        >
                            {sortOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                        </select>
                    </div>

                    {/* List */}
                    <div className="bento-card rounded-xl bg-white border border-slate-200/60 overflow-hidden">
                        {isLoading ? (
                            <div className="text-center py-16">
                                <span className="material-symbols-outlined animate-spin text-3xl text-primary">progress_activity</span>
                            </div>
                        ) : items.length === 0 ? (
                            <div className="text-center py-16">
                                <span className="material-symbols-outlined text-4xl text-slate-300 mb-2">folder_open</span>
                                <p className="text-sm text-slate-500">
                                    {debouncedSearch ? 'Không tìm thấy đề tài phù hợp.' : 'Chưa có dữ liệu đồ án hướng dẫn.'}
                                </p>
                            </div>
                        ) : (
                            <div className="divide-y divide-slate-100">
                                {items.map(p => {
                                    const defense = getDefenseResult(p.statusValue)
                                    return (
                                        <button
                                            key={p.id}
                                            onClick={() => setSelected(p)}
                                            className="w-full text-left flex items-start gap-3 p-4 hover:bg-slate-50 transition-colors"
                                        >
                                            <div className="p-2 rounded-lg bg-indigo-500/10 text-indigo-600 shrink-0">
                                                <span className="material-symbols-outlined text-[20px]">topic</span>
                                            </div>
                                            <div className="flex-1 min-w-0">
                                                <p className="text-sm font-semibold text-slate-800 truncate">{p.nameVi}</p>
                                                <p className="text-xs text-slate-500 mt-0.5 truncate">
                                                    {p.code} · {p.semesterName}{p.groupCode ? ` · Nhóm ${p.groupCode}` : ''}
                                                </p>
                                            </div>
                                            <div className="flex flex-col items-end gap-1 shrink-0">
                                                <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium border ${projectStatusColor(p.statusValue)}`}>
                                                    {projectStatusLabel(p.statusValue)}
                                                </span>
                                                <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium border ${defense.color}`}>
                                                    <span className="material-symbols-outlined text-[13px]">{defense.icon}</span>
                                                    {defense.label}
                                                </span>
                                            </div>
                                        </button>
                                    )
                                })}
                            </div>
                        )}
                    </div>

                    {/* Pagination */}
                    {totalPages > 1 && (
                        <div className="flex items-center justify-between text-sm">
                            <span className="text-slate-500">Tổng {totalCount} đề tài</span>
                            <div className="flex items-center gap-2">
                                <button
                                    disabled={page <= 1}
                                    onClick={() => setPage(p => p - 1)}
                                    className="px-3 py-1.5 rounded-lg border border-slate-200 bg-white disabled:opacity-40 hover:bg-slate-50 transition-colors"
                                >
                                    Trước
                                </button>
                                <span className="text-slate-600">Trang {page}/{totalPages}</span>
                                <button
                                    disabled={page >= totalPages}
                                    onClick={() => setPage(p => p + 1)}
                                    className="px-3 py-1.5 rounded-lg border border-slate-200 bg-white disabled:opacity-40 hover:bg-slate-50 transition-colors"
                                >
                                    Sau
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            </div>

            {selected && <SupervisedProjectDetailModal project={selected} onClose={() => setSelected(null)} />}
        </>
    )
}
