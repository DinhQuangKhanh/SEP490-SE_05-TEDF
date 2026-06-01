import { useEffect, useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { projectService, type ProjectDetail, type ProjectListItem } from '@/lib/projectService'

// ── Status config ────────────────────────────────────────────────────────────

const statusConfig: Record<string, { label: string; color: string; icon: string }> = {
    Draft: { label: 'Bản nháp', color: 'bg-slate-100 text-slate-600 border-slate-200', icon: 'edit_note' },
    PendingMentorReview: { label: 'Chờ GVHD duyệt', color: 'bg-orange-50 text-orange-700 border-orange-200', icon: 'hourglass_top' },
    PendingEvaluation: { label: 'Chờ đánh giá', color: 'bg-yellow-50 text-yellow-700 border-yellow-200', icon: 'schedule' },
    NeedsModification: { label: 'Cần chỉnh sửa', color: 'bg-amber-50 text-amber-700 border-amber-200', icon: 'edit' },
    Approved: { label: 'Đã duyệt', color: 'bg-blue-50 text-blue-700 border-blue-200', icon: 'check' },
    Rejected: { label: 'Từ chối', color: 'bg-red-50 text-red-700 border-red-200', icon: 'close' },
    InProgress: { label: 'Đang thực hiện', color: 'bg-indigo-50 text-indigo-700 border-indigo-200', icon: 'play_arrow' },
    Completed: { label: 'Đã hoàn thành', color: 'bg-emerald-50 text-emerald-700 border-emerald-200', icon: 'verified' },
    Cancelled: { label: 'Đã hủy', color: 'bg-slate-100 text-slate-500 border-slate-200', icon: 'block' },
}

const sourceTypeLabels: Record<string, string> = {
    FromPool: 'Từ ngân đề tài',
    DirectRegistration: 'Đăng ký trực tiếp',
}

// ── Helpers ───────────────────────────────────────────────────────────────────

const majorChipColors: Record<string, string> = {
    SE: 'bg-blue-50 text-blue-700 border-blue-200',
    AI: 'bg-purple-50 text-purple-700 border-purple-200',
    DS: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    IA: 'bg-amber-50 text-amber-700 border-amber-200',
    IC: 'bg-pink-50 text-pink-700 border-pink-200',
    AS: 'bg-cyan-50 text-cyan-700 border-cyan-200',
    IS: 'bg-orange-50 text-orange-700 border-orange-200',
    GD: 'bg-red-50 text-red-700 border-red-200',
}

const initialsColorPalette = [
    'bg-blue-100 text-blue-600',
    'bg-purple-100 text-purple-600',
    'bg-emerald-100 text-emerald-600',
    'bg-orange-100 text-orange-600',
    'bg-pink-100 text-pink-600',
    'bg-cyan-100 text-cyan-600',
]

function getInitials(name: string): string {
    const parts = name.trim().split(/\s+/)
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
    return name.substring(0, 2).toUpperCase()
}

function getInitialsColor(name: string): string {
    let hash = 0
    for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash)
    return initialsColorPalette[Math.abs(hash) % initialsColorPalette.length]
}

function parseTechnologies(tech: string | null): string[] {
    if (!tech) return []
    return tech.split(',').map((t) => t.trim()).filter(Boolean)
}

function formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return 'N/A'
    return new Date(dateStr).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

// ── Component ─────────────────────────────────────────────────────────────────

interface ProjectDetailDrawerProps {
    /** The list-level data for admin-specific fields (status, semester, students, etc.) */
    project: ProjectListItem | null
    isOpen: boolean
    onClose: () => void
}

export function ProjectDetailDrawer({ project, isOpen, onClose }: ProjectDetailDrawerProps) {
    const [detail, setDetail] = useState<ProjectDetail | null>(null)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    useEffect(() => {
        if (!project?.id || !isOpen) return
        setLoading(true)
        setError(null)
        projectService.getProjectDetail(project.id)
            .then(setDetail)
            .catch((e) => setError(e.message))
            .finally(() => setLoading(false))
    }, [project?.id, isOpen])

    // Close on Escape key
    useEffect(() => {
        if (!isOpen) return
        const handleKey = (e: KeyboardEvent) => {
            if (e.key === 'Escape') onClose()
        }
        window.addEventListener('keydown', handleKey)
        return () => window.removeEventListener('keydown', handleKey)
    }, [isOpen, onClose])

    const s = statusConfig[project?.status ?? ''] ?? statusConfig.Draft
    const chipColor = detail ? (majorChipColors[detail.majorCode] ?? 'bg-slate-50 text-slate-700 border-slate-200') : ''
    const techs = parseTechnologies(detail?.technologies ?? null)

    return (
        <AnimatePresence>
            {isOpen && (
                <>
                    {/* Backdrop */}
                    <motion.div
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                        className="fixed inset-0 z-50 bg-black/30 backdrop-blur-sm"
                        onClick={onClose}
                    />

                    {/* Drawer */}
                    <motion.div
                        initial={{ x: '100%' }}
                        animate={{ x: 0 }}
                        exit={{ x: '100%' }}
                        transition={{ type: 'spring', damping: 28, stiffness: 300 }}
                        className="fixed right-0 top-0 bottom-0 z-50 w-full max-w-2xl bg-white shadow-2xl flex flex-col"
                    >
                        {/* Header */}
                        <div className="px-6 py-5 border-b border-slate-100 shrink-0">
                            <div className="flex items-start justify-between gap-4">
                                <div className="min-w-0 flex-1">
                                    {/* Badges row */}
                                    {(detail || project) && (
                                        <div className="flex items-center gap-2 mb-3 flex-wrap">
                                            {detail && (
                                                <span className={`${chipColor} px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border`}>
                                                    {detail.majorCode}
                                                </span>
                                            )}
                                            <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-medium border ${s.color}`}>
                                                <span className="material-symbols-outlined text-[12px]">{s.icon}</span>
                                                {s.label}
                                            </span>
                                            {project?.sourceType && (
                                                <span className="bg-slate-50 text-slate-500 border-slate-200 px-2 py-0.5 rounded text-[10px] font-medium border">
                                                    {sourceTypeLabels[project.sourceType] ?? project.sourceType}
                                                </span>
                                            )}
                                        </div>
                                    )}
                                    <h2 className="text-lg font-bold text-[#101319] leading-tight">
                                        {loading ? 'Đang tải...' : detail?.nameVi ?? project?.nameVi ?? 'Đề tài'}
                                    </h2>
                                    {detail?.nameEn && (
                                        <p className="text-xs text-slate-400 mt-1 italic">{detail.nameEn}</p>
                                    )}
                                </div>
                                <button
                                    onClick={onClose}
                                    className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-colors shrink-0"
                                >
                                    <span className="material-symbols-outlined text-xl">close</span>
                                </button>
                            </div>
                        </div>

                        {/* Content */}
                        <div className="flex-1 overflow-y-auto p-6 scrollbar-hide">
                            {loading && (
                                <div className="flex flex-col gap-4 animate-pulse">
                                    <div className="h-4 w-full bg-slate-200 rounded" />
                                    <div className="h-4 w-3/4 bg-slate-200 rounded" />
                                    <div className="h-4 w-full bg-slate-100 rounded" />
                                    <div className="h-20 w-full bg-slate-100 rounded" />
                                    <div className="h-4 w-1/2 bg-slate-200 rounded" />
                                    <div className="h-20 w-full bg-slate-100 rounded" />
                                </div>
                            )}

                            {error && (
                                <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">
                                    <span className="material-symbols-outlined text-base align-middle mr-1">error</span>
                                    {error}
                                </div>
                            )}

                            {detail && !loading && (
                                <div className="flex flex-col gap-5">
                                    {/* Info cards */}
                                    <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                                        <InfoCard icon="tag" label="Mã đề tài" value={detail.code} />
                                        <InfoCard icon="calendar_today" label="Ngày tạo" value={formatDate(detail.createdAt)} />
                                        <InfoCard icon="group" label="Tối đa SV" value={`${detail.maxStudents} sinh viên`} />
                                        <InfoCard icon="school" label="Chuyên ngành" value={detail.majorName} />
                                    </div>

                                    {/* Admin-specific info */}
                                    {project && (
                                        <div className="bg-slate-50 rounded-lg p-4 border border-slate-100">
                                            <div className="grid grid-cols-2 gap-4 text-sm">
                                                <div>
                                                    <span className="text-slate-400 text-xs block mb-1">Kỳ học</span>
                                                    <span className="text-slate-700 font-medium">{project.semesterName}</span>
                                                </div>
                                                <div>
                                                    <span className="text-slate-400 text-xs block mb-1">Nhóm</span>
                                                    <span className="text-slate-700 font-medium">{project.groupCode ?? 'Chưa có nhóm'}</span>
                                                </div>
                                                {project.studentNames.length > 0 && (
                                                    <div className="col-span-2">
                                                        <span className="text-slate-400 text-xs block mb-1">Sinh viên</span>
                                                        <div className="flex flex-wrap gap-2">
                                                            {project.studentNames.map((name, idx) => (
                                                                <span key={idx} className="inline-flex items-center gap-1.5 bg-white border border-slate-200 rounded-full px-3 py-1 text-xs font-medium text-slate-700">
                                                                    <span className="material-symbols-outlined text-[14px] text-slate-400">person</span>
                                                                    {name}
                                                                </span>
                                                            ))}
                                                        </div>
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    )}

                                    {/* Mô tả */}
                                    <Section title="Mô tả" icon="description">
                                        <p className="text-sm text-slate-600 leading-relaxed whitespace-pre-line">
                                            {detail.description || 'Chưa có mô tả.'}
                                        </p>
                                    </Section>

                                    {/* Mục tiêu */}
                                    {detail.objectives && (
                                        <Section title="Mục tiêu" icon="flag">
                                            <p className="text-sm text-slate-600 leading-relaxed whitespace-pre-line">
                                                {detail.objectives}
                                            </p>
                                        </Section>
                                    )}

                                    {/* Phạm vi */}
                                    {detail.scope && (
                                        <Section title="Phạm vi & Yêu cầu" icon="tune">
                                            <p className="text-sm text-slate-600 leading-relaxed whitespace-pre-line">
                                                {detail.scope}
                                            </p>
                                        </Section>
                                    )}

                                    {/* Công nghệ */}
                                    {techs.length > 0 && (
                                        <Section title="Công nghệ sử dụng" icon="code">
                                            <div className="flex flex-wrap gap-2">
                                                {techs.map((t) => (
                                                    <span key={t} className="bg-slate-100 text-slate-700 text-xs px-3 py-1 rounded-full font-medium">
                                                        {t}
                                                    </span>
                                                ))}
                                            </div>
                                        </Section>
                                    )}

                                    {/* Kết quả mong đợi */}
                                    {detail.expectedResults && (
                                        <Section title="Kết quả mong đợi" icon="emoji_events">
                                            <p className="text-sm text-slate-600 leading-relaxed whitespace-pre-line">
                                                {detail.expectedResults}
                                            </p>
                                        </Section>
                                    )}

                                    {/* Giảng viên hướng dẫn */}
                                    {detail.mentors.length > 0 && (
                                        <Section title="Giảng viên hướng dẫn" icon="school">
                                            <div className="flex flex-col gap-2">
                                                {detail.mentors.map((m) => (
                                                    <div key={m.mentorId} className="flex items-center gap-3">
                                                        <div className={`w-8 h-8 rounded-full ${getInitialsColor(m.fullName)} flex items-center justify-center text-xs font-bold`}>
                                                            {getInitials(m.fullName)}
                                                        </div>
                                                        <span className="text-sm text-slate-700 font-medium">{m.fullName}</span>
                                                    </div>
                                                ))}
                                            </div>
                                        </Section>
                                    )}

                                    {/* Cập nhật lần cuối */}
                                    {detail.updatedAt && (
                                        <p className="text-xs text-slate-400 pt-2 border-t border-slate-100">
                                            Cập nhật lần cuối: {formatDate(detail.updatedAt)}
                                        </p>
                                    )}
                                </div>
                            )}
                        </div>

                        {/* Footer */}
                        <div className="px-6 py-4 border-t border-slate-100 flex justify-end shrink-0">
                            <button
                                onClick={onClose}
                                className="px-5 py-2 rounded-lg border border-slate-200 text-sm font-medium text-slate-600 hover:bg-slate-50 transition-colors"
                            >
                                Đóng
                            </button>
                        </div>
                    </motion.div>
                </>
            )}
        </AnimatePresence>
    )
}

// ── Section helper ────────────────────────────────────────────────────────────

function Section({ title, icon, children }: { title: string; icon: string; children: React.ReactNode }) {
    return (
        <div>
            <div className="flex items-center gap-2 mb-2">
                <span className="material-symbols-outlined text-base text-primary">{icon}</span>
                <h3 className="text-sm font-bold text-[#101319]">{title}</h3>
            </div>
            {children}
        </div>
    )
}

// ── InfoCard helper ───────────────────────────────────────────────────────────

function InfoCard({ icon, label, value }: { icon: string; label: string; value: string }) {
    return (
        <div className="bg-white border border-slate-100 rounded-lg p-3 flex items-start gap-2.5">
            <span className="material-symbols-outlined text-primary text-[18px] mt-0.5">{icon}</span>
            <div className="min-w-0">
                <p className="text-[11px] text-slate-400 leading-tight">{label}</p>
                <p className="text-sm font-medium text-slate-700 truncate">{value}</p>
            </div>
        </div>
    )
}
