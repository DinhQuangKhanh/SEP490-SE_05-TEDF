import { useEffect, useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { topicPoolService, type TopicDetail } from '@/lib/topicPoolService'

// ── Major color mapping ──────────────────────────────────────────────────────

const majorChipColors: Record<string, string> = {
    CNTT: 'bg-blue-50 text-blue-700 border-blue-200',
    KTPM: 'bg-purple-50 text-purple-700 border-purple-200',
    HTTT: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    KHDL: 'bg-amber-50 text-amber-700 border-amber-200',
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

// ── Component ────────────────────────────────────────────────────────────────

interface TopicDetailDrawerProps {
    projectId: string | null
    isOpen: boolean
    onClose: () => void
    isFavorite: boolean
    onToggleFavorite: () => void
    groupHasProject?: boolean
    hasGroup?: boolean
    isLeader?: boolean
    onRegister?: (projectId: string) => void
}

export function TopicDetailDrawer({ projectId, isOpen, onClose, isFavorite, onToggleFavorite, groupHasProject, hasGroup, isLeader, onRegister }: TopicDetailDrawerProps) {
    const [detail, setDetail] = useState<TopicDetail | null>(null)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    useEffect(() => {
        if (!projectId || !isOpen) return
        setLoading(true)
        setError(null)
        topicPoolService.getTopicDetail(projectId)
            .then(setDetail)
            .catch((e) => setError(e.message))
            .finally(() => setLoading(false))
    }, [projectId, isOpen])

    const isAvailable = detail?.poolStatus === 0
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
                        className="fixed right-0 top-0 bottom-0 z-50 w-full max-w-lg bg-white shadow-2xl flex flex-col"
                    >
                        {/* Header */}
                        <div className="px-6 py-4 border-b border-slate-100 flex items-start justify-between gap-4 shrink-0">
                            <div className="min-w-0">
                                {detail && (
                                    <div className="flex items-center gap-2 mb-2">
                                        <span className={`${chipColor} px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border`}>
                                            {detail.majorCode}
                                        </span>
                                        <div className="flex items-center gap-1.5">
                                            <span className={`w-2 h-2 rounded-full ${isAvailable ? 'bg-green-500' : 'bg-slate-300'}`} />
                                            <span className={`text-[11px] font-medium ${isAvailable ? 'text-green-600' : 'text-slate-400'}`}>
                                                {isAvailable ? 'Còn trống' : 'Đã có nhóm'}
                                            </span>
                                        </div>
                                    </div>
                                )}
                                <h2 className="text-lg font-bold text-[#101319] leading-tight">
                                    {loading ? 'Đang tải...' : detail?.nameVi ?? 'Đề tài'}
                                </h2>
                                {detail?.nameEn && (
                                    <p className="text-xs text-slate-400 mt-0.5 italic">{detail.nameEn}</p>
                                )}
                            </div>
                            <button
                                onClick={onClose}
                                className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-colors shrink-0"
                            >
                                <span className="material-symbols-outlined text-xl">close</span>
                            </button>
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
                                    {/* Code & info bar */}
                                    <div className="flex items-center gap-3 text-xs text-slate-400">
                                        <span>Mã: <span className="font-mono text-slate-600">{detail.code}</span></span>
                                        <span>•</span>
                                        <span>Tối đa: <span className="font-semibold text-slate-600">{detail.maxStudents} SV</span></span>
                                    </div>

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

                                    {/* Mentors */}
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
                                </div>
                            )}
                        </div>

                        {/* Footer */}
                        {detail && !loading && (
                            <div className="px-6 py-4 border-t border-slate-100 flex gap-3 shrink-0">
                                <motion.button
                                    whileTap={{ scale: 0.9 }}
                                    onClick={onToggleFavorite}
                                    className={`border py-2.5 px-4 rounded-lg transition-colors flex items-center gap-2 text-sm font-medium ${isFavorite
                                        ? 'border-red-200 bg-red-50 text-red-500'
                                        : 'bg-white border-slate-200 text-slate-500 hover:text-red-500 hover:border-red-200 hover:bg-red-50'
                                        }`}
                                >
                                    <span className={`material-symbols-outlined text-lg ${isFavorite ? 'fill-1' : ''}`}>favorite</span>
                                    {isFavorite ? 'Đã quan tâm' : 'Quan tâm'}
                                </motion.button>

                                {!hasGroup ? (
                                    <button className="flex-1 bg-slate-200 text-slate-400 py-2.5 rounded-lg text-sm font-bold cursor-not-allowed flex items-center justify-center gap-2" disabled>
                                        <span className="material-symbols-outlined text-lg">group_add</span>
                                        Tham gia nhóm trước
                                    </button>
                                ) : groupHasProject ? (
                                    <button className="flex-1 bg-slate-200 text-slate-400 py-2.5 rounded-lg text-sm font-bold cursor-not-allowed flex items-center justify-center gap-2" disabled>
                                        <span className="material-symbols-outlined text-lg">block</span>
                                        Nhóm đã có đề tài
                                    </button>
                                ) : !isLeader ? (
                                    <button className="flex-1 bg-slate-200 text-slate-400 py-2.5 rounded-lg text-sm font-bold cursor-not-allowed flex items-center justify-center gap-2" disabled>
                                        <span className="material-symbols-outlined text-lg">lock</span>
                                        Chỉ nhóm trưởng đăng ký
                                    </button>
                                ) : isAvailable ? (
                                    <button
                                        onClick={() => detail && onRegister?.(detail.id)}
                                        className="flex-1 bg-primary text-white py-2.5 rounded-lg text-sm font-bold hover:bg-primary-light transition-colors flex items-center justify-center gap-2"
                                    >
                                        <span className="material-symbols-outlined text-lg">app_registration</span>
                                        Đăng ký đề tài
                                    </button>
                                ) : (
                                    <button className="flex-1 bg-slate-200 text-slate-400 py-2.5 rounded-lg text-sm font-bold cursor-not-allowed flex items-center justify-center gap-2" disabled>
                                        <span className="material-symbols-outlined text-lg">check_circle</span>
                                        Đã có nhóm đăng ký
                                    </button>
                                )}
                            </div>
                        )}
                    </motion.div>
                </>
            )}
        </AnimatePresence>
    )
}

// ── Section helper ───────────────────────────────────────────────────────────

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
