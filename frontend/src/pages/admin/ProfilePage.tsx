import { useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Header } from '@/components/layout'
import { useAuth } from '@/contexts/AuthContext'
import { profileService } from '@/services/users/profileService'
import { MyProfile } from '@/types/users/user.types'

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
    const { user: authUser } = useAuth()
    const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false)
    const [isEditModalOpen, setIsEditModalOpen] = useState(false)
    const [profile, setProfile] = useState<MyProfile | null>(null)
    const [isLoading, setIsLoading] = useState(true)

    // Fetch full profile from API
    useEffect(() => {
        const fetchProfile = async () => {
            if (!authUser) return;
            try {
                const response = await profileService.getMyProfile()
                setProfile(response)
            } catch (error) {
                console.error("Failed to fetch profile", error)
            } finally {
                setIsLoading(false)
            }
        }
        fetchProfile()
    }, [authUser])

    const togglePrivacy = async (fieldName: string) => {
        let hiddenFields: string[] = []
        if (profile?.privacySettings) {
            try {
                hiddenFields = JSON.parse(profile.privacySettings)
            } catch {
                // Ignore
            }
        }

        const newHiddenFields = hiddenFields.includes(fieldName)
            ? hiddenFields.filter(f => f !== fieldName)
            : [...hiddenFields, fieldName]
        
        try {
            await profileService.updateMyProfile({
                phoneNumber: profile?.phoneNumber,
                birthDate: profile?.birthDate,
                privacySettings: JSON.stringify(newHiddenFields)
            })
            // Update local state
            setProfile(prev => prev ? { ...prev, privacySettings: JSON.stringify(newHiddenFields) } : prev)
        } catch (err) {
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const e = err as any;
            console.error('Update failed:', e.response?.data || e.message || e);
            alert(`Có lỗi xảy ra khi cập nhật: ${e.response?.data?.title || e.response?.data?.message || e.message}`);
        }
    }

    if (!authUser) return null

    const handleSaveProfile = async (updatedData: { phoneNumber?: string, birthDate?: string }) => {
        try {
            await profileService.updateMyProfile({
                ...updatedData,
                privacySettings: profile?.privacySettings
            })
            setProfile(prev => prev ? { ...prev, ...updatedData } : prev)
        } catch (err) {
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const e = err as any;
            console.error('Update profile failed:', e)
            alert('Lỗi cập nhật thông tin!')
            throw e
        }
    }

    if (isLoading) {
        return (
            <>
                <Header title="Thông Tin Cá Nhân" />
                <div className="flex-1 p-8 flex items-center justify-center">
                    <span className="material-symbols-outlined animate-spin text-4xl text-primary">progress_activity</span>
                </div>
            </>
        )
    }

    // Determine what to render based on roles
    const roles = (profile?.roles || authUser.roles || []).map((r: string) => r.toLowerCase())
    const isStudent = roles.includes('student')
    const isLecturer = roles.includes('mentor') || roles.includes('evaluator') || roles.includes('departmenthead')

    const initials = (profile?.fullName || authUser.name)
        .split(' ')
        .map((w: string) => w[0])
        .join('')
        .slice(-2)
        .toUpperCase()

    // Parse privacy settings (assume array of field names that are PRIVATE)
    let hiddenFields: string[] = []
    if (profile?.privacySettings) {
        try {
            hiddenFields = JSON.parse(profile.privacySettings)
        } catch {
            console.error('Failed to parse privacy settings')
        }
    }

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
                        className="bento-card rounded-xl overflow-hidden shadow-sm border border-slate-200/60 bg-white"
                    >
                        {/* Banner */}
                        <div className="h-48 sm:h-56 bg-gradient-to-r from-slate-900 via-slate-800 to-slate-900 relative px-8 py-8 flex flex-col justify-end">
                            <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNjAiIGhlaWdodD0iNjAiIHZpZXdCb3g9IjAgMCA2MCA2MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48ZyBmaWxsPSJub25lIiBmaWxsLXJ1bGU9ImV2ZW5vZGQiPjxnIGZpbGw9IiNmZmYiIGZpbGwtb3BhY2l0eT0iMC4wNSI+PHBhdGggZD0iTTM2IDM0djItSDJ2LTJoMnptMC00VjhoMnYyMmgtMlYzMHptLTE2IDZWMjJoMnYxNGgtMnptMTAtMTBWMTRoMnYxMmgtMnoiLz48L2c+PC9nPjwvc3ZnPg==')] opacity-20" />
                            
                            <div className="relative z-10 flex flex-col sm:flex-row sm:items-end gap-6 w-full">
                                {/* Avatar */}
                                <div className="relative shrink-0 sm:translate-y-12 translate-y-8">
                                    {profile?.avatarUrl || authUser.avatar ? (
                                        <img
                                            src={profile?.avatarUrl || authUser.avatar}
                                            alt={profile?.fullName || authUser.name}
                                            className="w-28 h-28 sm:w-32 sm:h-32 rounded-2xl border-4 border-white shadow-xl object-cover bg-white"
                                        />
                                    ) : (
                                        <div className="w-28 h-28 sm:w-32 sm:h-32 rounded-2xl border-4 border-white shadow-xl bg-gradient-to-br from-violet-600 to-fuchsia-600 flex items-center justify-center">
                                            <span className="text-4xl font-bold text-white drop-shadow-md">{initials}</span>
                                        </div>
                                    )}
                                    <div className="absolute -bottom-1 -right-1 w-6 h-6 bg-emerald-500 rounded-full border-2 border-white shadow-sm" title="Đang hoạt động" />
                                </div>

                                {/* Name & Role */}
                                <div className="flex-1 sm:pb-2 pt-10 sm:pt-0">
                                    <h2 className="text-2xl sm:text-3xl font-bold text-white tracking-tight drop-shadow-md">{profile?.fullName || authUser.name}</h2>
                                    <p className="text-slate-300 font-medium mt-1 drop-shadow-sm">{profile?.email || authUser.email}</p>
                                    <div className="flex flex-wrap gap-2 mt-3">
                                        {roles.map((role: string) => (
                                            <span
                                                key={role}
                                                className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border shadow-sm ${roleColors[role] ?? 'bg-white/10 text-white border-white/20 backdrop-blur-sm'}`}
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
                        
                        {/* Spacing for the avatar that overlaps */}
                        <div className="h-12 sm:h-16 bg-white"></div>
                    </motion.div>

                    <motion.div variants={item} className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                        {/* Thông tin cơ bản */}
                        <div className="bento-card p-6 rounded-md">
                            <div className="flex items-center gap-2 mb-5">
                                <div className="p-2 bg-blue-500/10 rounded-md text-blue-600">
                                    <span className="material-symbols-outlined">person</span>
                                </div>
                                <h3 className="text-slate-800 text-lg font-bold">Thông Tin Cơ Bản</h3>
                                <button
                                    onClick={() => setIsEditModalOpen(true)}
                                    className="ml-auto flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-primary/10 text-primary hover:bg-primary hover:text-white transition-colors text-sm font-semibold"
                                >
                                    <span className="material-symbols-outlined text-[16px]">edit</span>
                                    Chỉnh sửa
                                </button>
                            </div>
                            <div className="space-y-4">
                                <InfoRow
                                    icon="badge"
                                    label="Họ và tên"
                                    value={profile?.fullName || authUser.name}
                                />
                                <InfoRow
                                    icon="mail"
                                    label="Email"
                                    value={profile?.email || authUser.email}
                                />
                                {isStudent && (
                                    <>
                                        <InfoRow
                                            icon="school"
                                            label="Chuyên ngành đang học"
                                            value={profile?.departmentName || "Chưa cập nhật"}
                                            placeholder={!profile?.departmentName}
                                            isPrivate={hiddenFields.includes('major')}
                                            onTogglePrivacy={() => togglePrivacy('major')}
                                        />
                                        <InfoRow
                                            icon="menu_book"
                                            label="Chuyên ngành hẹp đang học"
                                            value={"Chưa cập nhật"}
                                            placeholder={true}
                                            isPrivate={hiddenFields.includes('narrowMajor')}
                                            onTogglePrivacy={() => togglePrivacy('narrowMajor')}
                                        />
                                    </>
                                )}
                                {isLecturer && (
                                    <>
                                        <InfoRow
                                            icon="business"
                                            label="Bộ môn đang giảng dạy"
                                            value={profile?.departmentName || "Chưa cập nhật"}
                                            placeholder={!profile?.departmentName}
                                            isPrivate={hiddenFields.includes('department')}
                                            onTogglePrivacy={() => togglePrivacy('department')}
                                        />
                                    </>
                                )}
                                {(isStudent || isLecturer) && (
                                    <>
                                        <InfoRow
                                            icon="cake"
                                            label="Ngày sinh"
                                            value={profile?.birthDate || "Chưa cập nhật"}
                                            placeholder={!profile?.birthDate}
                                            isPrivate={hiddenFields.includes('birthDate')}
                                            onTogglePrivacy={() => togglePrivacy('birthDate')}
                                        />
                                        <InfoRow
                                            icon="call"
                                            label="SĐT liên hệ"
                                            value={profile?.phoneNumber || "Chưa cập nhật"}
                                            placeholder={!profile?.phoneNumber}
                                            isPrivate={hiddenFields.includes('phoneNumber')}
                                            onTogglePrivacy={() => togglePrivacy('phoneNumber')}
                                        />
                                    </>
                                )}
                            </div>
                        </div>

                        <div className="space-y-6">
                            {/* Bảo Mật & Tài Khoản */}
                            <div className="bento-card p-6 rounded-md">
                                <div className="flex items-center gap-2 mb-5">
                                    <div className="p-2 bg-amber-500/10 rounded-md text-amber-600">
                                        <span className="material-symbols-outlined">security</span>
                                    </div>
                                    <h3 className="text-slate-800 text-lg font-bold">Bảo Mật & Tài Khoản</h3>
                                </div>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <SecurityCard
                                        icon="verified_user"
                                        iconColor="text-green-600"
                                        iconBg="bg-green-500/10"
                                        title="Trạng thái"
                                        value={profile?.status === 'Active' ? 'Đang hoạt động' : 'Bị khóa'}
                                        valueColor={profile?.status === 'Active' ? 'text-green-600' : 'text-red-600'}
                                    />
                                    
                                    <button 
                                        onClick={() => setIsPasswordModalOpen(true)}
                                        className="p-4 rounded-lg bg-slate-50 border border-slate-100 hover:border-primary/30 hover:bg-primary/5 hover:shadow-sm transition-all text-left group"
                                    >
                                        <div className="p-2 bg-slate-200/50 rounded-md text-slate-600 w-fit mb-3 group-hover:bg-primary/10 group-hover:text-primary transition-colors">
                                            <span className="material-symbols-outlined text-[20px]">key</span>
                                        </div>
                                        <p className="text-xs text-slate-500 font-medium">Bảo mật</p>
                                        <p className="text-sm font-semibold mt-0.5 text-slate-800 group-hover:text-primary transition-colors">Đổi mật khẩu</p>
                                    </button>
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
                                    {roles.map((role: string) => (
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
                                                {' '}Đang kích hoạt
                                            </span>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        </div>
                    </motion.div>

                    {/* Lịch sử hướng dẫn (Chỉ Lecturer) */}
                    {isLecturer && (
                        <motion.div variants={item}>
                            <div className="bento-card p-6 rounded-md">
                                <div className="flex items-center gap-2 mb-5">
                                    <div className="p-2 bg-indigo-500/10 rounded-md text-indigo-600">
                                        <span className="material-symbols-outlined">history_edu</span>
                                    </div>
                                    <h3 className="text-slate-800 text-lg font-bold">Lịch Sử Hướng Dẫn Đồ Án</h3>
                                    <div className="ml-auto flex items-center gap-1 text-xs text-slate-500 bg-slate-100 px-2 py-1 rounded">
                                        <span className="material-symbols-outlined text-[14px]">public</span>
                                        Công khai
                                    </div>
                                </div>
                                <div className="text-center py-8 bg-slate-50 rounded-lg border border-slate-100 border-dashed">
                                    <span className="material-symbols-outlined text-4xl text-slate-300 mb-2">folder_open</span>
                                    <p className="text-sm text-slate-500">Chưa có dữ liệu đồ án hướng dẫn.</p>
                                </div>
                            </div>
                        </motion.div>
                    )}
                </motion.div>
            </div>

            {/* Modal Đổi mật khẩu */}
            {isPasswordModalOpen && (
                <PasswordChangeModal onClose={() => setIsPasswordModalOpen(false)} />
            )}

            {/* Modal Chỉnh sửa thông tin */}
            {isEditModalOpen && (
                <ProfileEditModal 
                    profile={profile} 
                    onClose={() => setIsEditModalOpen(false)} 
                    onSave={handleSaveProfile} 
                />
            )}
        </>
    )
}

// ── Helper Components ──────────────────────────────────────────

function InfoRow({
    icon,
    label,
    value,
    mono = false,
    placeholder = false,
    isPrivate = false,
    onTogglePrivacy
}: {
    icon: string
    label: string
    value: string
    mono?: boolean
    placeholder?: boolean
    isPrivate?: boolean
    onTogglePrivacy?: () => void
}) {
    return (
        <div className="flex items-center gap-3 py-2 border-b border-slate-100 last:border-0 group">
            <span className="material-symbols-outlined text-[18px] text-slate-400 self-start mt-0.5">{icon}</span>
            <div className="flex-1 min-w-0">
                <p className="text-xs text-slate-400 uppercase tracking-wider font-medium">{label}</p>
                <p className={`text-sm mt-0.5 truncate ${mono ? 'font-mono text-xs' : ''} ${placeholder ? 'text-slate-400 italic' : 'text-slate-800 font-medium'}`}>
                    {value}
                </p>
            </div>
            {onTogglePrivacy && (
                <button
                    onClick={onTogglePrivacy}
                    className="p-1.5 rounded-md hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-colors opacity-0 group-hover:opacity-100 focus:opacity-100"
                    title={isPrivate ? "Đang ẩn với người khác. Nhấn để công khai" : "Đang công khai. Nhấn để ẩn"}
                >
                    <span className="material-symbols-outlined text-[20px]">
                        {isPrivate ? 'lock' : 'public'}
                    </span>
                </button>
            )}
            {/* Show icon even when not hovering if it's private to alert user */}
            {onTogglePrivacy && isPrivate && (
                <span className="material-symbols-outlined text-[16px] text-amber-500 group-hover:hidden">lock</span>
            )}
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

function PasswordChangeModal({ onClose }: { onClose: () => void }) {
    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault()
        alert('Tính năng đổi mật khẩu Firebase đang được cập nhật.')
        onClose()
    }

    return (
        <ModalWrapper title="Đổi Mật Khẩu" onClose={onClose}>
                
                <form onSubmit={handleSubmit} className="p-6 space-y-4">
                    <div>
                        <label htmlFor="currentPassword" className="block text-sm font-medium text-slate-700 mb-1">Mật khẩu hiện tại</label>
                        <input id="currentPassword" type="password" required className="w-full px-3 py-2 border border-slate-200 rounded-md focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary" />
                    </div>
                    <div>
                        <label htmlFor="newPassword" className="block text-sm font-medium text-slate-700 mb-1">Mật khẩu mới</label>
                        <input id="newPassword" type="password" required minLength={6} className="w-full px-3 py-2 border border-slate-200 rounded-md focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary" />
                    </div>
                    <div>
                        <label htmlFor="confirmPassword" className="block text-sm font-medium text-slate-700 mb-1">Xác nhận mật khẩu mới</label>
                        <input id="confirmPassword" type="password" required minLength={6} className="w-full px-3 py-2 border border-slate-200 rounded-md focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary" />
                    </div>
                    
                    <div className="pt-4 flex gap-3">
                        <button type="button" onClick={onClose} className="flex-1 px-4 py-2 text-sm font-medium text-slate-600 bg-slate-100 rounded-md hover:bg-slate-200">
                            Hủy
                        </button>
                        <button type="submit" className="flex-1 px-4 py-2 text-sm font-medium text-white bg-primary rounded-md hover:bg-primary/90">
                            Xác nhận
                        </button>
                    </div>
                </form>
        </ModalWrapper>
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

function ProfileEditModal({ 
    profile, 
    onClose, 
    onSave 
}: { 
    profile: MyProfile | null, 
    onClose: () => void,
    onSave: (data: { phoneNumber?: string, birthDate?: string }) => Promise<void>
}) {
    const [phoneNumber, setPhoneNumber] = useState(profile?.phoneNumber || '')
    const [birthDate, setBirthDate] = useState(profile?.birthDate || '')
    const [isSaving, setIsSaving] = useState(false)

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setIsSaving(true)
        try {
            await onSave({
                phoneNumber: phoneNumber.trim() || undefined,
                birthDate: birthDate || undefined
            })
            onClose()
        } catch {
            // Error is handled in onSave
        } finally {
            setIsSaving(false)
        }
    }

    return (
        <ModalWrapper title="Chỉnh sửa thông tin" onClose={onClose}>
                
                <form onSubmit={handleSubmit} className="p-5 space-y-4">
                    <div>
                        <label htmlFor="editPhone" className="block text-sm font-semibold text-slate-700 mb-1.5">Số điện thoại</label>
                        <input
                            id="editPhone"
                            type="tel"
                            value={phoneNumber}
                            onChange={(e) => setPhoneNumber(e.target.value)}
                            placeholder="Nhập số điện thoại"
                            className="w-full px-3 py-2 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                        />
                    </div>
                    
                    <div>
                        <label htmlFor="editBirthDate" className="block text-sm font-semibold text-slate-700 mb-1.5">Ngày sinh</label>
                        <input
                            id="editBirthDate"
                            type="date"
                            value={birthDate}
                            onChange={(e) => setBirthDate(e.target.value)}
                            className="w-full px-3 py-2 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                        />
                    </div>

                    <div className="pt-4 flex gap-3">
                        <button
                            type="button"
                            onClick={onClose}
                            disabled={isSaving}
                            className="flex-1 px-4 py-2 bg-slate-100 text-slate-600 font-semibold rounded-lg hover:bg-slate-200 transition-colors disabled:opacity-50"
                        >
                            Hủy bỏ
                        </button>
                        <button
                            type="submit"
                            disabled={isSaving}
                            className="flex-1 px-4 py-2 bg-primary text-white font-semibold rounded-lg hover:bg-primary/90 transition-colors flex items-center justify-center gap-2 disabled:opacity-50"
                        >
                            {isSaving ? (
                                <>
                                    <span className="material-symbols-outlined animate-spin text-[18px]">progress_activity</span>
                                    Đang lưu...
                                </>
                            ) : (
                                'Lưu thay đổi'
                            )}
                        </button>
                    </div>
                </form>
        </ModalWrapper>
    )
}
