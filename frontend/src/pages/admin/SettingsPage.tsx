import { useState, useEffect, useMemo, useCallback } from 'react'
import { motion } from 'framer-motion'
import { Header } from '@/components/layout'
import { useMaintenance } from '@/contexts/MaintenanceContext'
import { useBranding } from '@/contexts/SettingsContext'
import { useSystemError } from '@/contexts/SystemErrorContext'
import { settingsService, type ArchiveGroup } from '@/lib/settingsService'

const container = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { staggerChildren: 0.08 } },
}
const item = { hidden: { opacity: 0, y: 20 }, show: { opacity: 1, y: 0 } }

// Setting keys (must match backend SettingKeys)
const K = {
    MaxGroupMembers: 'MaxGroupMembers',
    MaxTopicsPerMentor: 'MaxTopicsPerMentor',
    AllowDirectRegistration: 'AllowDirectRegistration',
    RequireOutlineApproval: 'RequireOutlineApproval',
    PrimaryColor: 'PrimaryColor',
    HeaderName: 'HeaderName',
    LogoUrl: 'LogoUrl',
    MaintenanceMode: 'MaintenanceMode',
    EmailOnEvaluationResult: 'EmailOnEvaluationResult',
    NotifyMentorOnRegistration: 'NotifyMentorOnRegistration',
} as const

const colorThemes = [
    { name: 'Blue (Default)', color: '#2c6090' },
    { name: 'Crimson', color: '#A64B4B' },
    { name: 'Forest Green', color: '#5F8F61' },
    { name: 'Indigo', color: '#6366f1' },
    { name: 'Purple', color: '#8b5cf6' },
    { name: 'Teal', color: '#14b8a6' },
    { name: 'Orange', color: '#f97316' },
]

function adjustColor(color: string, amount: number) {
    const hex = color.replace('#', '')
    const r = Math.max(0, Math.min(255, parseInt(hex.slice(0, 2), 16) + amount))
    const g = Math.max(0, Math.min(255, parseInt(hex.slice(2, 4), 16) + amount))
    const b = Math.max(0, Math.min(255, parseInt(hex.slice(4, 6), 16) + amount))
    return `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`
}
function applyPreview(color: string) {
    document.documentElement.style.setProperty('--color-primary', color)
    document.documentElement.style.setProperty('--color-primary-dark', adjustColor(color, -20))
    document.documentElement.style.setProperty('--color-primary-light', adjustColor(color, 20))
}
function formatBytes(bytes: number) {
    if (!bytes) return '0 B'
    const gb = bytes / (1024 * 1024 * 1024)
    if (gb >= 1) return `${gb.toFixed(1)} GB`
    const mb = bytes / (1024 * 1024)
    return `${mb.toFixed(0)} MB`
}

export function SettingsPage() {
    const { setMaintenanceMode } = useMaintenance()
    const { refresh, version } = useBranding()
    const { showError } = useSystemError()

    const [values, setValues] = useState<Record<string, string>>({})
    const [initial, setInitial] = useState<Record<string, string>>({})
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [showSaved, setShowSaved] = useState(false)
    const [archives, setArchives] = useState<ArchiveGroup[]>([])
    const [showColorPicker, setShowColorPicker] = useState(false)
    const [customColor, setCustomColor] = useState('#2c6090')

    // Load settings + archives
    useEffect(() => {
        let active = true
        ;(async () => {
            try {
                const [settings, arch] = await Promise.all([
                    settingsService.getAdminSettings(),
                    settingsService.getArchives().catch(() => [] as ArchiveGroup[]),
                ])
                if (!active) return
                const map = Object.fromEntries(settings.map((s) => [s.key, s.value]))
                setValues(map)
                setInitial(map)
                setArchives(arch)
            } catch {
                if (active) showError('Không tải được cấu hình hệ thống.')
            } finally {
                if (active) setLoading(false)
            }
        })()
        return () => {
            active = false
        }
    }, [showError])

    const get = useCallback((k: string, d = '') => values[k] ?? d, [values])
    const set = useCallback((k: string, v: string) => setValues((p) => ({ ...p, [k]: v })), [])
    const getBool = useCallback((k: string) => (values[k] ?? 'false') === 'true', [values])
    const setBool = useCallback((k: string, b: boolean) => set(k, b ? 'true' : 'false'), [set])

    const hasChanges = useMemo(
        () => Object.keys(values).some((k) => values[k] !== initial[k]),
        [values, initial],
    )

    const handleColorSelect = (color: string) => {
        set(K.PrimaryColor, color)
        applyPreview(color) // live preview; authoritative apply happens on save
    }

    const handleSave = async () => {
        setSaving(true)
        try {
            const changed = Object.fromEntries(
                Object.entries(values).filter(([k, v]) => initial[k] !== v),
            )
            await settingsService.updateSettings(changed)
            setInitial(values)
            setMaintenanceMode(getBool(K.MaintenanceMode))
            await refresh() // re-apply branding system-wide
            setShowSaved(true)
            setTimeout(() => setShowSaved(false), 3000)
        } catch (e) {
            showError(e instanceof Error ? e.message : 'Lưu cấu hình thất bại.')
        } finally {
            setSaving(false)
        }
    }

    const handleLogoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0]
        if (!file) return
        try {
            const { logoUrl } = await settingsService.uploadLogo(file)
            set(K.LogoUrl, logoUrl)
            await refresh()
        } catch (err) {
            showError(err instanceof Error ? err.message : 'Tải logo thất bại.')
        }
    }

    const handleTestEmail = async () => {
        try {
            await settingsService.sendTestEmail()
            setShowSaved(true)
            setTimeout(() => setShowSaved(false), 3000)
        } catch (err) {
            showError(err instanceof Error ? err.message : 'Gửi email kiểm tra thất bại.')
        }
    }

    const totalBytes = archives.reduce((sum, a) => sum + a.totalSizeBytes, 0)
    const primaryColor = get(K.PrimaryColor, '#2c6090')

    return (
        <>
            <Header title="Cấu Hình Hệ Thống" />

            {showSaved && (
                <motion.div
                    initial={{ opacity: 0, y: -50 }}
                    animate={{ opacity: 1, y: 0 }}
                    className="fixed top-20 right-6 z-50 bg-green-500 text-white px-6 py-3 rounded-lg shadow-lg flex items-center gap-2"
                >
                    <span className="material-symbols-outlined">check_circle</span>
                    <span className="font-medium">Thao tác thành công!</span>
                </motion.div>
            )}

            <div className="flex-1 overflow-y-auto p-8 scrollbar-hide bg-slate-50">
                {loading ? (
                    <div className="flex items-center justify-center h-64 text-slate-500">
                        <span className="material-symbols-outlined animate-spin mr-2">progress_activity</span>
                        Đang tải cấu hình...
                    </div>
                ) : (
                    <motion.div variants={container} initial="hidden" animate="show" className="space-y-6">
                        {/* Header actions */}
                        <motion.div variants={item} className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-2">
                            <div>
                                <h3 className="text-lg font-bold text-slate-800">Thiết lập chung</h3>
                                <p className="text-sm text-slate-500">Quản lý các quy tắc, giao diện và thông số vận hành của TEDF</p>
                            </div>
                            <div className="flex gap-3 items-center">
                                {hasChanges && (
                                    <span className="text-sm text-amber-600 font-medium flex items-center gap-1">
                                        <span className="material-symbols-outlined text-[16px]">warning</span>
                                        Có thay đổi chưa lưu
                                    </span>
                                )}
                                <button
                                    onClick={handleSave}
                                    disabled={!hasChanges || saving}
                                    className={`flex items-center gap-2 px-4 py-2 rounded-md text-sm font-medium shadow-sm transition-colors ${hasChanges && !saving ? 'bg-primary text-white hover:bg-primary-dark' : 'bg-slate-300 text-white cursor-not-allowed'}`}
                                >
                                    <span className="material-symbols-outlined text-[18px]">save</span>
                                    {saving ? 'Đang lưu...' : 'Lưu thay đổi'}
                                </button>
                            </div>
                        </motion.div>

                        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                            {/* Left column */}
                            <div className="lg:col-span-2 space-y-6">
                                {/* Registration rules */}
                                <motion.div variants={item} className="bento-card rounded-md overflow-hidden">
                                    <div className="p-4 border-b border-slate-200 bg-slate-50/50 flex items-center gap-2">
                                        <span className="material-symbols-outlined text-primary">rule_settings</span>
                                        <h4 className="font-bold text-slate-800">Quy Tắc Đăng Ký Đề Tài</h4>
                                    </div>
                                    <div className="p-6 space-y-6">
                                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                            <div>
                                                <label className="block text-sm font-medium text-slate-700 mb-1">Số lượng sinh viên tối đa / nhóm</label>
                                                <input className="input-field" type="number" min={1} max={20}
                                                    value={get(K.MaxGroupMembers, '5')}
                                                    onChange={(e) => set(K.MaxGroupMembers, e.target.value)} />
                                                <p className="text-xs text-slate-500 mt-1">Khuyến nghị: 2-5 sinh viên</p>
                                            </div>
                                            <div>
                                                <label className="block text-sm font-medium text-slate-700 mb-1">Số lượng đề tài tối đa / GVHD</label>
                                                <input className="input-field" type="number" min={1} max={100}
                                                    value={get(K.MaxTopicsPerMentor, '5')}
                                                    onChange={(e) => set(K.MaxTopicsPerMentor, e.target.value)} />
                                            </div>
                                        </div>
                                        <div className="border-t border-slate-100 pt-4 space-y-4">
                                            <ToggleRow
                                                title="Cho phép đề xuất đề tài mới"
                                                description="Sinh viên được phép tự đề xuất đề tài thay vì chọn từ danh sách có sẵn."
                                                checked={getBool(K.AllowDirectRegistration)}
                                                onChange={(b) => setBool(K.AllowDirectRegistration, b)} />
                                            <ToggleRow
                                                title="Yêu cầu phê duyệt đề cương"
                                                description="Bắt buộc giảng viên phải duyệt đề cương trước khi bắt đầu thực hiện."
                                                checked={getBool(K.RequireOutlineApproval)}
                                                onChange={(b) => setBool(K.RequireOutlineApproval, b)} />
                                        </div>
                                    </div>
                                </motion.div>

                                {/* Notifications (replaces email config) */}
                                <motion.div variants={item} className="bento-card rounded-md overflow-hidden">
                                    <div className="p-4 border-b border-slate-200 bg-slate-50/50 flex items-center gap-2">
                                        <span className="material-symbols-outlined text-primary">notifications_active</span>
                                        <h4 className="font-bold text-slate-800">Thông Báo & Email</h4>
                                    </div>
                                    <div className="p-6 space-y-6">
                                        <div className="flex items-start gap-3 bg-slate-50 border border-slate-200 rounded-md p-3">
                                            <span className="material-symbols-outlined text-slate-500 mt-0.5">info</span>
                                            <p className="text-xs text-slate-600">
                                                Thông tin máy chủ email (SMTP) được cấu hình an toàn phía máy chủ và không hiển thị tại đây.
                                                Dùng nút bên dưới để kiểm tra kết nối gửi email.
                                            </p>
                                        </div>
                                        <div className="space-y-4">
                                            <ToggleRow
                                                title="Email khi có kết quả thẩm định"
                                                description="Gửi thông báo cho các bên khi đề tài có kết quả thẩm định cuối cùng."
                                                checked={getBool(K.EmailOnEvaluationResult)}
                                                onChange={(b) => setBool(K.EmailOnEvaluationResult, b)} />
                                            <ToggleRow
                                                title="Thông báo GVHD khi có đăng ký"
                                                description="Báo cho giảng viên khi một nhóm đăng ký đề tài của họ."
                                                checked={getBool(K.NotifyMentorOnRegistration)}
                                                onChange={(b) => setBool(K.NotifyMentorOnRegistration, b)} />
                                        </div>
                                        <div className="flex justify-end">
                                            <button onClick={handleTestEmail}
                                                className="text-sm font-medium text-primary hover:text-primary-light border border-primary/20 bg-primary/5 hover:bg-primary/10 px-3 py-1.5 rounded transition-colors">
                                                Gửi email kiểm tra
                                            </button>
                                        </div>
                                    </div>
                                </motion.div>

                                {/* Archive management */}
                                <motion.div variants={item} className="bento-card rounded-md overflow-hidden">
                                    <div className="p-4 border-b border-slate-200 bg-slate-50/50 flex items-center justify-between">
                                        <div className="flex items-center gap-2">
                                            <span className="material-symbols-outlined text-primary">inventory_2</span>
                                            <h4 className="font-bold text-slate-800">Quản Lý Kho Lưu Trữ Đề Tài Cũ</h4>
                                        </div>
                                        <span className="text-xs font-medium px-2 py-1 bg-green-100 text-green-700 rounded-full border border-green-200">Hoạt động bình thường</span>
                                    </div>
                                    <div className="overflow-x-auto">
                                        <table className="w-full text-left text-sm text-slate-500">
                                            <thead className="bg-slate-50 text-xs uppercase font-semibold text-slate-600 border-b border-slate-200">
                                                <tr>
                                                    <th className="px-6 py-3">Năm học</th>
                                                    <th className="px-6 py-3">Số lượng đề tài</th>
                                                    <th className="px-6 py-3">Dung lượng</th>
                                                    <th className="px-6 py-3">Trạng thái</th>
                                                </tr>
                                            </thead>
                                            <tbody className="divide-y divide-slate-100">
                                                {archives.length === 0 ? (
                                                    <tr><td colSpan={4} className="px-6 py-6 text-center text-slate-400">Chưa có dữ liệu lưu trữ</td></tr>
                                                ) : (
                                                    archives.map((a) => (
                                                        <tr key={a.academicYear} className="hover:bg-slate-50 transition-colors">
                                                            <td className="px-6 py-3 font-medium text-slate-800">{a.academicYear}</td>
                                                            <td className="px-6 py-3">{a.projectCount}</td>
                                                            <td className="px-6 py-3">{formatBytes(a.totalSizeBytes)}</td>
                                                            <td className="px-6 py-3"><span className="text-xs bg-slate-100 text-slate-600 px-2 py-1 rounded">Đã lưu trữ</span></td>
                                                        </tr>
                                                    ))
                                                )}
                                            </tbody>
                                        </table>
                                        <div className="p-4 bg-slate-50/50 border-t border-slate-200 flex items-center justify-between">
                                            <div className="text-xs text-slate-500">Tổng dung lượng: <span className="font-bold text-slate-700">{formatBytes(totalBytes)}</span></div>
                                        </div>
                                    </div>
                                </motion.div>
                            </div>

                            {/* Right column */}
                            <div className="lg:col-span-1 space-y-6">
                                {/* Appearance & logo */}
                                <motion.div variants={item} className="bento-card rounded-md overflow-hidden">
                                    <div className="p-4 border-b border-slate-200 bg-slate-50/50 flex items-center gap-2">
                                        <span className="material-symbols-outlined text-primary">palette</span>
                                        <h4 className="font-bold text-slate-800">Giao Diện & Logo</h4>
                                    </div>
                                    <div className="p-6 space-y-6">
                                        <div>
                                            <label className="block text-sm font-medium text-slate-700 mb-2">Logo hệ thống</label>
                                            <div className="flex items-center gap-4">
                                                <div className="w-20 h-20 bg-slate-100 rounded-lg border border-slate-200 flex items-center justify-center text-primary font-bold text-2xl overflow-hidden">
                                                    {get(K.LogoUrl) ? (
                                                        <img src={get(K.LogoUrl)} alt="logo" className="w-full h-full object-contain" />
                                                    ) : (
                                                        <span>{(get(K.HeaderName, 'T')[0] || 'T').toUpperCase()}</span>
                                                    )}
                                                </div>
                                                <div className="flex-1">
                                                    <label className="text-sm bg-white border border-slate-300 text-slate-700 hover:bg-slate-50 px-3 py-1.5 rounded-md font-medium w-full mb-2 block text-center cursor-pointer">
                                                        Tải lên mới
                                                        <input type="file" accept="image/png,image/jpeg,image/svg+xml" className="hidden" onChange={handleLogoUpload} />
                                                    </label>
                                                    <p className="text-[10px] text-slate-400">PNG, JPG, SVG tối đa 2MB.</p>
                                                </div>
                                            </div>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-slate-700 mb-2">Màu chủ đạo</label>
                                            <p className="text-xs text-slate-500 mb-3">Áp dụng cho toàn bộ người dùng sau khi lưu</p>
                                            <div className="flex flex-wrap gap-3">
                                                {colorThemes.map((theme) => (
                                                    <button key={theme.color} onClick={() => handleColorSelect(theme.color)} title={theme.name}
                                                        className={`w-10 h-10 rounded-full transition-all hover:scale-110 flex items-center justify-center ${primaryColor === theme.color ? 'ring-2 ring-offset-2 scale-110' : ''}`}
                                                        style={{ backgroundColor: theme.color, '--tw-ring-color': theme.color } as React.CSSProperties}>
                                                        {primaryColor === theme.color && <span className="material-symbols-outlined text-white text-[18px]">check</span>}
                                                    </button>
                                                ))}
                                                <button onClick={() => setShowColorPicker(!showColorPicker)}
                                                    className="w-10 h-10 rounded-full bg-slate-100 border-2 border-dashed border-slate-300 flex items-center justify-center hover:bg-slate-200">
                                                    <span className="material-symbols-outlined text-[18px] text-slate-500">add</span>
                                                </button>
                                            </div>
                                            {showColorPicker && (
                                                <div className="mt-4 p-4 bg-slate-50 rounded-lg border border-slate-200 flex items-center gap-3">
                                                    <input type="color" value={customColor} onChange={(e) => setCustomColor(e.target.value)} className="w-12 h-10 rounded cursor-pointer border-0 p-0" />
                                                    <input type="text" value={customColor} onChange={(e) => setCustomColor(e.target.value)} className="flex-1 input-field uppercase" placeholder="#000000" />
                                                    <button onClick={() => { handleColorSelect(customColor); setShowColorPicker(false) }}
                                                        className="px-4 py-2 bg-primary text-white text-sm font-medium rounded-md hover:bg-primary-dark">Chọn</button>
                                                </div>
                                            )}
                                            <div className="mt-3 flex items-center gap-2 text-xs text-slate-500">
                                                <div className="w-4 h-4 rounded-full border border-slate-200" style={{ backgroundColor: primaryColor }} />
                                                <span>Màu đang chọn: <span className="font-mono font-medium text-slate-700">{primaryColor}</span></span>
                                            </div>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-slate-700 mb-1">Tên hiển thị (Header)</label>
                                            <input className="input-field" type="text" maxLength={50}
                                                value={get(K.HeaderName, 'TEDF')}
                                                onChange={(e) => set(K.HeaderName, e.target.value)} />
                                        </div>
                                    </div>
                                </motion.div>

                                {/* Maintenance */}
                                <motion.div variants={item} className="bento-card rounded-md overflow-hidden border-orange-200">
                                    <div className="p-4 bg-orange-50 border-b border-orange-100 flex items-center gap-2">
                                        <span className="material-symbols-outlined text-orange-600">engineering</span>
                                        <h4 className="font-bold text-orange-800">Bảo Trì Hệ Thống</h4>
                                    </div>
                                    <div className="p-6">
                                        <p className="text-sm text-slate-600 mb-4">Khi bật chế độ bảo trì, chỉ Admin mới có thể truy cập hệ thống. Các yêu cầu của sinh viên/giảng viên sẽ bị chặn (503).</p>
                                        <div className="flex items-center justify-between">
                                            <div>
                                                <span className="text-sm font-bold text-slate-700">Chế độ bảo trì</span>
                                                {getBool(K.MaintenanceMode) && (
                                                    <span className="ml-2 text-xs font-medium px-2 py-0.5 bg-orange-100 text-orange-700 rounded-full">Đang bật</span>
                                                )}
                                            </div>
                                            <label className="relative inline-flex items-center cursor-pointer">
                                                <input className="sr-only peer" type="checkbox"
                                                    checked={getBool(K.MaintenanceMode)}
                                                    onChange={(e) => setBool(K.MaintenanceMode, e.target.checked)} />
                                                <div className="w-11 h-6 bg-slate-200 rounded-full peer peer-checked:after:translate-x-full after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-orange-500" />
                                            </label>
                                        </div>
                                        <p className="text-xs text-amber-600 mt-3">Thay đổi sẽ có hiệu lực sau khi bấm "Lưu thay đổi".</p>
                                    </div>
                                </motion.div>

                                {/* Version */}
                                <motion.div variants={item} className="bg-blue-50 p-4 rounded-md border border-blue-100">
                                    <div className="flex items-start gap-3">
                                        <span className="material-symbols-outlined text-blue-600 mt-0.5">info</span>
                                        <div>
                                            <h5 className="text-sm font-bold text-blue-800 mb-1">Phiên bản hệ thống</h5>
                                            <p className="text-xs text-blue-700">{version || 'Không xác định'}</p>
                                        </div>
                                    </div>
                                </motion.div>
                            </div>
                        </div>
                    </motion.div>
                )}
            </div>
        </>
    )
}

function ToggleRow({ title, description, checked, onChange }: { title: string; description: string; checked: boolean; onChange: (b: boolean) => void }) {
    return (
        <div className="flex items-center justify-between">
            <div>
                <p className="text-sm font-medium text-slate-800">{title}</p>
                <p className="text-xs text-slate-500">{description}</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
                <input className="sr-only peer" type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} />
                <div className="w-11 h-6 bg-slate-200 rounded-full peer peer-checked:after:translate-x-full after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary" />
            </label>
        </div>
    )
}
