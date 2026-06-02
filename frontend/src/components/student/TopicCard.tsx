import { motion } from 'framer-motion'
import type { TopicInPoolItem } from '@/lib/topicPoolService'

// ── Major color mapping ──────────────────────────────────────────────────────

const majorColors: Record<string, { bar: string; chip: string }> = {
    CNTT: { bar: 'bg-blue-500', chip: 'bg-blue-50 text-blue-700 border-blue-200' },
    KTPM: { bar: 'bg-purple-500', chip: 'bg-purple-50 text-purple-700 border-purple-200' },
    HTTT: { bar: 'bg-emerald-500', chip: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
    KHDL: { bar: 'bg-amber-500', chip: 'bg-amber-50 text-amber-700 border-amber-200' },
}

const defaultColor = { bar: 'bg-slate-500', chip: 'bg-slate-50 text-slate-700 border-slate-200' }

// ── Helpers ──────────────────────────────────────────────────────────────────

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

interface TopicCardProps {
    topic: TopicInPoolItem
    isFavorite: boolean
    onToggleFavorite: () => void
    onViewDetail: () => void
    groupHasProject?: boolean
}

export function TopicCard({ topic, isFavorite, onToggleFavorite, onViewDetail, groupHasProject }: TopicCardProps) {
    const colors = majorColors[topic.majorCode] ?? defaultColor
    const techs = parseTechnologies(topic.technologies)
    const isAvailable = topic.poolStatus === 0 // PoolTopicStatus.Available

    return (
        <motion.div
            variants={{ hidden: { opacity: 0, y: 20 }, show: { opacity: 1, y: 0 } }}
            className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm hover:shadow-lg hover:-translate-y-0.5 transition-all duration-200 group overflow-hidden flex flex-col relative"
        >
            {/* Top color bar */}
            <div className={`h-[3px] ${colors.bar}`} />

            {/* Content */}
            <div className="p-5 flex-1 flex flex-col">
                {/* Major chip + Status */}
                <div className="flex justify-between items-center mb-3">
                    <span className={`${colors.chip} px-2.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border`}>
                        {topic.majorCode}
                    </span>
                    <div className="flex items-center gap-1.5">
                        <span className={`w-2 h-2 rounded-full ${isAvailable ? 'bg-green-500' : 'bg-slate-300'}`} />
                        <span className={`text-[11px] font-medium ${isAvailable ? 'text-green-600' : 'text-slate-400'}`}>
                            {isAvailable ? 'Còn trống' : 'Đã có nhóm'}
                        </span>
                    </div>
                </div>

                {/* Title */}
                <h3 className="text-[15px] font-bold text-[#101319] group-hover:text-primary transition-colors leading-snug mb-2 line-clamp-2">
                    {topic.nameVi}
                </h3>

                {/* Description */}
                <p className="text-xs text-slate-500 leading-relaxed mb-3 line-clamp-2">
                    {topic.description || 'Chưa có mô tả.'}
                </p>

                {/* Technology pills */}
                {techs.length > 0 && (
                    <div className="flex flex-wrap gap-1.5 mb-3">
                        {techs.slice(0, 3).map((t) => (
                            <span key={t} className="bg-slate-100 text-slate-600 text-[10px] px-2 py-0.5 rounded-full font-medium">
                                {t}
                            </span>
                        ))}
                        {techs.length > 3 && (
                            <span className="bg-slate-100 text-slate-400 text-[10px] px-2 py-0.5 rounded-full font-medium">
                                +{techs.length - 3}
                            </span>
                        )}
                    </div>
                )}

                {/* Spacer */}
                <div className="flex-1" />

                {/* Mentor + Max students */}
                <div className="flex items-center justify-between pt-3 border-t border-slate-100">
                    <div className="flex items-center gap-2 min-w-0">
                        <div className={`w-7 h-7 rounded-full ${getInitialsColor(topic.mentorName)} flex items-center justify-center text-[10px] font-bold shrink-0`}>
                            {getInitials(topic.mentorName)}
                        </div>
                        <span className="text-xs text-slate-600 font-medium truncate">
                            {topic.mentorName}
                        </span>
                    </div>
                    <span className="text-[10px] text-slate-400 shrink-0 ml-2">
                        Tối đa: {topic.maxStudents} SV
                    </span>
                </div>
            </div>

            {/* Footer actions */}
            <div className="px-5 py-3 bg-slate-50/50 border-t border-[#e9ecf1] flex gap-2">
                <motion.button
                    whileTap={{ scale: 0.85 }}
                    onClick={(e) => { e.stopPropagation(); onToggleFavorite() }}
                    className={`border py-2 px-2.5 rounded-lg transition-colors flex items-center justify-center ${isFavorite
                        ? 'border-red-200 bg-red-50 text-red-500'
                        : 'bg-white border-[#e9ecf1] text-slate-400 hover:text-red-500 hover:border-red-200 hover:bg-red-50'
                        }`}
                    title="Quan tâm"
                >
                    <span className={`material-symbols-outlined text-lg ${isFavorite ? 'fill-1' : ''}`}>favorite</span>
                </motion.button>

                <button
                    onClick={onViewDetail}
                    className="flex-1 bg-white border border-[#e9ecf1] text-[#101319] py-2 rounded-lg text-xs font-bold hover:bg-slate-50 transition-colors flex items-center justify-center gap-1.5"
                >
                    <span className="material-symbols-outlined text-base">visibility</span>
                    Chi tiết
                </button>

                {groupHasProject ? (
                    <button className="flex-1 bg-slate-200 text-slate-400 py-2 rounded-lg text-xs font-bold cursor-not-allowed flex items-center justify-center gap-1.5" disabled>
                        <span className="material-symbols-outlined text-base">block</span>
                        Nhóm đã có đề tài
                    </button>
                ) : isAvailable ? (
                    <button className="flex-1 bg-primary text-white py-2 rounded-lg text-xs font-bold hover:bg-primary-light transition-colors flex items-center justify-center gap-1.5">
                        <span className="material-symbols-outlined text-base">app_registration</span>
                        Đăng ký
                    </button>
                ) : (
                    <button className="flex-1 bg-slate-200 text-slate-400 py-2 rounded-lg text-xs font-bold cursor-not-allowed flex items-center justify-center gap-1.5" disabled>
                        <span className="material-symbols-outlined text-base">check_circle</span>
                        Đã có nhóm
                    </button>
                )}
            </div>
        </motion.div>
    )
}

// ── Skeleton ─────────────────────────────────────────────────────────────────

export function TopicCardSkeleton() {
    return (
        <div className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm overflow-hidden flex flex-col animate-pulse">
            <div className="h-[3px] bg-slate-200" />
            <div className="p-5 flex-1 flex flex-col gap-3">
                <div className="flex justify-between">
                    <div className="h-5 w-12 bg-slate-200 rounded" />
                    <div className="h-4 w-16 bg-slate-200 rounded" />
                </div>
                <div className="h-5 w-full bg-slate-200 rounded" />
                <div className="h-5 w-3/4 bg-slate-200 rounded" />
                <div className="h-4 w-full bg-slate-100 rounded" />
                <div className="h-4 w-2/3 bg-slate-100 rounded" />
                <div className="flex gap-1.5">
                    <div className="h-5 w-14 bg-slate-100 rounded-full" />
                    <div className="h-5 w-16 bg-slate-100 rounded-full" />
                </div>
                <div className="flex-1" />
                <div className="flex items-center justify-between pt-3 border-t border-slate-100">
                    <div className="flex items-center gap-2">
                        <div className="w-7 h-7 rounded-full bg-slate-200" />
                        <div className="h-3 w-28 bg-slate-200 rounded" />
                    </div>
                    <div className="h-3 w-16 bg-slate-200 rounded" />
                </div>
            </div>
            <div className="px-5 py-3 bg-slate-50/50 border-t border-[#e9ecf1] flex gap-2">
                <div className="h-9 w-10 bg-slate-200 rounded-lg" />
                <div className="h-9 flex-1 bg-slate-200 rounded-lg" />
                <div className="h-9 flex-1 bg-slate-200 rounded-lg" />
            </div>
        </div>
    )
}
