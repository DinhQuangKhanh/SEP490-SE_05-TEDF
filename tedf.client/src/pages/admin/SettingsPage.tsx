import { useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Header } from '@/components/layout'
import { useMaintenance } from '@/contexts/MaintenanceContext'

const container = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { staggerChildren: 0.1 } }
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 }
}

// Predefined color themes
const colorThemes = [
    { name: 'Blue (Default)', color: '#2c6090', hoverColor: '#1e4a6e' },
    { name: 'Crimson', color: '#A64B4B', hoverColor: '#8a3d3d' },
    { name: 'Forest Green', color: '#5F8F61', hoverColor: '#4d7450' },
    { name: 'Indigo', color: '#6366f1', hoverColor: '#4f46e5' },
    { name: 'Purple', color: '#8b5cf6', hoverColor: '#7c3aed' },
    { name: 'Teal', color: '#14b8a6', hoverColor: '#0d9488' },
    { name: 'Orange', color: '#f97316', hoverColor: '#ea580c' },
]

export function SettingsPage() {
    const { isMaintenanceMode, setMaintenanceMode } = useMaintenance()
    // Saved color (currently applied to system)
    const [savedColor, setSavedColor] = useState(() => {
        return localStorage.getItem('themeColor') || '#2c6090'
    })
    // Pending color (selected but not saved yet)
    const [pendingColor, setPendingColor] = useState(() => {
        return localStorage.getItem('themeColor') || '#2c6090'
    })
    const [showColorPicker, setShowColorPicker] = useState(false)
    const [customColor, setCustomColor] = useState('#2c6090')
    const [showSaveSuccess, setShowSaveSuccess] = useState(false)
    const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false)

    // Apply theme color to CSS variables (only when saved)
    useEffect(() => {
        document.documentElement.style.setProperty('--color-primary', savedColor)
        const hoverColor = adjustColor(savedColor, -20)
        document.documentElement.style.setProperty('--color-primary-dark', hoverColor)
        document.documentElement.style.setProperty('--color-primary-light', adjustColor(savedColor, 20))
    }, [savedColor])

    // Track unsaved changes
    useEffect(() => {
        setHasUnsavedChanges(pendingColor !== savedColor)
    }, [pendingColor, savedColor])

    // Helper function to adjust color brightness
    const adjustColor = (color: string, amount: number) => {
        const hex = color.replace('#', '')
        const r = Math.max(0, Math.min(255, parseInt(hex.slice(0, 2), 16) + amount))
        const g = Math.max(0, Math.min(255, parseInt(hex.slice(2, 4), 16) + amount))
        const b = Math.max(0, Math.min(255, parseInt(hex.slice(4, 6), 16) + amount))
        return `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`
    }

    const handleColorSelect = (color: string) => {
        setPendingColor(color)
    }

    const handleCustomColorApply = () => {
        setPendingColor(customColor)
        setShowColorPicker(false)
    }

    const handleSaveChanges = () => {
        setSavedColor(pendingColor)
        localStorage.setItem('themeColor', pendingColor)
        setShowSaveSuccess(true)
        setTimeout(() => setShowSaveSuccess(false), 3000)
    }

    const handleResetDefault = () => {
        const defaultColor = '#2c6090'
        setPendingColor(defaultColor)
        setSavedColor(defaultColor)
        localStorage.setItem('themeColor', defaultColor)
    }

    return (
        <>
            <Header title="Cấu Hình Hệ Thống" />

            {/* Success Toast */}
            {showSaveSuccess && (
                <motion.div
                    initial={{ opacity: 0, y: -50 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -50 }}
                    className="fixed top-20 right-6 z-50 bg-green-500 text-white px-6 py-3 rounded-lg shadow-lg flex items-center gap-2"
                >
                    <span className="material-symbols-outlined">check_circle</span>
                    <span className="font-medium">Đã lưu thay đổi thành công!</span>
                </motion.div>
            )}

            <div className="flex-1 overflow-y-auto p-8 scrollbar-hide bg-slate-50">
                <motion.div
                    variants={container}
                    initial="hidden"
                    animate="show"
                    className="space-y-6"
                >
                    {/* Header Actions */}
                    <motion.div variants={item} className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-2">
                        <div>
                            <h3 className="text-lg font-bold text-slate-800">Thiết lập chung</h3>
                            <p className="text-sm text-slate-500">Quản lý các quy tắc, giao diện và thông số vận hành của TEDF</p>
                        </div>
                        <div className="flex gap-3 items-center">
                            {hasUnsavedChanges && (
                                <span className="text-sm text-amber-600 font-medium flex items-center gap-1">
                                    <span className="material-symbols-outlined text-[16px]">warning</span>
                                    Có thay đổi chưa lưu
                                </span>
                            )}
                            <button
                                onClick={handleResetDefault}
                                className="px-4 py-2 border border-slate-300 rounded-md text-sm font-medium text-slate-700 bg-white hover:bg-slate-50 transition-colors"
                            >
                                Khôi phục mặc định
                            </button>
                            <button
                                onClick={handleSaveChanges}
                                className={`flex items-center gap-2 px-4 py-2 rounded-md text-sm font-medium shadow-sm transition-colors ${hasUnsavedChanges
                                    ? 'bg-primary text-white hover:bg-primary-dark animate-pulse'
                                    : 'bg-primary text-white hover:bg-primary-light'
                                    }`}
                            >
                                <span className="material-symbols-outlined text-[18px]">save</span>
                                Lưu thay đổi
                            </button>
                        </div>
                    </motion.div>

                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                        {/* Left Column */}
                        <div className="lg:col-span-2 space-y-6">
                            {/* Topic Registration Rules */}
                            <motion.div variants={item} className="bento-card rounded-md overflow-hidden">
                                <div className="p-4 border-b border-slate-200 bg-slate-50/50 flex items-center gap-2">
                                    <span className="material-symbols-outlined text-primary">rule_settings</span>
                                    <h4 className="font-bold text-slate-800">Quy Tắc Đăng Ký Đề Tài</h4>
                                </div>
                                <div className="p-6 space-y-6">
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                        <div>
                                            <label className="block text-sm font-medium text-slate-700 mb-1">Số lượng sinh viên tối đa / nhóm</label>
                                            <input className="w-full bg-white border border-slate-300 text-sm text-slate-800 rounded-md py-2 px-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all shadow-sm" type="number" defaultValue={3} />
                                            <p className="text-xs text-slate-500 mt-1">Khuyến nghị: 2-5 sinh viên</p>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-slate-700 mb-1">Số lượng đề tài tối đa / GVHD</label>
                                            <input className="w-full bg-white border border-slate-300 text-sm text-slate-800 rounded-md py-2 px-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all shadow-sm" type="number" defaultValue={5} />
                                        </div>
                                    </div>
                                    <div className="border-t border-slate-100 pt-4 space-y-4">
                                        <ToggleRow
                                            title="Cho phép đề xuất đề tài mới"
                                            description="Sinh viên được phép tự đề xuất đề tài thay vì chọn từ danh sách có sẵn."
                                            defaultChecked
                                        />
                                        <ToggleRow
                                            title="Yêu cầu phê duyệt đề cương"
                                            description="Bắt buộc giảng viên phải duyệt đề cương trước khi bắt đầu thực hiện."
                                            defaultChecked
                                        />
                                    </div>
                                </div>
                            </motion.div>

                            {/* Email Config */}
                            <motion.div variants={item} className="bento-card rounded-md overflow-hidden">
                                <div className="p-4 border-b border-slate-200 bg-slate-50/50 flex items-center gap-2">
                                    <span className="material-symbols-outlined text-primary">mail</span>
                                    <h4 className="font-bold text-slate-800">Cấu Hình Email Tự Động</h4>
                                </div>
                                <div className="p-6 space-y-6">
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                        <div className="col-span-1 md:col-span-2">
                                            <label className="block text-sm font-medium text-slate-700 mb-1">Email gửi thông báo (No-reply)</label>
                                            <div className="relative">
                                                <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-400 text-[18px]">alternate_email</span>
                                                <input className="w-full bg-white border border-slate-300 text-sm text-slate-800 rounded-md py-2 pl-10 pr-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all shadow-sm" type="email" defaultValue="system@TEDF.edu.vn" />
                                            </div>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-slate-700 mb-1">SMTP Server</label>
                                            <input className="w-full bg-white border border-slate-300 text-sm text-slate-800 rounded-md py-2 px-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all shadow-sm" type="text" defaultValue="smtp.gmail.com" />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-slate-700 mb-1">Port</label>
                                            <input className="w-full bg-white border border-slate-300 text-sm text-slate-800 rounded-md py-2 px-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all shadow-sm" type="number" defaultValue={587} />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-slate-700 mb-1">SMTP Username</label>
                                            <input className="w-full bg-white border border-slate-300 text-sm text-slate-800 rounded-md py-2 px-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all shadow-sm" type="text" defaultValue="admin_mailer" />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-slate-700 mb-1">SMTP Password</label>
                                            <input className="w-full bg-white border border-slate-300 text-sm text-slate-800 rounded-md py-2 px-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all shadow-sm" type="password" defaultValue="••••••••••••" />
                                        </div>
                                    </div>
                                    <div className="flex justify-end">
                                        <button className="text-sm font-medium text-primary hover:text-primary-light border border-primary/20 bg-primary/5 hover:bg-primary/10 px-3 py-1.5 rounded transition-colors">
                                            Gửi email kiểm tra
                                        </button>
                                    </div>
                                </div>
                            </motion.div>

                            {/* Archive Management */}
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
                                                <th className="px-6 py-3">Kỳ học</th>
                                                <th className="px-6 py-3">Số lượng đề tài</th>
                                                <th className="px-6 py-3">Dung lượng</th>
                                                <th className="px-6 py-3">Trạng thái</th>
                                                <th className="px-6 py-3 text-right">Hành động</th>
                                            </tr>
                                        </thead>
                                        <tbody className="divide-y divide-slate-100">
                                            <tr className="hover:bg-slate-50 transition-colors">
                                                <td className="px-6 py-3 font-medium text-slate-800">Fall 2023</td>
                                                <td className="px-6 py-3">482</td>
                                                <td className="px-6 py-3">1.2 GB</td>
                                                <td className="px-6 py-3"><span className="text-xs bg-slate-100 text-slate-600 px-2 py-1 rounded">Đã lưu trữ</span></td>
                                                <td className="px-6 py-3 text-right">
                                                    <button className="text-primary hover:text-primary-light text-xs font-semibold">Tải xuống</button>
                                                </td>
                                            </tr>
                                            <tr className="hover:bg-slate-50 transition-colors">
                                                <td className="px-6 py-3 font-medium text-slate-800">Spring 2023</td>
                                                <td className="px-6 py-3">512</td>
                                                <td className="px-6 py-3">1.4 GB</td>
                                                <td className="px-6 py-3"><span className="text-xs bg-slate-100 text-slate-600 px-2 py-1 rounded">Đã lưu trữ</span></td>
                                                <td className="px-6 py-3 text-right">
                                                    <button className="text-primary hover:text-primary-light text-xs font-semibold">Tải xuống</button>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                    <div className="p-4 bg-slate-50/50 border-t border-slate-200 flex items-center justify-between">
                                        <div className="text-xs text-slate-500">Tổng dung lượng: <span className="font-bold text-slate-700">2.6 GB / 100 GB</span></div>
                                        <button className="text-xs font-medium text-slate-600 hover:text-primary flex items-center gap-1">
                                            <span className="material-symbols-outlined text-[16px]">settings</span>
                                            Cấu hình tự động lưu trữ
                                        </button>
                                    </div>
                                </div>
                            </motion.div>
                        </div>

                        {/* Right Column */}
                        <div className="lg:col-span-1 space-y-6">
                            {/* UI Settings */}
                            <motion.div variants={item} className="bento-card rounded-md overflow-hidden">
                                <div className="p-4 border-b border-slate-200 bg-slate-50/50 flex items-center gap-2">
                                    <span className="material-symbols-outlined text-primary">palette</span>
                                    <h4 className="font-bold text-slate-800">Giao Diện & Logo</h4>
                                </div>
                                <div className="p-6 space-y-6">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-2">Logo hệ thống</label>
                                        <div className="flex items-center gap-4">
                                            <div className="w-20 h-20 bg-slate-100 rounded-lg border border-slate-200 flex items-center justify-center text-primary font-bold text-2xl overflow-hidden relative group cursor-pointer">
                                                <span className="group-hover:opacity-0 transition-opacity">U</span>
                                                <div className="absolute inset-0 bg-black/40 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                                                    <span className="material-symbols-outlined text-white">edit</span>
                                                </div>
                                            </div>
                                            <div className="flex-1">
                                                <button className="text-sm bg-white border border-slate-300 text-slate-700 hover:bg-slate-50 px-3 py-1.5 rounded-md font-medium w-full mb-2">Tải lên mới</button>
                                                <p className="text-[10px] text-slate-400">PNG, JPG tối đa 2MB. Kích thước đề xuất 512x512px.</p>
                                            </div>
                                        </div>
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-2">Màu chủ đạo</label>
                                        <p className="text-xs text-slate-500 mb-3">Chọn màu để thay đổi giao diện toàn hệ thống</p>
                                        <div className="flex flex-wrap gap-3">
                                            {colorThemes.map((theme) => (
                                                <button
                                                    key={theme.color}
                                                    onClick={() => handleColorSelect(theme.color)}
                                                    title={theme.name}
                                                    className={`w-10 h-10 rounded-full transition-all hover:scale-110 ${pendingColor === theme.color
                                                        ? 'ring-2 ring-offset-2 scale-110'
                                                        : 'hover:ring-2 hover:ring-offset-2'
                                                        }`}
                                                    style={{
                                                        backgroundColor: theme.color,
                                                        '--tw-ring-color': theme.color
                                                    } as React.CSSProperties}
                                                >
                                                    {pendingColor === theme.color && (
                                                        <span className="material-symbols-outlined text-white text-[18px]">check</span>
                                                    )}
                                                </button>
                                            ))}
                                            <button
                                                onClick={() => setShowColorPicker(!showColorPicker)}
                                                className="w-10 h-10 rounded-full bg-slate-100 border-2 border-dashed border-slate-300 flex items-center justify-center cursor-pointer hover:bg-slate-200 hover:border-slate-400 transition-all"
                                            >
                                                <span className="material-symbols-outlined text-[18px] text-slate-500">add</span>
                                            </button>
                                        </div>

                                        {/* Custom Color Picker */}
                                        {showColorPicker && (
                                            <motion.div
                                                initial={{ opacity: 0, y: -10 }}
                                                animate={{ opacity: 1, y: 0 }}
                                                className="mt-4 p-4 bg-slate-50 rounded-lg border border-slate-200"
                                            >
                                                <label className="block text-xs font-medium text-slate-600 mb-2">Màu tùy chỉnh</label>
                                                <div className="flex items-center gap-3">
                                                    <input
                                                        type="color"
                                                        value={customColor}
                                                        onChange={(e) => setCustomColor(e.target.value)}
                                                        className="w-12 h-10 rounded cursor-pointer border-0 p-0"
                                                    />
                                                    <input
                                                        type="text"
                                                        value={customColor}
                                                        onChange={(e) => setCustomColor(e.target.value)}
                                                        className="flex-1 bg-white border border-slate-300 text-sm text-slate-800 rounded-md py-2 px-3 uppercase"
                                                        placeholder="#000000"
                                                    />
                                                    <button
                                                        onClick={handleCustomColorApply}
                                                        className="px-4 py-2 bg-primary text-white text-sm font-medium rounded-md hover:bg-primary-dark transition-colors"
                                                    >
                                                        Áp dụng
                                                    </button>
                                                </div>
                                            </motion.div>
                                        )}

                                        {/* Current color indicator */}
                                        <div className="mt-3 flex items-center gap-2 text-xs text-slate-500">
                                            <div
                                                className="w-4 h-4 rounded-full border border-slate-200"
                                                style={{ backgroundColor: pendingColor }}
                                            />
                                            <span>Màu đang chọn: <span className="font-mono font-medium text-slate-700">{pendingColor}</span></span>
                                        </div>
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-1">Tên hiển thị (Header)</label>
                                        <input className="w-full bg-white border border-slate-300 text-sm text-slate-800 rounded-md py-2 px-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all shadow-sm" type="text" defaultValue="TEDF" />
                                    </div>
                                </div>
                            </motion.div>

                            {/* Maintenance Mode */}
                            <motion.div variants={item} className="bento-card rounded-md overflow-hidden border-orange-200">
                                <div className="p-4 bg-orange-50 border-b border-orange-100 flex items-center gap-2">
                                    <span className="material-symbols-outlined text-orange-600">engineering</span>
                                    <h4 className="font-bold text-orange-800">Bảo Trì Hệ Thống</h4>
                                </div>
                                <div className="p-6">
                                    <p className="text-sm text-slate-600 mb-4">Khi bật chế độ bảo trì, chỉ có Admin mới có thể truy cập hệ thống. Sinh viên và giảng viên sẽ thấy trang thông báo.</p>
                                    <div className="flex items-center justify-between">
                                        <div>
                                            <span className="text-sm font-bold text-slate-700">Chế độ bảo trì</span>
                                            {isMaintenanceMode && (
                                                <span className="ml-2 text-xs font-medium px-2 py-0.5 bg-orange-100 text-orange-700 rounded-full">Đang bật</span>
                                            )}
                                        </div>
                                        <label className="relative inline-flex items-center cursor-pointer">
                                            <input
                                                className="sr-only peer"
                                                type="checkbox"
                                                checked={isMaintenanceMode}
                                                onChange={(e) => setMaintenanceMode(e.target.checked)}
                                            />
                                            <div className="w-11 h-6 bg-slate-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-orange-500"></div>
                                        </label>
                                    </div>
                                </div>
                            </motion.div>

                            {/* System Version */}
                            <motion.div variants={item} className="bg-blue-50 p-4 rounded-md border border-blue-100">
                                <div className="flex items-start gap-3">
                                    <span className="material-symbols-outlined text-blue-600 mt-0.5">info</span>
                                    <div>
                                        <h5 className="text-sm font-bold text-blue-800 mb-1">Phiên bản hệ thống</h5>
                                        <p className="text-xs text-blue-700">v2.4.0 (Build 20240410)</p>
                                        <p className="text-xs text-blue-600 mt-1">Lần cập nhật cuối: 2 ngày trước</p>
                                    </div>
                                </div>
                            </motion.div>
                        </div>
                    </div>
                </motion.div>
            </div>
        </>
    )
}

function ToggleRow({ title, description, defaultChecked = false }: { title: string; description: string; defaultChecked?: boolean }) {
    return (
        <div className="flex items-center justify-between">
            <div>
                <p className="text-sm font-medium text-slate-800">{title}</p>
                <p className="text-xs text-slate-500">{description}</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
                <input className="sr-only peer" type="checkbox" defaultChecked={defaultChecked} />
                <div className="w-11 h-6 bg-slate-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
            </label>
        </div>
    )
}
