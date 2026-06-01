import { useState, useRef, useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { apiClient } from '@/lib/apiClient'
import { SemesterDto } from '@/types/admin.types'

interface EditSemesterModalProps {
    isOpen: boolean
    onClose: () => void
    onUpdated?: () => void
    initialData: SemesterDto | null
}

interface PhaseInput {
    id: number
    name: string
    type: string
    startDate: string
    endDate: string
}

const PHASES_TEMPLATE = [
    { label: '1. Đăng ký', name: 'Đăng ký đề tài', type: 'Registration', color: 'text-primary' },
    { label: '2. Thẩm định', name: 'Thẩm định đề tài', type: 'Evaluation', color: 'text-orange-600' },
    { label: '3. Thực hiện', name: 'Thực hiện đồ án', type: 'Implementation', color: 'text-emerald-600' },
    { label: '4. Bảo vệ', name: 'Bảo vệ đồ án', type: 'Defense', color: 'text-purple-600' },
]

export function EditSemesterModal({ isOpen, onClose, onUpdated, initialData }: EditSemesterModalProps) {
    const [name, setName] = useState('')
    const [startDate, setStartDate] = useState('')
    const [endDate, setEndDate] = useState('')
    const [description, setDescription] = useState('')
    const [phases, setPhases] = useState<PhaseInput[]>([])
    const [uploadedFile, setUploadedFile] = useState<File | null>(null)
    const fileInputRef = useRef<HTMLInputElement>(null)

    const [isSubmitting, setIsSubmitting] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [success, setSuccess] = useState(false)
    const contentRef = useRef<HTMLDivElement>(null)

    useEffect(() => {
        if (initialData && isOpen) {
            setName(initialData.name)
            setStartDate(initialData.startDate.split('T')[0])
            setEndDate(initialData.endDate.split('T')[0])
            setDescription(initialData.description || '')
            setPhases(
                initialData.phases
                    .slice()
                    .sort((a, b) => a.order - b.order)
                    .map((p) => ({
                        id: p.id,
                        name: p.name,
                        type: p.type,
                        startDate: p.startDate.split('T')[0],
                        endDate: p.endDate.split('T')[0],
                    }))
            )
            setError(null)
            setSuccess(false)
            setUploadedFile(null)
            if (fileInputRef.current) fileInputRef.current.value = ''
        }
    }, [initialData, isOpen])

    const showError = (msg: string) => {
        setError(msg)
        contentRef.current?.scrollTo({ top: 0, behavior: 'smooth' })
    }

    const handleDismiss = () => {
        setError(null)
        onClose()
    }

    const handleFileSelect = (file: File | undefined) => {
        if (!file) return
        const ext = file.name.split('.').pop()?.toLowerCase()
        if (!['csv', 'xlsx', 'xls'].includes(ext ?? '')) {
            showError('Chỉ chấp nhận file .csv, .xlsx hoặc .xls')
            return
        }
        setUploadedFile(file)
        setError(null)
    }

    const updatePhase = (index: number, field: 'startDate' | 'endDate', value: string) => {
        setPhases((prev) => prev.map((p, i) => (i === index ? { ...p, [field]: value } : p)))
    }

    const handleSubmit = async () => {
        if (!initialData) return

        if (!name.trim()) { showError('Vui lòng nhập tên kỳ học.'); return }
        if (!startDate) { showError('Vui lòng chọn ngày bắt đầu.'); return }
        if (!endDate) { showError('Vui lòng chọn ngày kết thúc.'); return }

        const semStart = new Date(startDate)
        const semEnd = new Date(endDate)

        if (semEnd <= semStart) { showError('Ngày kết thúc phải sau ngày bắt đầu.'); return }

        // Validate phases
        const validPhases = phases.filter((p) => p.startDate && p.endDate)
        for (let i = 0; i < validPhases.length; i++) {
            const pStart = new Date(validPhases[i].startDate)
            const pEnd = new Date(validPhases[i].endDate)
            const templateIndex = PHASES_TEMPLATE.findIndex((t) => t.type === validPhases[i].type)
            const pName = templateIndex >= 0 ? PHASES_TEMPLATE[templateIndex].label : `Giai đoạn ${i + 1}`

            if (pEnd <= pStart) { showError(`${pName}: Ngày kết thúc phải sau ngày bắt đầu.`); return }
            if (pStart < semStart) { showError(`${pName}: Ngày bắt đầu không được trước ngày bắt đầu kỳ học.`); return }
            if (pEnd > semEnd) { showError(`${pName}: Ngày kết thúc không được sau ngày kết thúc kỳ học.`); return }

            if (i > 0) {
                const prevEnd = new Date(validPhases[i - 1].endDate)
                const prevTemplateIndex = PHASES_TEMPLATE.findIndex((t) => t.type === validPhases[i - 1].type)
                const prevName = prevTemplateIndex >= 0 ? PHASES_TEMPLATE[prevTemplateIndex].label : `Giai đoạn ${i}`
                if (pStart < prevEnd) {
                    showError(`${pName}: Ngày bắt đầu phải sau hoặc bằng ngày kết thúc của ${prevName}.`)
                    return
                }
            }
        }

        setIsSubmitting(true)
        setError(null)

        try {
            await apiClient.put(`/api/admin/semesters/${initialData.id}`, {
                id: initialData.id,
                name: name.trim(),
                startDate: new Date(startDate).toISOString(),
                endDate: new Date(endDate).toISOString(),
                description: description.trim() || null,
                phases: validPhases.map((p) => ({
                    id: p.id,
                    startDate: new Date(p.startDate).toISOString(),
                    endDate: new Date(p.endDate).toISOString(),
                })),
            })
            setSuccess(true)
            onUpdated?.()
            setTimeout(() => {
                onClose()
            }, 1500)
        } catch (err) {
            showError(err instanceof Error ? err.message : 'Có lỗi xảy ra khi cập nhật kỳ học.')
        } finally {
            setIsSubmitting(false)
        }
    }

    // Build phase display: use existing phases matched to template order
    const displayPhases = PHASES_TEMPLATE.map((template, index) => {
        const existing = phases.find((p) => p.type === template.type)
        return {
            ...template,
            id: existing?.id ?? 0,
            startDate: existing?.startDate ?? '',
            endDate: existing?.endDate ?? '',
            index: existing ? phases.indexOf(existing) : index,
        }
    })

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
                        className="bg-white w-full max-w-4xl max-h-[90vh] rounded-xl shadow-2xl overflow-hidden flex flex-col"
                    >
                        {/* Header */}
                        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-white shrink-0">
                            <div>
                                <h2 className="text-xl font-bold text-slate-800">Chỉnh sửa Kỳ Học</h2>
                                <p className="text-sm text-slate-500">Cập nhật thời gian và đối tượng tham gia đồ án</p>
                            </div>
                            <div className="flex items-center gap-2">
                                <button onClick={handleDismiss} className="text-slate-400 hover:text-slate-600 transition-colors p-1 hover:bg-slate-100 rounded-lg">
                                    <span className="material-symbols-outlined">close</span>
                                </button>
                            </div>
                        </div>

                        {/* Content */}
                        <div ref={contentRef} className="flex-1 overflow-y-auto p-6 space-y-8">
                            {/* Error / Success */}
                            {error && (
                                <div className="p-3 bg-red-50 border border-red-200 rounded-md flex items-start gap-3">
                                    <span className="material-symbols-outlined text-red-600 text-[20px] mt-0.5">error</span>
                                    <p className="text-sm text-red-800">{error}</p>
                                </div>
                            )}
                            {success && (
                                <div className="p-3 bg-green-50 border border-green-200 rounded-md flex items-start gap-3">
                                    <span className="material-symbols-outlined text-green-600 text-[20px] mt-0.5">check_circle</span>
                                    <p className="text-sm text-green-800 font-semibold">Cập nhật kỳ học thành công!</p>
                                </div>
                            )}

                            {/* Section 1: General Info */}
                            <motion.section initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1 }}>
                                <div className="flex items-center gap-2 mb-4">
                                    <span className="w-8 h-8 rounded bg-blue-50 text-primary flex items-center justify-center font-bold text-sm">1</span>
                                    <h3 className="font-bold text-slate-700">Thông tin chung</h3>
                                </div>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                    <div>
                                        <label className="block text-sm font-semibold text-slate-700 mb-1.5">Tên kỳ học <span className="text-red-500">*</span></label>
                                        <input className="w-full border border-slate-200 rounded-md px-3 py-2 focus:ring-2 focus:ring-primary/20 focus:border-primary text-sm outline-none transition-all" placeholder="VD: Summer 2024" type="text" value={name} onChange={(e) => setName(e.target.value)} />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-semibold text-slate-700 mb-1.5">Mã kỳ học</label>
                                        <input className="w-full border border-slate-200 rounded-md px-3 py-2 text-sm outline-none transition-all bg-slate-50 text-slate-500 cursor-not-allowed" type="text" value={initialData?.code ?? ''} readOnly />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-semibold text-slate-700 mb-1.5">Ngày bắt đầu <span className="text-red-500">*</span></label>
                                        <input className="w-full border border-slate-200 rounded-md px-3 py-2 focus:ring-2 focus:ring-primary/20 focus:border-primary text-sm outline-none transition-all" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-semibold text-slate-700 mb-1.5">Ngày kết thúc <span className="text-red-500">*</span></label>
                                        <input className="w-full border border-slate-200 rounded-md px-3 py-2 focus:ring-2 focus:ring-primary/20 focus:border-primary text-sm outline-none transition-all" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
                                    </div>
                                </div>
                            </motion.section>

                            {/* Section 2: Timeline */}
                            <motion.section initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.2 }}>
                                <div className="flex items-center gap-2 mb-4">
                                    <span className="w-8 h-8 rounded bg-blue-50 text-primary flex items-center justify-center font-bold text-sm">2</span>
                                    <h3 className="font-bold text-slate-700">Thiết lập giai đoạn (Timeline)</h3>
                                </div>
                                <div className="bg-slate-50 rounded-lg p-4 border border-slate-100">
                                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                                        {displayPhases.map((phase) => (
                                            <div key={phase.type} className="bg-white p-3 rounded border border-slate-200 shadow-sm hover:shadow-md transition-shadow">
                                                <p className={`text-xs font-bold ${phase.color} uppercase mb-2`}>{phase.label}</p>
                                                <input className="w-full border-none p-0 text-sm focus:ring-0 bg-transparent" type="date" placeholder="Từ ngày" value={phase.startDate} onChange={(e) => updatePhase(phase.index, 'startDate', e.target.value)} />
                                                <div className="h-px bg-slate-100 my-1" />
                                                <input className="w-full border-none p-0 text-sm focus:ring-0 bg-transparent" type="date" placeholder="Đến ngày" value={phase.endDate} onChange={(e) => updatePhase(phase.index, 'endDate', e.target.value)} />
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            </motion.section>

                            {/* Section 3: Participants */}
                            <motion.section initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.3 }}>
                                <div className="flex items-center gap-2 mb-4">
                                    <span className="w-8 h-8 rounded bg-blue-50 text-primary flex items-center justify-center font-bold text-sm">3</span>
                                    <div className="flex items-center gap-2">
                                        <h3 className="font-bold text-slate-700">Đối tượng tham gia</h3>
                                        <span className="bg-primary text-white text-[10px] px-1.5 py-0.5 rounded font-bold uppercase tracking-tight">Quan trọng</span>
                                    </div>
                                </div>
                                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                                    {/* Upload Excel */}
                                    <input
                                        ref={fileInputRef}
                                        type="file"
                                        accept=".csv,.xlsx,.xls"
                                        className="hidden"
                                        onChange={(e) => handleFileSelect(e.target.files?.[0])}
                                    />
                                    <div
                                        className={`border-2 border-dashed rounded-xl p-6 transition-colors group cursor-pointer ${uploadedFile ? 'border-green-300 bg-green-50/30' : 'border-slate-200 hover:border-primary/50'
                                            }`}
                                        onClick={() => !uploadedFile && fileInputRef.current?.click()}
                                        onDragOver={(e) => { e.preventDefault(); e.stopPropagation() }}
                                        onDrop={(e) => { e.preventDefault(); e.stopPropagation(); handleFileSelect(e.dataTransfer.files?.[0]) }}
                                    >
                                        <div className="flex flex-col items-center text-center">
                                            {uploadedFile ? (
                                                <>
                                                    <div className="w-12 h-12 rounded-full bg-green-100 flex items-center justify-center text-green-600 mb-3">
                                                        <span className="material-symbols-outlined text-3xl">check_circle</span>
                                                    </div>
                                                    <h4 className="font-bold text-slate-800 mb-1">Đã tải lên</h4>
                                                    <p className="text-xs text-slate-600 mb-1 font-medium">{uploadedFile.name}</p>
                                                    <p className="text-[10px] text-slate-400 mb-3">{(uploadedFile.size / 1024).toFixed(1)} KB</p>
                                                    <button
                                                        onClick={(e) => { e.stopPropagation(); setUploadedFile(null); if (fileInputRef.current) fileInputRef.current.value = '' }}
                                                        className="py-1.5 px-4 bg-red-50 border border-red-200 rounded-md text-xs font-semibold text-red-600 hover:bg-red-100 transition-colors"
                                                    >
                                                        Xóa tệp tin
                                                    </button>
                                                </>
                                            ) : (
                                                <>
                                                    <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center text-slate-400 group-hover:bg-primary/10 group-hover:text-primary transition-colors mb-3">
                                                        <span className="material-symbols-outlined text-3xl">upload_file</span>
                                                    </div>
                                                    <h4 className="font-bold text-slate-800 mb-1">Tải danh sách Excel</h4>
                                                    <p className="text-xs text-slate-500 mb-4 px-4">Kéo thả hoặc bấm để tải lên file .csv / .xlsx chứa danh sách MSSV đủ điều kiện.</p>
                                                    <button
                                                        type="button"
                                                        onClick={(e) => { e.stopPropagation(); fileInputRef.current?.click() }}
                                                        className="w-full py-2 px-4 bg-white border border-slate-300 rounded-md text-sm font-semibold text-slate-700 hover:bg-slate-50 transition-colors"
                                                    >
                                                        Chọn tệp tin
                                                    </button>
                                                </>
                                            )}
                                            <a className="mt-3 text-[11px] text-primary hover:underline flex items-center gap-1" href="/templates/danh_sach_sinh_vien_mau.csv" download>
                                                <span className="material-symbols-outlined text-[14px]">download</span> Tải file mẫu (.csv)
                                            </a>
                                        </div>
                                    </div>

                                    {/* Filter */}
                                    <div className="border border-slate-200 rounded-xl p-6 bg-slate-50/50">
                                        <h4 className="font-bold text-slate-800 mb-3 flex items-center gap-2">
                                            <span className="material-symbols-outlined text-[20px] text-primary">filter_alt</span>
                                            Lọc theo điều kiện
                                        </h4>
                                        <div className="space-y-4">
                                            <div>
                                                <label className="block text-xs font-semibold text-slate-500 uppercase mb-1.5">Khóa &amp; Ngành</label>
                                                <div className="grid grid-cols-2 gap-2">
                                                    <select className="text-xs border border-slate-200 rounded-md px-2 py-2 focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none">
                                                        <option>Tất cả Khóa</option>
                                                        <option>K2020</option>
                                                        <option>K2021</option>
                                                    </select>
                                                    <select className="text-xs border border-slate-200 rounded-md px-2 py-2 focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none">
                                                        <option>Tất cả Ngành</option>
                                                        <option>CNTT</option>
                                                        <option>Kỹ thuật phần mềm</option>
                                                    </select>
                                                </div>
                                            </div>
                                            <div>
                                                <label className="block text-xs font-semibold text-slate-500 uppercase mb-1.5">Điểm tích lũy (GPA) tối thiểu</label>
                                                <div className="flex items-center gap-3">
                                                    <input className="w-full text-xs border border-slate-200 rounded-md px-2 py-2 focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none" placeholder="VD: 2.5" step="0.1" type="number" />
                                                    <span className="text-xs text-slate-400 font-medium whitespace-nowrap">/ 4.0</span>
                                                </div>
                                            </div>
                                            <div className="pt-2">
                                                <button className="w-full py-2 px-4 bg-primary/10 text-primary border border-primary/20 rounded-md text-xs font-bold hover:bg-primary/20 transition-all">
                                                    Xem trước danh sách (245 SV)
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                {/* Info Note */}
                                <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.4 }} className="mt-4 p-3 bg-blue-50 border border-blue-100 rounded-md flex items-start gap-3">
                                    <span className="material-symbols-outlined text-blue-600 text-[20px] mt-0.5">verified</span>
                                    <p className="text-xs text-blue-800 leading-relaxed">
                                        <strong>Lưu ý:</strong> Sau khi xác nhận, hệ thống sẽ tự động gắn thẻ
                                        <span className="px-1.5 py-0.5 bg-blue-100 border border-blue-200 rounded text-[10px] font-bold mx-1">ĐỦ ĐIỀU KIỆN LÀM ĐỒ ÁN</span>
                                        cho các sinh viên thuộc danh sách trên. Chỉ những SV này mới có quyền đăng ký đề tài trong kỳ học.
                                    </p>
                                </motion.div>
                            </motion.section>
                        </div>

                        {/* Footer */}
                        <div className="px-6 py-4 border-t border-slate-100 flex items-center justify-end gap-3 bg-slate-50/50 shrink-0">
                            <button onClick={handleDismiss} disabled={isSubmitting} className="px-4 py-2 text-sm font-semibold text-slate-600 hover:text-slate-800 transition-colors disabled:opacity-50">Hủy bỏ</button>
                            <button
                                onClick={handleSubmit}
                                disabled={isSubmitting || success}
                                className="px-6 py-2 bg-primary text-white rounded-md text-sm font-bold shadow-lg shadow-primary/20 hover:bg-primary/90 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                            >
                                {isSubmitting ? (
                                    <><span className="material-symbols-outlined animate-spin text-[18px]">progress_activity</span>Đang lưu...</>
                                ) : 'Cập nhật kỳ học'}
                            </button>
                        </div>
                    </motion.div>
                </motion.div>
            )}
        </AnimatePresence>
    )
}
