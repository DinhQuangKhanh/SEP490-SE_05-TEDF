import { useState, useEffect, useCallback } from 'react'
import { motion } from 'framer-motion'
import { Header } from '@/components/layout'
import { useSystemError } from '@/contexts/SystemErrorContext'
import { userService, type UserListItem, type UserListResponse } from '@/lib/userService'

const PAGE_SIZE = 20

const container = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { staggerChildren: 0.05 } }
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 }
}

const roleTabs = [
    { key: '', label: 'Tất cả' },
    { key: 'Student', label: 'Sinh viên' },
    { key: 'Mentor', label: 'Mentor' },
    { key: 'Evaluator', label: 'Evaluator' },
    { key: 'Admin', label: 'Admin' },
]

const roleStyleMap: Record<string, string> = {
    Student: 'bg-slate-100 text-slate-700 border-slate-200',
    Mentor: 'bg-purple-50 text-purple-700 border-purple-100',
    Evaluator: 'bg-orange-50 text-orange-700 border-orange-100',
    Admin: 'bg-slate-800 text-slate-100 border-slate-700',
    DepartmentHead: 'bg-blue-50 text-blue-700 border-blue-100',
}

const roleLabelMap: Record<string, string> = {
    Student: 'Sinh viên',
    Mentor: 'Mentor',
    Evaluator: 'Evaluator',
    Admin: 'Admin',
    DepartmentHead: 'Trưởng BM',
}

const initialsColorPalette = [
    'bg-blue-100 text-blue-600',
    'bg-purple-100 text-purple-600',
    'bg-emerald-100 text-emerald-600',
    'bg-orange-100 text-orange-600',
    'bg-pink-100 text-pink-600',
    'bg-cyan-100 text-cyan-600',
    'bg-amber-100 text-amber-600',
    'bg-indigo-100 text-indigo-600',
]

function getInitialsColor(name: string): string {
    let hash = 0
    for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash)
    return initialsColorPalette[Math.abs(hash) % initialsColorPalette.length]
}

function getInitials(name: string): string {
    const parts = name.trim().split(/\s+/)
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
    return name.substring(0, 2).toUpperCase()
}

function formatDate(dateStr: string): string {
    const d = new Date(dateStr)
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
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

export function UsersPage() {
    const [activeRole, setActiveRole] = useState('')
    const [search, setSearch] = useState('')
    const [debouncedSearch, setDebouncedSearch] = useState('')
    const [page, setPage] = useState(1)
    const [data, setData] = useState<UserListResponse | null>(null)
    const [loading, setLoading] = useState(false)
    const [lockingUserId, setLockingUserId] = useState<string | null>(null)
    const { showError } = useSystemError()

    // Debounced search (400ms)
    useEffect(() => {
        const timer = setTimeout(() => setDebouncedSearch(search), 400)
        return () => clearTimeout(timer)
    }, [search])

    // Reset page when filters change
    useEffect(() => {
        setPage(1)
    }, [activeRole, debouncedSearch])

    // Fetch users
    const fetchUsers = useCallback(async () => {
        setLoading(true)
        try {
            const result = await userService.getUsers({
                role: activeRole || undefined,
                search: debouncedSearch || undefined,
                page,
                pageSize: PAGE_SIZE,
            })
            setData(result)
        } catch (err) {
            showError(err instanceof Error ? err.message : 'Không thể tải danh sách người dùng.')
        } finally {
            setLoading(false)
        }
    }, [activeRole, debouncedSearch, page, showError])

    useEffect(() => {
        fetchUsers()
    }, [fetchUsers])

    // Lock / Unlock handler
    const handleToggleLock = async (user: UserListItem) => {
        setLockingUserId(user.id)
        try {
            if (user.status === 'Locked') {
                await userService.unlockUser(user.id)
            } else {
                await userService.lockUser(user.id)
            }
            await fetchUsers()
        } catch (err) {
            showError(err instanceof Error ? err.message : 'Thao tác thất bại.')
        } finally {
            setLockingUserId(null)
        }
    }

    const totalPages = data?.totalPages ?? 0
    const pageNumbers = getPageNumbers(page, totalPages)

    return (
        <>
            <Header title="Quản Lý Người Dùng" showSearch={false} />

            <div className="flex-1 overflow-y-auto p-8 scrollbar-hide bg-slate-50">
                <motion.div
                    variants={container}
                    initial="hidden"
                    animate="show"
                    className="flex flex-col h-full"
                >
                    {/* Filters */}
                    <motion.div variants={item} className="flex flex-col lg:flex-row lg:items-center justify-between gap-4 mb-6">
                        <div className="flex bg-white p-1 rounded-lg border border-slate-200 shadow-sm overflow-x-auto scrollbar-hide">
                            {roleTabs.map((tab) => (
                                <button
                                    key={tab.key}
                                    onClick={() => setActiveRole(tab.key)}
                                    className={`px-4 py-2 text-sm font-medium rounded-md whitespace-nowrap transition-all ${activeRole === tab.key
                                        ? 'bg-primary text-white shadow-sm'
                                        : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                                        }`}
                                >
                                    {tab.label}
                                </button>
                            ))}
                        </div>
                        <div className="flex items-center gap-3 w-full lg:w-auto">
                            <div className="relative flex-1 lg:w-72">
                                <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-400 text-[18px]">search</span>
                                <input
                                    className="w-full pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary bg-white placeholder-slate-400 text-slate-700"
                                    placeholder="Tìm theo tên, email hoặc mã số..."
                                    type="text"
                                    value={search}
                                    onChange={(e) => setSearch(e.target.value)}
                                />
                            </div>
                            <button className="flex items-center justify-center gap-2 px-4 py-2 bg-white border border-slate-200 text-slate-700 text-sm font-medium rounded-md hover:bg-slate-50 hover:text-primary transition-colors whitespace-nowrap">
                                <span className="material-symbols-outlined text-[20px]">upload_file</span>
                                <span className="hidden sm:inline">Nhập Excel</span>
                            </button>
                            <button className="flex items-center justify-center gap-2 px-4 py-2 bg-primary text-white text-sm font-medium rounded-md hover:bg-primary-light transition-colors shadow-sm whitespace-nowrap">
                                <span className="material-symbols-outlined text-[20px]">person_add</span>
                                <span className="hidden sm:inline">Thêm mới</span>
                            </button>
                        </div>
                    </motion.div>

                    {/* Table */}
                    <motion.div variants={item} className="bento-card rounded-md overflow-hidden bg-white flex flex-col flex-1 min-h-0">
                        <div className="overflow-auto flex-1">
                            <table className="w-full text-left text-sm text-slate-600">
                                <thead className="bg-slate-50 text-xs uppercase font-bold text-slate-500 border-b border-slate-200 sticky top-0 z-10">
                                    <tr>
                                        <th className="px-6 py-4">Người dùng</th>
                                        <th className="px-6 py-4">Vai trò</th>
                                        <th className="px-6 py-4">Khoa / Đơn vị</th>
                                        <th className="px-6 py-4">Ngày tạo</th>
                                        <th className="px-6 py-4">Trạng thái</th>
                                        <th className="px-6 py-4 text-right">Hành động</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100">
                                    {loading ? (
                                        Array.from({ length: 8 }).map((_, i) => (
                                            <tr key={i} className="animate-pulse">
                                                <td className="px-6 py-4">
                                                    <div className="flex items-center gap-3">
                                                        <div className="w-10 h-10 rounded-full bg-slate-200" />
                                                        <div className="space-y-2">
                                                            <div className="h-4 w-32 bg-slate-200 rounded" />
                                                            <div className="h-3 w-48 bg-slate-100 rounded" />
                                                        </div>
                                                    </div>
                                                </td>
                                                <td className="px-6 py-4"><div className="h-5 w-16 bg-slate-200 rounded-full" /></td>
                                                <td className="px-6 py-4"><div className="h-4 w-28 bg-slate-200 rounded" /></td>
                                                <td className="px-6 py-4"><div className="h-4 w-20 bg-slate-200 rounded" /></td>
                                                <td className="px-6 py-4"><div className="h-5 w-20 bg-slate-200 rounded" /></td>
                                                <td className="px-6 py-4"><div className="h-4 w-16 bg-slate-200 rounded float-right" /></td>
                                            </tr>
                                        ))
                                    ) : !data || data.items.length === 0 ? (
                                        <tr>
                                            <td colSpan={6} className="px-6 py-16 text-center">
                                                <span className="material-symbols-outlined text-[48px] text-slate-300 block mb-2">group_off</span>
                                                <p className="text-slate-500 font-medium">Không tìm thấy người dùng nào</p>
                                                <p className="text-slate-400 text-xs mt-1">Thử thay đổi bộ lọc hoặc từ khóa tìm kiếm</p>
                                            </td>
                                        </tr>
                                    ) : (
                                        data.items.map((user) => (
                                            <UserRow
                                                key={user.id}
                                                user={user}
                                                isLocking={lockingUserId === user.id}
                                                onToggleLock={() => handleToggleLock(user)}
                                            />
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
                                    trên <span className="font-medium text-slate-900">{data.totalCount.toLocaleString('vi-VN')}</span> người dùng
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

function UserRow({
    user,
    isLocking,
    onToggleLock,
}: {
    user: UserListItem
    isLocking: boolean
    onToggleLock: () => void
}) {
    const isLocked = user.status === 'Locked'
    const code = user.studentCode ?? user.employeeCode ?? ''
    const primaryRole = user.roles[0] ?? ''
    const roleStyle = roleStyleMap[primaryRole] ?? 'bg-slate-100 text-slate-700 border-slate-200'
    const roleLabel = roleLabelMap[primaryRole] ?? primaryRole
    const isCurrentAdmin = user.roles.includes('Admin')

    return (
        <motion.tr
            whileHover={{ backgroundColor: 'rgb(248 250 252)' }}
            className={`transition-colors group ${isLocked ? 'bg-slate-50/50' : ''}`}
        >
            <td className={`px-6 py-4 ${isLocked ? 'opacity-60' : ''}`}>
                <div className="flex items-center gap-3">
                    {user.avatarUrl ? (
                        <div
                            className="w-10 h-10 rounded-full bg-slate-200 bg-cover bg-center shrink-0"
                            style={{ backgroundImage: `url("${user.avatarUrl}")` }}
                        />
                    ) : (
                        <div className={`w-10 h-10 rounded-full ${getInitialsColor(user.fullName)} flex items-center justify-center font-bold text-sm shrink-0`}>
                            {getInitials(user.fullName)}
                        </div>
                    )}
                    <div className="min-w-0">
                        <p className="font-semibold text-slate-800 truncate">{user.fullName}</p>
                        <p className="text-xs text-slate-500 truncate">
                            {code && <>{code} • </>}{user.email}
                        </p>
                    </div>
                </div>
            </td>
            <td className={`px-6 py-4 ${isLocked ? 'opacity-60' : ''}`}>
                <div className="flex flex-wrap gap-1">
                    {user.roles.map((r) => (
                        <span
                            key={r}
                            className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold border ${roleStyleMap[r] ?? 'bg-slate-100 text-slate-700 border-slate-200'}`}
                        >
                            {roleLabelMap[r] ?? r}
                        </span>
                    ))}
                </div>
            </td>
            <td className={`px-6 py-4 ${isLocked ? 'opacity-60' : ''}`}>
                {user.departmentName ?? <span className="text-slate-400">—</span>}
            </td>
            <td className={`px-6 py-4 ${isLocked ? 'opacity-60' : ''}`}>
                {formatDate(user.createdAt)}
            </td>
            <td className="px-6 py-4">
                {isLocked ? (
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded text-xs font-medium bg-slate-100 text-slate-500 border border-slate-200">
                        <span className="material-symbols-outlined text-[14px]">lock</span>
                        Đã khóa
                    </span>
                ) : user.status === 'Inactive' ? (
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded text-xs font-medium bg-amber-50 text-amber-600 border border-amber-100">
                        <span className="w-1.5 h-1.5 rounded-full bg-amber-400"></span>
                        Không hoạt động
                    </span>
                ) : (
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded text-xs font-medium bg-success/10 text-success border border-success/20">
                        <span className="w-1.5 h-1.5 rounded-full bg-success"></span>
                        Hoạt động
                    </span>
                )}
            </td>
            <td className="px-6 py-4 text-right">
                <div className="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button
                        className="p-1.5 text-slate-400 hover:text-primary hover:bg-primary/5 rounded transition-colors"
                        title="Chỉnh sửa"
                    >
                        <span className="material-symbols-outlined text-[20px]">edit</span>
                    </button>
                    {!isCurrentAdmin && (
                        isLocked ? (
                            <button
                                onClick={onToggleLock}
                                disabled={isLocking}
                                className="p-1.5 text-success hover:bg-success/5 rounded transition-colors disabled:opacity-50"
                                title="Mở khóa"
                            >
                                <span className="material-symbols-outlined text-[20px]">
                                    {isLocking ? 'hourglass_empty' : 'lock_open_right'}
                                </span>
                            </button>
                        ) : (
                            <button
                                onClick={onToggleLock}
                                disabled={isLocking}
                                className="p-1.5 text-slate-400 hover:text-error hover:bg-error/5 rounded transition-colors disabled:opacity-50"
                                title="Khóa tài khoản"
                            >
                                <span className="material-symbols-outlined text-[20px]">
                                    {isLocking ? 'hourglass_empty' : 'lock'}
                                </span>
                            </button>
                        )
                    )}
                </div>
            </td>
        </motion.tr>
    )
}
