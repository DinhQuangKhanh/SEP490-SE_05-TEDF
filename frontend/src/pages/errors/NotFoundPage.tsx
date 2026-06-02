import { motion } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'

const roleHomeMap: Record<string, string> = {
    admin: '/admin',
    mentor: '/mentor',
    evaluator: '/evaluator',
    student: '/student',
}

export function NotFoundPage() {
    const navigate = useNavigate()
    const { user, isAuthenticated } = useAuth()

    const homePath = isAuthenticated && user ? (roleHomeMap[user.role] ?? '/login') : '/login'

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-slate-100 flex items-center justify-center p-6 relative overflow-hidden">
            {/* Background decorations */}
            <div className="absolute inset-0 overflow-hidden">
                <div className="absolute -top-32 -right-32 w-72 h-72 bg-primary/5 rounded-full blur-3xl" />
                <div className="absolute -bottom-32 -left-32 w-72 h-72 bg-primary/10 rounded-full blur-3xl" />
            </div>

            {/* Grid pattern */}
            <div
                className="absolute inset-0 opacity-[0.03]"
                style={{
                    backgroundImage: 'radial-gradient(circle at 1px 1px, #2c6090 1px, transparent 0)',
                    backgroundSize: '32px 32px',
                }}
            />

            <motion.div
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.6, ease: 'easeOut' }}
                className="relative z-10 max-w-md w-full text-center"
            >
                {/* 404 Number */}
                <motion.div
                    initial={{ opacity: 0, scale: 0.8 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 0.1, duration: 0.5, type: 'spring', stiffness: 200 }}
                    className="mb-6"
                >
                    <span className="text-[120px] sm:text-[160px] font-extrabold leading-none tracking-tighter bg-gradient-to-br from-primary/80 to-primary/30 bg-clip-text text-transparent select-none">
                        404
                    </span>
                </motion.div>

                {/* Icon */}
                <motion.div
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.2, duration: 0.4 }}
                    className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-primary/10 border border-primary/15 mb-6"
                >
                    <span className="material-symbols-outlined text-primary text-[36px]">explore_off</span>
                </motion.div>

                {/* Title */}
                <motion.h1
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.25, duration: 0.4 }}
                    className="text-2xl sm:text-3xl font-bold text-slate-800 mb-3 tracking-tight"
                >
                    Không tìm thấy trang
                </motion.h1>

                {/* Description */}
                <motion.p
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.3, duration: 0.4 }}
                    className="text-slate-500 text-sm sm:text-base leading-relaxed mb-8 max-w-sm mx-auto"
                >
                    Trang bạn đang tìm kiếm không tồn tại hoặc đã bị di chuyển.
                    Vui lòng kiểm tra lại đường dẫn.
                </motion.p>

                {/* Actions */}
                <motion.div
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.35, duration: 0.4 }}
                    className="flex flex-col sm:flex-row items-center justify-center gap-3"
                >
                    <button
                        onClick={() => navigate(-1)}
                        className="flex items-center gap-2 px-5 py-2.5 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 hover:border-slate-300 transition-all shadow-sm"
                    >
                        <span className="material-symbols-outlined text-lg">arrow_back</span>
                        Quay lại trang trước
                    </button>
                    <button
                        onClick={() => navigate(homePath, { replace: true })}
                        className="flex items-center gap-2 px-5 py-2.5 text-sm font-medium text-white rounded-xl transition-all shadow-sm"
                        style={{ backgroundColor: 'var(--color-primary)' }}
                    >
                        <span className="material-symbols-outlined text-lg">home</span>
                        Về trang chủ
                    </button>
                </motion.div>

                {/* Footer hint */}
                <motion.p
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ delay: 0.5, duration: 0.4 }}
                    className="mt-10 text-xs text-slate-400"
                >
                    Nếu bạn cho rằng đây là lỗi hệ thống, vui lòng liên hệ{' '}
                    <a href="#" className="underline hover:text-primary transition-colors">
                        bộ phận hỗ trợ kỹ thuật
                    </a>
                </motion.p>
            </motion.div>
        </div>
    )
}
