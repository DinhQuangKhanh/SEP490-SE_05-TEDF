import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import type { SupervisedProject, TopicDocument } from '@/types'
import { topicService } from '@/lib'
import { TopicAttachmentList } from '@/components/common/TopicAttachmentList'
import { projectStatusLabel, projectStatusColor, getDefenseResult } from '@/lib/projects/projectStatus'

function formatDate(value: string | null): string {
    if (!value) return '—'
    const d = new Date(value)
    return isNaN(d.getTime()) ? '—' : d.toLocaleDateString('vi-VN')
}

/** Detail of a supervised project, with the (status-derived) defense result. */
export function SupervisedProjectDetailModal({
    project,
    onClose,
}: {
    project: SupervisedProject
    onClose: () => void
}) {
    const [documents, setDocuments] = useState<TopicDocument[]>([])

    // Close on Escape for keyboard accessibility.
    useEffect(() => {
        const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
        document.addEventListener('keydown', onKey)
        return () => document.removeEventListener('keydown', onKey)
    }, [onClose])

    // Attachments (register form + documents); a failure just leaves the list empty.
    useEffect(() => {
        topicService.getTopicDocuments(project.id).then(setDocuments).catch(() => setDocuments([]))
    }, [project.id])

    const defense = getDefenseResult(project.statusValue)

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
            {/* Accessible backdrop: a real button so it has keyboard support (instead of a div onClick). */}
            <button
                type="button"
                aria-label="Đóng"
                onClick={onClose}
                className="fixed inset-0 cursor-default"
            />
            <motion.div
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1 }}
                className="relative z-10 bg-white rounded-xl shadow-xl w-full max-w-2xl max-h-[85vh] overflow-hidden flex flex-col"
            >
                <div className="px-6 py-4 border-b border-slate-100 flex items-start justify-between gap-3">
                    <div className="min-w-0">
                        <h3 className="text-lg font-bold text-slate-800 break-words">{project.nameVi}</h3>
                        <p className="text-xs text-slate-500 mt-0.5">{project.code}</p>
                    </div>
                    <button
                        onClick={onClose}
                        className="p-1 text-slate-400 hover:text-slate-600 rounded-md hover:bg-slate-100 shrink-0"
                    >
                        <span className="material-symbols-outlined">close</span>
                    </button>
                </div>

                <div className="p-6 space-y-5 overflow-y-auto">
                    <div className="flex flex-wrap items-center gap-2">
                        <span className={`inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium border ${projectStatusColor(project.statusValue)}`}>
                            {projectStatusLabel(project.statusValue)}
                        </span>
                        <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-semibold border ${defense.color}`}>
                            <span className="material-symbols-outlined text-[16px]">{defense.icon}</span>
                            Kết quả bảo vệ: {defense.label}
                        </span>
                    </div>

                    {project.nameEn && <Field label="Tên tiếng Anh" value={project.nameEn} />}

                    <div className="grid grid-cols-2 gap-4">
                        <Field label="Học kỳ" value={project.semesterName} />
                        <Field label="Nhóm" value={project.groupCode ? `Nhóm ${project.groupCode}` : '—'} />
                        <Field label="Ngày bắt đầu" value={formatDate(project.startDate)} />
                        <Field label="Hạn nộp" value={formatDate(project.deadline)} />
                    </div>

                    {project.description && <Field label="Mô tả" value={project.description} multiline />}
                    {project.objectives && <Field label="Mục tiêu" value={project.objectives} multiline />}

                    <div>
                        <p className="text-xs text-slate-400 uppercase tracking-wider font-medium mb-1">Tài liệu đính kèm</p>
                        <TopicAttachmentList documents={documents} title={null} />
                    </div>

                    <p className="text-[11px] text-slate-400 italic pt-3 border-t border-slate-100">
                        * "Kết quả bảo vệ" hiện được suy ra từ trạng thái đồ án — hệ thống chưa có trường điểm/kết quả bảo vệ riêng.
                    </p>
                </div>
            </motion.div>
        </div>
    )
}

function Field({ label, value, multiline = false }: { label: string; value: string; multiline?: boolean }) {
    return (
        <div className="min-w-0">
            <p className="text-xs text-slate-400 uppercase tracking-wider font-medium mb-1">{label}</p>
            <p className={`text-sm text-slate-800 break-words ${multiline ? 'whitespace-pre-wrap leading-relaxed' : 'font-medium'}`}>
                {value}
            </p>
        </div>
    )
}
