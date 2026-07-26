import { motion } from 'framer-motion'
import { useAuth } from '@/contexts/AuthContext'

interface BlockedDisplay {
    icon: string
    title: string
    fallback: string
}

/** Per-gate-reason icon, heading and default message. */
const BLOCKED_DISPLAY: Record<string, BlockedDisplay> = {
    locked: { icon: 'lock', title: 'Tài khoản đã bị khóa', fallback: 'Tài khoản của bạn đã bị khóa.' },
    inactive: { icon: 'person_off', title: 'Tài khoản đã bị vô hiệu hóa', fallback: 'Tài khoản của bạn đã bị vô hiệu hóa.' },
    student_not_eligible: {
        icon: 'school',
        title: 'Chưa đủ điều kiện',
        fallback: 'Bạn không thuộc danh sách sinh viên đủ điều kiện làm đồ án trong học kỳ hiện tại hoặc sắp tới.',
    },
    mentor_not_eligible: {
        icon: 'school',
        title: 'Chưa được phân công',
        fallback: 'Bạn không thuộc danh sách giảng viên được phân công trong học kỳ hiện tại hoặc sắp tới.',
    },
}

const DEFAULT_BLOCKED_DISPLAY: BlockedDisplay = {
    icon: 'block',
    title: 'Không thể truy cập',
    fallback: 'Tài khoản của bạn hiện không thể truy cập hệ thống.',
}

/**
 * Shown by ProtectedRoute when the server access gate denies the account
 * (locked / inactive / student-not-eligible). The user is authenticated with
 * Firebase but not allowed to use the system, so we stop them here and offer logout.
 */
export function AccountBlockedPage() {
    const { access, logout } = useAuth()

    const kind = access?.kind ?? null
    const reason = access?.reason

    const config = (kind && BLOCKED_DISPLAY[kind]) || DEFAULT_BLOCKED_DISPLAY

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-red-50/30 flex items-center justify-center p-6 relative overflow-hidden">
            <div className="absolute inset-0 overflow-hidden">
                <div className="absolute -top-32 -right-32 w-72 h-72 bg-red-500/5 rounded-full blur-3xl" />
                <div className="absolute -bottom-32 -left-32 w-72 h-72 bg-red-500/10 rounded-full blur-3xl" />
            </div>

            <motion.div
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.5, ease: 'easeOut' }}
                className="relative z-10 max-w-md w-full text-center"
            >
                <motion.div
                    initial={{ opacity: 0, scale: 0.8 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 0.1, duration: 0.4, type: 'spring', stiffness: 200 }}
                    className="inline-flex items-center justify-center w-20 h-20 rounded-2xl bg-red-50 border border-red-100 mb-6"
                >
                    <span className="material-symbols-outlined text-red-500 text-[44px]">{config.icon}</span>
                </motion.div>

                <h1 className="text-2xl sm:text-3xl font-bold text-slate-800 mb-3 tracking-tight">{config.title}</h1>

                <p className="text-slate-500 text-sm sm:text-base leading-relaxed mb-8 max-w-sm mx-auto">
                    {reason || config.fallback} Vui lòng liên hệ quản trị viên nếu bạn cho rằng đây là nhầm lẫn.
                </p>

                <button
                    type="button"
                    onClick={() => logout()}
                    className="inline-flex items-center gap-2 px-6 py-2.5 text-sm font-medium text-white rounded-xl transition-all shadow-sm"
                    style={{ backgroundColor: 'var(--color-primary)' }}
                >
                    <span className="material-symbols-outlined text-lg">logout</span>
                    Đăng xuất
                </button>
            </motion.div>
        </div>
    )
}
