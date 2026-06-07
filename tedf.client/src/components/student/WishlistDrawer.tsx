import { motion, AnimatePresence } from 'framer-motion'
import type { useWishlist } from '@/hooks/useWishlist'

// ── Major color mapping ──────────────────────────────────────────────────────

const majorChipColors: Record<string, string> = {
    CNTT: 'bg-blue-50 text-blue-700 border-blue-200',
    KTPM: 'bg-purple-50 text-purple-700 border-purple-200',
    HTTT: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    KHDL: 'bg-amber-50 text-amber-700 border-amber-200',
}

// ── Component ────────────────────────────────────────────────────────────────

interface WishlistDrawerProps {
    isOpen: boolean
    onClose: () => void
    wishlist: ReturnType<typeof useWishlist>
    onViewDetail: (id: string) => void
}

export function WishlistDrawer({ isOpen, onClose, wishlist, onViewDetail }: WishlistDrawerProps) {
    const items = wishlist.ids.map((id) => ({
        id,
        summary: wishlist.summaries[id],
    })).filter((x) => x.summary) // Only show items with cached summary

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
                        className="fixed right-0 top-0 bottom-0 z-50 w-full max-w-md bg-white shadow-2xl flex flex-col"
                    >
                        {/* Header */}
                        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between shrink-0">
                            <div className="flex items-center gap-3">
                                <span className="material-symbols-outlined text-xl text-primary">bookmark</span>
                                <h2 className="text-lg font-bold text-[#101319]">
                                    Đề tài quan tâm
                                    <span className="ml-2 text-sm font-normal text-slate-400">({wishlist.count})</span>
                                </h2>
                            </div>
                            <div className="flex items-center gap-2">
                                {wishlist.count > 0 && (
                                    <button
                                        onClick={wishlist.clear}
                                        className="text-xs text-slate-400 hover:text-red-500 transition-colors font-medium"
                                    >
                                        Xóa tất cả
                                    </button>
                                )}
                                <button
                                    onClick={onClose}
                                    className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-colors"
                                >
                                    <span className="material-symbols-outlined text-xl">close</span>
                                </button>
                            </div>
                        </div>

                        {/* Content */}
                        <div className="flex-1 overflow-y-auto scrollbar-hide">
                            {items.length === 0 ? (
                                <div className="flex flex-col items-center justify-center h-full text-center px-8">
                                    <span className="material-symbols-outlined text-[56px] text-slate-200 mb-4">bookmark_border</span>
                                    <p className="text-slate-500 font-medium mb-1">Chưa có đề tài nào</p>
                                    <p className="text-slate-400 text-sm">
                                        Nhấn <span className="material-symbols-outlined text-sm align-middle text-red-400">favorite</span> trên đề tài để lưu vào danh sách quan tâm.
                                    </p>
                                </div>
                            ) : (
                                <div className="p-4 flex flex-col gap-2">
                                    <AnimatePresence mode="popLayout">
                                        {items.map(({ id, summary }) => {
                                            const chipColor = majorChipColors[summary!.majorCode] ?? 'bg-slate-50 text-slate-700 border-slate-200'
                                            const isAvailable = summary!.poolStatus === 0

                                            return (
                                                <motion.div
                                                    key={id}
                                                    layout
                                                    initial={{ opacity: 0, x: 20 }}
                                                    animate={{ opacity: 1, x: 0 }}
                                                    exit={{ opacity: 0, x: -20, height: 0, marginBottom: 0 }}
                                                    className="bg-white border border-slate-100 rounded-lg p-4 hover:border-primary/30 hover:bg-primary/[0.02] transition-colors cursor-pointer group"
                                                    onClick={() => { onViewDetail(id); onClose() }}
                                                >
                                                    <div className="flex items-start justify-between gap-3">
                                                        <div className="min-w-0 flex-1">
                                                            <div className="flex items-center gap-2 mb-1.5">
                                                                <span className={`${chipColor} px-2 py-0.5 rounded text-[9px] font-bold uppercase tracking-wider border`}>
                                                                    {summary!.majorCode}
                                                                </span>
                                                                <span className={`flex items-center gap-1 text-[10px] font-medium ${isAvailable ? 'text-green-600' : 'text-slate-400'}`}>
                                                                    <span className={`w-1.5 h-1.5 rounded-full ${isAvailable ? 'bg-green-500' : 'bg-slate-300'}`} />
                                                                    {isAvailable ? 'Còn trống' : 'Đã có nhóm'}
                                                                </span>
                                                            </div>
                                                            <h4 className="text-sm font-semibold text-[#101319] leading-snug line-clamp-2 group-hover:text-primary transition-colors">
                                                                {summary!.nameVi}
                                                            </h4>
                                                            <p className="text-xs text-slate-400 mt-1 flex items-center gap-1">
                                                                <span className="material-symbols-outlined text-xs">school</span>
                                                                {summary!.mentorName}
                                                            </p>
                                                        </div>
                                                        <button
                                                            onClick={(e) => { e.stopPropagation(); wishlist.toggle(id) }}
                                                            className="p-1 rounded hover:bg-red-50 text-slate-300 hover:text-red-400 transition-colors shrink-0"
                                                            title="Bỏ quan tâm"
                                                        >
                                                            <span className="material-symbols-outlined text-lg">close</span>
                                                        </button>
                                                    </div>
                                                </motion.div>
                                            )
                                        })}
                                    </AnimatePresence>
                                </div>
                            )}
                        </div>
                    </motion.div>
                </>
            )}
        </AnimatePresence>
    )
}
