import { motion } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'

const roleHomeMap: Record<string, string> = {
    admin: '/admin',
    mentor: '/mentor',
    evaluator: '/evaluator',
    student: '/student',
}

const roleLabelMap: Record<string, string> = {
    admin: 'Quản trị viên',
    mentor: 'Giảng viên hướng dẫn',
    evaluator: 'Phản biện',
    student: 'Sinh viên',
}

export function AccessDeniedPage() {
    const navigate = useNavigate()
    const { user, isAuthenticated, logout } = useAuth()

    const homePath = isAuthenticated && user ? (roleHomeMap[user.role] ?? '/login') : '/login'
    const roleLabel = user ? (roleLabelMap[user.role] ?? user.role) : ''

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-red-50/30 flex items-center justify-center p-6 relative overflow-hidden">
            {/* Background decorations */}
            <div className="absolute inset-0 overflow-hidden">
                <div className="absolute -top-32 -right-32 w-72 h-72 bg-red-500/5 rounded-full blur-3xl" />
                <div className="absolute -bottom-32 -left-32 w-72 h-72 bg-red-500/10 rounded-full blur-3xl" />
            </div>

            {/* Grid pattern */}
            <div
                className="absolute inset-0 opacity-[0.03]"
                style={{
                    backgroundImage: 'radial-gradient(circle at 1px 1px, #dc2626 1px, transparent 0)',
                    backgroundSize: '32px 32px',
                }}
            />

            <motion.div
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.6, ease: 'easeOut' }}
                className="relative z-10 max-w-md w-full text-center"
            >
                {/* 403 Number */}
                <motion.div
                    initial={{ opacity: 0, scale: 0.8 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 0.1, duration: 0.5, type: 'spring', stiffness: 200 }}
                    className="mb-6"
                >
                    <span className="text-[120px] sm:text-[160px] font-extrabold leading-none tracking-tighter bg-gradient-to-br from-red-500/80 to-red-400/30 bg-clip-text text-transparent select-none">
                        403
                    </span>
                </motion.div>

                {/* Icon */}
                <motion.div
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.2, duration: 0.4 }}
                    className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-red-50 border border-red-100 mb-6"
                >
                    <span className="material-symbols-outlined text-red-500 text-[36px]">shield_lock</span>
                </motion.div>

                {/* Title */}
                <motion.h1
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.25, duration: 0.4 }}
                    className="text-2xl sm:text-3xl font-bold text-slate-800 mb-3 tracking-tight"
                >
                    Truy cập bị từ chối
                </motion.h1>

                {/* Description */}
                <motion.p
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.3, duration: 0.4 }}
                    className="text-slate-500 text-sm sm:text-base leading-relaxed mb-4 max-w-sm mx-auto"
                >
                    Bạn không có quyền truy cập vào trang này.
                    Tài khoản của bạn không được phân quyền để sử dụng chức năng này.
                </motion.p>

                {/* Role badge */}
                {user && (
                    <motion.div
                        initial={{ opacity: 0, y: 10 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ delay: 0.35, duration: 0.4 }}
                        className="inline-flex items-center gap-2 px-4 py-2 bg-slate-100 border border-slate-200 rounded-full mb-8"
                    >
                        <span className="material-symbols-outlined text-slate-400 text-base">person</span>
                        <span className="text-sm text-slate-600">
                            Vai trò hiện tại: <strong className="text-slate-800">{roleLabel}</strong>
                        </span>
                    </motion.div>
                )}

                {/* Actions */}
                <motion.div
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.4, duration: 0.4 }}
                    className="flex flex-col sm:flex-row items-center justify-center gap-3"
                >
                    <button
                        onClick={() => navigate(homePath, { replace: true })}
                        className="flex items-center gap-2 px-5 py-2.5 text-sm font-medium text-white rounded-xl transition-all shadow-sm"
                        style={{ backgroundColor: 'var(--color-primary)' }}
                    >
                        <span className="material-symbols-outlined text-lg">home</span>
                        Về trang chủ
                    </button>
                    <button
                        onClick={() => navigate(-1)}
                        className="flex items-center gap-2 px-5 py-2.5 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 hover:border-slate-300 transition-all shadow-sm"
                    >
                        <span className="material-symbols-outlined text-lg">arrow_back</span>
                        Quay lại
                    </button>
                    <button
                        onClick={() => logout()}
                        className="flex items-center gap-2 px-5 py-2.5 text-sm font-medium text-red-600 bg-white border border-red-200 rounded-xl hover:bg-red-50 hover:border-red-300 transition-all shadow-sm"
                    >
                        <span className="material-symbols-outlined text-lg">logout</span>
                        Đăng xuất
                    </button>
                </motion.div>

                {/* Footer hint */}
                <motion.p
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ delay: 0.5, duration: 0.4 }}
                    className="mt-10 text-xs text-slate-400"
                >
                    Nếu bạn cho rằng đây là lỗi phân quyền, vui lòng liên hệ{' '}
                    <a href="#" className="underline hover:text-primary transition-colors">
                        quản trị viên hệ thống
                    </a>
                </motion.p>
            </motion.div>
        </div>
    )
}
