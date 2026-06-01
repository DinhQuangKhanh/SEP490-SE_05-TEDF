import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { apiClient } from '@/lib/apiClient'

interface CreateTicketModalProps {
    isOpen: boolean
    onClose: () => void
    onCreated?: () => void
}

export function CreateTicketModal({ isOpen, onClose, onCreated }: CreateTicketModalProps) {
    const [title, setTitle] = useState('')
    const [description, setDescription] = useState('')
    const [category, setCategory] = useState(1) // 1: Technical, 2: Academic, 3: Account, 4: Other
    const [priority, setPriority] = useState(1) // 0: Low, 1: Medium, 2: High

    const [isSubmitting, setIsSubmitting] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [success, setSuccess] = useState(false)

    const resetForm = () => {
        setTitle('')
        setDescription('')
        setCategory(1)
        setPriority(1)
        setError(null)
        setSuccess(false)
    }

    const handleDismiss = () => {
        if (!isSubmitting) {
            setError(null)
            onClose()
        }
    }

    const handleSubmit = async () => {
        if (!title.trim()) { setError('Vui lòng nhập tiêu đề yêu cầu.'); return }
        if (!description.trim()) { setError('Vui lòng nhập nội dung chi tiết.'); return }

        setIsSubmitting(true)
        setError(null)

        try {
            await apiClient.post('/api/supports', {
                title: title.trim(),
                description: description.trim(),
                category: Number(category),
                priority: Number(priority),
            })
            
            setSuccess(true)
            onCreated?.()
            setTimeout(() => {
                resetForm()
                onClose()
            }, 1000)
        } catch (err: any) {
            setError(err.response?.data?.message || err.message || 'Có lỗi xảy ra khi tạo yêu cầu mới!')
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <AnimatePresence>
            {isOpen && (
                <motion.div
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4"
                    onClick={handleDismiss}
                >
                    <motion.div
                        initial={{ opacity: 0, scale: 0.95, y: 20 }}
                        animate={{ opacity: 1, scale: 1, y: 0 }}
                        exit={{ opacity: 0, scale: 0.95, y: 20 }}
                        transition={{ type: 'spring', damping: 25, stiffness: 300 }}
                        onClick={(e) => e.stopPropagation()}
                        className="bg-white w-full max-w-2xl rounded-xl shadow-2xl overflow-hidden flex flex-col"
                    >
                        {/* Header */}
                        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-white shrink-0">
                            <div>
                                <h2 className="text-xl font-bold text-slate-800">Tạo Yêu Cầu Hỗ Trợ</h2>
                                <p className="text-sm text-slate-500">Mô tả vấn đề bạn đang gặp phải</p>
                            </div>
                            <button onClick={handleDismiss} className="text-slate-400 hover:text-slate-600 transition-colors p-1 hover:bg-slate-100 rounded-lg">
                                <span className="material-symbols-outlined">close</span>
                            </button>
                        </div>

                        {/* Content */}
                        <div className="flex-1 overflow-y-auto p-6 space-y-5">
                            {error && (
                                <div className="p-3 bg-red-50 border border-red-200 rounded-md flex items-start gap-3">
                                    <span className="material-symbols-outlined text-red-600 text-[20px] mt-0.5">error</span>
                                    <p className="text-sm text-red-800">{error}</p>
                                </div>
                            )}
                            {success && (
                                <div className="p-3 bg-green-50 border border-green-200 rounded-md flex items-start gap-3">
                                    <span className="material-symbols-outlined text-green-600 text-[20px] mt-0.5">check_circle</span>
                                    <p className="text-sm text-green-800 font-semibold">Tạo yêu cầu thành công!</p>
                                </div>
                            )}

                            <div>
                                <label className="block text-sm font-semibold text-slate-700 mb-1.5">Tiêu đề <span className="text-red-500">*</span></label>
                                <input 
                                    className="w-full border border-slate-200 rounded-md px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary" 
                                    placeholder="Tóm tắt vấn đề..." 
                                    type="text" 
                                    value={title} 
                                    onChange={(e) => setTitle(e.target.value)} 
                                />
                            </div>

                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Phân loại <span className="text-red-500">*</span></label>
                                    <select 
                                        className="w-full border border-slate-200 rounded-md px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                                        value={category}
                                        onChange={(e) => setCategory(Number(e.target.value))}
                                    >
                                        <option value={1}>🏢 Lỗi kỹ thuật / Hệ thống</option>
                                        <option value={2}>📚 Vấn đề học vụ</option>
                                        <option value={3}>👤 Tài khoản</option>
                                        <option value={4}>📝 Khác</option>
                                    </select>
                                </div>
                                <div>
                                    <label className="block text-sm font-semibold text-slate-700 mb-1.5">Mức độ ưu tiên <span className="text-red-500">*</span></label>
                                    <select 
                                        className="w-full border border-slate-200 rounded-md px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                                        value={priority}
                                        onChange={(e) => setPriority(Number(e.target.value))}
                                    >
                                        <option value={0}>Thấp</option>
                                        <option value={1}>Trung bình</option>
                                        <option value={2}>Cao (Khẩn cấp)</option>
                                    </select>
                                </div>
                            </div>

                            <div>
                                <label className="block text-sm font-semibold text-slate-700 mb-1.5">Chi tiết vấn đề <span className="text-red-500">*</span></label>
                                <textarea 
                                    className="w-full h-32 border border-slate-200 rounded-md px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary resize-none custom-scrollbar" 
                                    placeholder="Mô tả cụ thể lỗi gặp phải..." 
                                    value={description} 
                                    onChange={(e) => setDescription(e.target.value)} 
                                />
                            </div>
                        </div>

                        {/* Footer */}
                        <div className="px-6 py-4 border-t border-slate-100 flex items-center justify-end gap-3 bg-slate-50/50 shrink-0">
                            <button onClick={handleDismiss} disabled={isSubmitting} className="px-4 py-2 text-sm font-semibold text-slate-600 hover:text-slate-800 transition-colors disabled:opacity-50">Hủy</button>
                            <button
                                onClick={handleSubmit}
                                disabled={isSubmitting || success}
                                className="px-5 py-2 bg-primary text-white rounded-md text-sm font-bold shadow-lg shadow-primary/20 hover:bg-primary/90 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                            >
                                {isSubmitting ? (
                                    <><span className="material-symbols-outlined animate-spin text-[18px]">progress_activity</span>Đang gửi...</>
                                ) : 'Gửi yêu cầu'}
                            </button>
                        </div>
                    </motion.div>
                </motion.div>
            )}
        </AnimatePresence>
    )
}
