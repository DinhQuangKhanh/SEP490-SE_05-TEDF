import { motion } from 'framer-motion'
import { useAuth } from '@/contexts/AuthContext'

export function MaintenancePage() {
    const { isAuthenticated, logout } = useAuth()

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 flex items-center justify-center p-6 relative overflow-hidden">
            {/* Animated background elements */}
            <div className="absolute inset-0 overflow-hidden">
                <div className="absolute -top-40 -right-40 w-80 h-80 bg-orange-500/10 rounded-full blur-3xl animate-pulse" />
                <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-amber-500/10 rounded-full blur-3xl animate-pulse" style={{ animationDelay: '1s' }} />
                <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] bg-orange-500/5 rounded-full blur-3xl" />
            </div>

            {/* Grid pattern overlay */}
            <div
                className="absolute inset-0 opacity-5"
                style={{
                    backgroundImage: `radial-gradient(circle at 1px 1px, white 1px, transparent 0)`,
                    backgroundSize: '40px 40px',
                }}
            />

            <motion.div
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.8, ease: 'easeOut' }}
                className="relative z-10 max-w-lg w-full text-center"
            >
                {/* Animated gear icon */}
                <motion.div
                    animate={{ rotate: 360 }}
                    transition={{ duration: 8, repeat: Infinity, ease: 'linear' }}
                    className="inline-block mb-8"
                >
                    <div className="w-24 h-24 rounded-full bg-gradient-to-br from-orange-500 to-amber-500 flex items-center justify-center shadow-lg shadow-orange-500/30">
                        <span className="material-symbols-outlined text-white text-[48px]">engineering</span>
                    </div>
                </motion.div>

                {/* Title */}
                <motion.h1
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.2, duration: 0.6 }}
                    className="text-4xl font-bold text-white mb-4 tracking-tight"
                >
                    Hệ thống đang bảo trì
                </motion.h1>

                {/* Subtitle */}
                <motion.p
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.4, duration: 0.6 }}
                    className="text-lg text-slate-300 mb-8 leading-relaxed"
                >
                    Chúng tôi đang thực hiện nâng cấp và bảo trì hệ thống để mang đến trải nghiệm tốt hơn. Vui lòng quay lại sau.
                </motion.p>

                {/* Info card */}
                <motion.div
                    initial={{ opacity: 0, scale: 0.95 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 0.6, duration: 0.6 }}
                    className="bg-white/5 backdrop-blur-xl border border-white/10 rounded-2xl p-6 mb-8"
                >
                    <div className="flex items-center gap-3 justify-center mb-4">
                        <span className="material-symbols-outlined text-amber-400">schedule</span>
                        <span className="text-sm font-semibold text-amber-400 uppercase tracking-wider">Thông tin bảo trì</span>
                    </div>
                    <div className="space-y-3">
                        <div className="flex items-center justify-between text-sm">
                            <span className="text-slate-400">Trạng thái</span>
                            <span className="text-orange-400 font-medium flex items-center gap-1.5">
                                <span className="w-2 h-2 bg-orange-400 rounded-full animate-pulse" />
                                Đang bảo trì
                            </span>
                        </div>
                        <div className="border-t border-white/5" />
                        <div className="flex items-center justify-between text-sm">
                            <span className="text-slate-400">Dự kiến hoàn thành</span>
                            <span className="text-slate-200 font-medium">Sớm nhất có thể</span>
                        </div>
                        <div className="border-t border-white/5" />
                        <div className="flex items-center justify-between text-sm">
                            <span className="text-slate-400">Liên hệ hỗ trợ</span>
                            <span className="text-slate-200 font-medium">support@unithesis.edu.vn</span>
                        </div>
                    </div>
                </motion.div>

                {/* Action buttons */}
                <motion.div
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.8, duration: 0.6 }}
                    className="flex flex-col sm:flex-row gap-3 justify-center"
                >
                    <button
                        onClick={() => window.location.reload()}
                        className="px-6 py-3 bg-gradient-to-r from-orange-500 to-amber-500 text-white font-semibold rounded-xl hover:from-orange-600 hover:to-amber-600 transition-all shadow-lg shadow-orange-500/25 flex items-center gap-2 justify-center"
                    >
                        <span className="material-symbols-outlined text-[20px]">refresh</span>
                        Thử lại
                    </button>
                    {isAuthenticated && (
                        <button
                            onClick={logout}
                            className="px-6 py-3 bg-white/10 text-white font-semibold rounded-xl hover:bg-white/20 transition-all border border-white/10 flex items-center gap-2 justify-center"
                        >
                            <span className="material-symbols-outlined text-[20px]">logout</span>
                            Đăng xuất
                        </button>
                    )}
                </motion.div>

                {/* Footer */}
                <motion.p
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ delay: 1, duration: 0.6 }}
                    className="mt-10 text-xs text-slate-500"
                >
                    UniThesis © 2026 — Hệ thống quản lý luận văn
                </motion.p>
            </motion.div>
        </div>
    )
}
