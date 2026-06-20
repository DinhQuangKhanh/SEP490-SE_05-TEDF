import { motion } from 'framer-motion'
import { Header } from '@/components/layout'
import { useAuth } from '@/contexts/AuthContext'

const container = {
    hidden: { opacity: 0 },
    show: {
        opacity: 1,
        transition: {
            staggerChildren: 0.1
        }
    }
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 }
}

const roleLabels: Record<string, string> = {
    admin: 'Quản trị viên',
    mentor: 'Giảng viên hướng dẫn',
    evaluator: 'Người thẩm định',
    student: 'Sinh viên',
    departmenthead: 'Chủ nhiệm bộ môn',
}

const roleColors: Record<string, string> = {
    admin: 'bg-blue-500/10 text-blue-600 border-blue-500/20',
    mentor: 'bg-emerald-500/10 text-emerald-600 border-emerald-500/20',
    evaluator: 'bg-purple-500/10 text-purple-600 border-purple-500/20',
    student: 'bg-amber-500/10 text-amber-600 border-amber-500/20',
    departmenthead: 'bg-rose-500/10 text-rose-600 border-rose-500/20',
}

const roleIcons: Record<string, string> = {
    admin: 'admin_panel_settings',
    mentor: 'school',
    evaluator: 'rate_review',
    student: 'person',
    departmenthead: 'supervisor_account',
}

export function ProfilePage() {
    const { user } = useAuth()

    if (!user) return null

    const initials = user.name
        .split(' ')
        .map(w => w[0])
        .join('')
        .slice(-2)
        .toUpperCase()

    return (
        <>
            <Header title="Thông Tin Cá Nhân" />

            <div className="flex-1 overflow-y-auto p-8 scrollbar-hide bg-slate-50">
                <motion.div
                    variants={container}
                    initial="hidden"
                    animate="show"
                    className="space-y-6 max-w-5xl mx-auto"
                >
                    {/* Profile Hero Card */}
                    <motion.div
                        variants={item}
                        className="bento-card rounded-md overflow-hidden"
                    >
                        {/* Banner */}
                        <div className="h-32 bg-gradient-to-r from-primary via-primary/80 to-primary/60 relative">
                            <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNjAiIGhlaWdodD0iNjAiIHZpZXdCb3g9IjAgMCA2MCA2MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48ZyBmaWxsPSJub25lIiBmaWxsLXJ1bGU9ImV2ZW5vZGQiPjxnIGZpbGw9IiNmZmYiIGZpbGwtb3BhY2l0eT0iMC4wNSI+PHBhdGggZD0iTTM2IDM0djItSDJ2LTJoMnptMC00VjhoMnYyMmgtMlYzMHptLTE2IDZWMjJoMnYxNGgtMnptMTAtMTBWMTRoMnYxMmgtMnoiLz48L2c+PC9nPjwvc3ZnPg==')] opacity-50" />
                        </div>

                        {/* Profile Info */}
                        <div className="px-8 pb-8 -mt-14 relative">
                            <div className="flex items-end gap-6">
                                {/* Avatar */}
                                <div className="relative">
                                    {user.avatar ? (
                                        <img
                                            src={user.avatar}
                                            alt={user.name}
                                            className="w-28 h-28 rounded-2xl border-4 border-white shadow-lg object-cover"
                                        />
                                    ) : (
                                        <div className="w-28 h-28 rounded-2xl border-4 border-white shadow-lg bg-gradient-to-br from-primary to-primary/70 flex items-center justify-center">
                                            <span className="text-3xl font-bold text-white">{initials}</span>
                                        </div>
                                    )}
                                    <div className="absolute -bottom-1 -right-1 w-6 h-6 bg-green-500 rounded-full border-2 border-white" title="Đang hoạt động" />
                                </div>

                                {/* Name & Role */}
                                <div className="flex-1 pb-1">
                                    <h2 className="text-2xl font-bold text-slate-800">{user.name}</h2>
                                    <p className="text-slate-500 mt-0.5">{user.email}</p>
                                    <div className="flex flex-wrap gap-2 mt-3">
                                        {user.roles.map((role) => (
                                            <span
                                                key={role}
                                                className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold border ${roleColors[role] ?? 'bg-slate-100 text-slate-600 border-slate-200'}`}
                                            >
                                                <span className="material-symbols-outlined text-[14px]">
                                                    {roleIcons[role] ?? 'badge'}
                                                </span>
                                                {roleLabels[role] ?? role}
                                            </span>
                                        ))}
                                    </div>
                                </div>
                            </div>
                        </div>
                    </motion.div>

                    {/* Detail Cards Grid */}
                    <motion.div variants={item} className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                        {/* Thông tin cơ bản */}
                        <div className="bento-card p-6 rounded-md">
                            <div className="flex items-center gap-2 mb-5">
                                <div className="p-2 bg-blue-500/10 rounded-md text-blue-600">
                                    <span className="material-symbols-outlined">person</span>
                                </div>
                                <h3 className="text-slate-800 text-lg font-bold">Thông Tin Cơ Bản</h3>
                            </div>
                            <div className="space-y-4">
                                <InfoRow
                                    icon="badge"
                                    label="Họ và tên"
                                    value={user.name}
                                />
                                <InfoRow
                                    icon="mail"
                                    label="Email"
                                    value={user.email}
                                />
                                <InfoRow
                                    icon="fingerprint"
                                    label="ID tài khoản"
                                    value={user.id}
                                    mono
                                />
                            </div>
                        </div>

                        {/* Vai trò & Quyền hạn */}
                        <div className="bento-card p-6 rounded-md">
                            <div className="flex items-center gap-2 mb-5">
                                <div className="p-2 bg-purple-500/10 rounded-md text-purple-600">
                                    <span className="material-symbols-outlined">shield_person</span>
                                </div>
                                <h3 className="text-slate-800 text-lg font-bold">Vai Trò & Quyền Hạn</h3>
                            </div>
                            <div className="space-y-3">
                                {user.roles.map((role) => (
                                    <div
                                        key={role}
                                        className="flex items-center gap-3 p-3 rounded-lg bg-slate-50 border border-slate-100 hover:border-primary/20 hover:bg-primary/5 transition-all duration-200"
                                    >
                                        <div className={`p-2 rounded-lg ${roleColors[role]?.split(' ')[0] ?? 'bg-slate-100'}`}>
                                            <span className={`material-symbols-outlined text-[20px] ${roleColors[role]?.split(' ')[1] ?? 'text-slate-500'}`}>
                                                {roleIcons[role] ?? 'badge'}
                                            </span>
                                        </div>
                                        <div className="flex-1">
                                            <p className="text-sm font-semibold text-slate-800">
                                                {roleLabels[role] ?? role}
                                            </p>
                                            <p className="text-xs text-slate-500">
                                                {getRoleDescription(role)}
                                            </p>
                                        </div>
                                        <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-green-500/10 text-green-600 rounded text-xs font-medium border border-green-500/20">
                                            <span className="w-1.5 h-1.5 rounded-full bg-green-500" />
                                            Đang kích hoạt
                                        </span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </motion.div>

                    {/* Thông tin bảo mật */}
                    <motion.div variants={item}>
                        <div className="bento-card p-6 rounded-md">
                            <div className="flex items-center gap-2 mb-5">
                                <div className="p-2 bg-amber-500/10 rounded-md text-amber-600">
                                    <span className="material-symbols-outlined">security</span>
                                </div>
                                <h3 className="text-slate-800 text-lg font-bold">Bảo Mật & Tài Khoản</h3>
                            </div>
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                <SecurityCard
                                    icon="verified_user"
                                    iconColor="text-green-600"
                                    iconBg="bg-green-500/10"
                                    title="Trạng thái"
                                    value="Đang hoạt động"
                                    valueColor="text-green-600"
                                />
                                <SecurityCard
                                    icon="login"
                                    iconColor="text-blue-600"
                                    iconBg="bg-blue-500/10"
                                    title="Phương thức đăng nhập"
                                    value="Firebase Auth"
                                />
                                <SecurityCard
                                    icon="shield"
                                    iconColor="text-purple-600"
                                    iconBg="bg-purple-500/10"
                                    title="Số vai trò"
                                    value={`${user.roles.length} vai trò`}
                                />
                            </div>
                        </div>
                    </motion.div>
                </motion.div>
            </div>
        </>
    )
}

// ── Helper Components ──────────────────────────────────────────

function InfoRow({
    icon,
    label,
    value,
    mono = false,
}: {
    icon: string
    label: string
    value: string
    mono?: boolean
}) {
    return (
        <div className="flex items-start gap-3 py-2 border-b border-slate-100 last:border-0">
            <span className="material-symbols-outlined text-[18px] text-slate-400 mt-0.5">{icon}</span>
            <div className="flex-1 min-w-0">
                <p className="text-xs text-slate-400 uppercase tracking-wider font-medium">{label}</p>
                <p className={`text-sm text-slate-800 font-medium mt-0.5 truncate ${mono ? 'font-mono text-xs' : ''}`}>
                    {value}
                </p>
            </div>
        </div>
    )
}

function SecurityCard({
    icon,
    iconColor,
    iconBg,
    title,
    value,
    valueColor = 'text-slate-800',
}: {
    icon: string
    iconColor: string
    iconBg: string
    title: string
    value: string
    valueColor?: string
}) {
    return (
        <div className="p-4 rounded-lg bg-slate-50 border border-slate-100 hover:shadow-sm transition-shadow">
            <div className={`p-2 ${iconBg} rounded-md ${iconColor} w-fit mb-3`}>
                <span className="material-symbols-outlined text-[20px]">{icon}</span>
            </div>
            <p className="text-xs text-slate-500 font-medium">{title}</p>
            <p className={`text-sm font-semibold mt-0.5 ${valueColor}`}>{value}</p>
        </div>
    )
}

function getRoleDescription(role: string): string {
    switch (role) {
        case 'admin':
            return 'Quản lý toàn bộ hệ thống, người dùng, kỳ học và cấu hình'
        case 'mentor':
            return 'Hướng dẫn sinh viên, quản lý đề tài và nhóm'
        case 'evaluator':
            return 'Thẩm định và đánh giá các đề tài'
        case 'student':
            return 'Đăng ký đề tài, tham gia nhóm và thực hiện đồ án'
        case 'departmenthead':
            return 'Quản lý bộ môn, phân công thẩm định viên'
        default:
            return 'Vai trò trong hệ thống'
    }
}
