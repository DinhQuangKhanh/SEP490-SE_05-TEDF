import { motion } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { NotificationDropdown } from '@/components/layout'

const container = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { staggerChildren: 0.05 } }
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 }
}

const technologies = ['Python, C++ (Embedded)', 'ROS (Robot Operating System 2)', 'OpenCV, TensorFlow Lite (Edge AI)', 'Flutter (Mobile App)']
const hardware = ['Khung xe robot 4 bánh (Omni wheels)', 'RPLidar A1, Camera Module v2', 'Jetson Nano (AI Processing Unit)']

const feedbackHistory = [
    {
        author: 'Hội đồng Chuyên môn',
        time: '10:30 AM - Hôm nay',
        status: 'revision',
        content: 'Cần điều chỉnh nội dung:',
        details: 'Đề tài có tính thực tiễn nhưng phạm vi quá rộng cho đồ án tốt nghiệp. Cần làm rõ các ý sau:',
        points: ['Giới hạn cụ thể các tính năng AI sẽ triển khai (chỉ té ngã hay cả hành vi khác?).', 'Bổ sung bảng dự trù kinh phí phần cứng (hiện tại chưa có).', 'Đánh giá rủi ro khả năng hoàn thành trong 4 tháng.'],
    },
    {
        author: 'TS. Trần Minh B',
        time: '09:00 AM - 08/10/2023',
        status: 'approved',
        content: 'Đồng ý về mặt chuyên môn. Đề tài phù hợp với hướng nghiên cứu IoT của bộ môn năm nay. Chuyển lên hội đồng trường thẩm định.',
    },
    {
        author: 'Nguyễn Văn A (Mentor)',
        time: '14:20 PM - 05/10/2023',
        status: 'sent',
        content: 'Đã gửi hồ sơ đề tài lên hệ thống.',
    },
]

export function MentorFeedbackPage() {
    const navigate = useNavigate()

    return (
        <>
            {/* Header */}
            <header className="h-16 flex items-center justify-between px-8 bg-white border-b border-slate-200 flex-shrink-0 z-50 shadow-sm">
                <nav className="flex text-sm font-medium text-slate-500 items-center">
                    <a href="#" className="hover:text-slate-900">Danh sách đề tài</a>
                    <span className="material-symbols-outlined text-slate-400 text-base mx-1">chevron_right</span>
                    <span className="text-slate-900 font-bold">Chi tiết thẩm định</span>
                </nav>
                <NotificationDropdown role="mentor" />
            </header>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-8 bg-slate-100">
                <motion.div variants={container} initial="hidden" animate="show" className="space-y-6">
                    {/* Back & Title */}
                    <motion.div variants={item} className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                        <div>
                            <button
                                onClick={() => navigate(-1)}
                                className="flex items-center gap-2 mb-2 text-slate-500 text-sm hover:text-primary transition-colors"
                            >
                                <span className="material-symbols-outlined text-[18px]">arrow_back</span>
                                <span>Quay lại danh sách</span>
                            </button>
                            <h2 className="text-2xl font-bold text-slate-900">Chi Tiết Thẩm Định &amp; Feedback</h2>
                            <p className="text-slate-500 mt-1 text-sm">Xem chi tiết nội dung và lịch sử phản hồi từ hội đồng chuyên môn.</p>
                        </div>
                        <div className="flex gap-3">
                            <button className="px-4 py-2 border border-slate-300 rounded-lg text-slate-700 font-medium bg-white hover:bg-slate-50 shadow-sm text-sm flex items-center gap-2">
                                <span className="material-symbols-outlined text-[20px]">print</span>
                                In phiếu
                            </button>
                        </div>
                    </motion.div>

                    {/* Project Info Card */}
                    <motion.div variants={item} className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
                        <div className="flex flex-col lg:flex-row justify-between items-start lg:items-center gap-6">
                            <div className="flex items-start gap-5">
                                <div className="size-16 bg-rose-50 border border-rose-100 rounded-xl flex items-center justify-center text-rose-600 flex-shrink-0 shadow-sm">
                                    <span className="material-symbols-outlined text-[32px]">smart_toy</span>
                                </div>
                                <div>
                                    <div className="flex flex-wrap items-center gap-3">
                                        <h3 className="text-xl font-bold text-slate-900">Phát triển Robot hỗ trợ người già tại nhà</h3>
                                        <span className="px-3 py-1 rounded-full text-xs font-bold bg-rose-100 text-rose-700 border border-rose-200 uppercase tracking-wide">
                                            Yêu cầu sửa
                                        </span>
                                    </div>
                                    <div className="flex flex-wrap items-center gap-x-6 gap-y-2 mt-3 text-sm text-slate-500">
                                        <div className="flex items-center gap-2">
                                            <span className="material-symbols-outlined text-[18px] text-slate-400">fingerprint</span>
                                            <span className="font-mono font-medium text-slate-700">PROJ-2024-089</span>
                                        </div>
                                        <div className="flex items-center gap-2">
                                            <span className="material-symbols-outlined text-[18px] text-slate-400">category</span>
                                            <span>Lĩnh vực: <span className="text-slate-700 font-medium">IoT &amp; Robotics</span></span>
                                        </div>
                                        <div className="flex items-center gap-2">
                                            <span className="material-symbols-outlined text-[18px] text-slate-400">calendar_today</span>
                                            <span>Ngày gửi: <span className="text-slate-700 font-medium">05/10/2023</span></span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div className="w-full lg:w-auto p-4 bg-slate-50 rounded-lg border border-slate-200 lg:min-w-[280px]">
                                <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Thông tin Sinh viên</p>
                                <div className="flex items-center gap-3">
                                    <div className="size-8 rounded-full bg-slate-200 flex items-center justify-center text-slate-500 text-xs font-bold">SV</div>
                                    <div>
                                        <p className="text-sm font-bold text-slate-900">Lê Hoàng Nam</p>
                                        <p className="text-xs text-slate-500">K15 - Kỹ thuật phần mềm</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </motion.div>

                    {/* Content Grid */}
                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                        {/* Project Content */}
                        <motion.div variants={item} className="lg:col-span-2 space-y-6">
                            <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
                                <div className="px-6 py-4 border-b border-slate-100 bg-white flex justify-between items-center">
                                    <h3 className="font-bold text-slate-900 text-lg flex items-center gap-2">
                                        <span className="material-symbols-outlined text-primary">description</span>
                                        Nội dung đề tài
                                    </h3>
                                    <button className="text-slate-500 text-sm font-medium hover:text-primary flex items-center gap-1 transition-colors">
                                        <span className="material-symbols-outlined text-[18px]">edit</span>
                                        Sửa đề tài
                                    </button>
                                </div>
                                <div className="p-6 space-y-8">
                                    <section>
                                        <h4 className="text-sm font-bold text-slate-900 uppercase tracking-wider mb-3 border-l-4 border-primary pl-3">
                                            Mô tả tổng quan
                                        </h4>
                                        <div className="bg-slate-50 p-4 rounded-lg border border-slate-100">
                                            <p className="text-slate-700 leading-relaxed text-sm text-justify">
                                                Dự án tập trung vào việc nghiên cứu và phát triển một mẫu robot di động (Mobile Robot) có khả năng hỗ trợ người cao tuổi trong sinh hoạt hàng ngày.
                                                Robot sẽ được tích hợp các cảm biến sinh trắc học để giám sát sức khỏe liên tục (nhịp tim, huyết áp, phát hiện té ngã) và module điều hướng tự động (SLAM) để hỗ trợ di chuyển an toàn trong nhà.
                                            </p>
                                        </div>
                                    </section>

                                    <section className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                        <div>
                                            <h4 className="text-sm font-bold text-slate-900 uppercase tracking-wider mb-3 border-l-4 border-primary pl-3">Công nghệ sử dụng</h4>
                                            <ul className="space-y-2">
                                                {technologies.map((tech) => (
                                                    <li key={tech} className="flex items-center gap-2 text-sm text-slate-700">
                                                        <span className="material-symbols-outlined text-[16px] text-green-600">check_circle</span>
                                                        {tech}
                                                    </li>
                                                ))}
                                            </ul>
                                        </div>
                                        <div>
                                            <h4 className="text-sm font-bold text-slate-900 uppercase tracking-wider mb-3 border-l-4 border-primary pl-3">Yêu cầu phần cứng</h4>
                                            <ul className="space-y-2">
                                                {hardware.map((hw) => (
                                                    <li key={hw} className="flex items-center gap-2 text-sm text-slate-700">
                                                        <span className="material-symbols-outlined text-[16px] text-blue-600">hardware</span>
                                                        {hw}
                                                    </li>
                                                ))}
                                            </ul>
                                        </div>
                                    </section>

                                    <section>
                                        <h4 className="text-sm font-bold text-slate-900 uppercase tracking-wider mb-3 border-l-4 border-primary pl-3">Kết quả dự kiến</h4>
                                        <div className="space-y-3">
                                            {[
                                                'Hoàn thiện mô hình phần cứng robot với khả năng tự hành cơ bản, tránh vật cản trong môi trường trong nhà.',
                                                'Xây dựng ứng dụng mobile hoàn chỉnh để người thân theo dõi camera và nhận cảnh báo té ngã real-time.',
                                                'Báo cáo thực nghiệm độ chính xác của thuật toán phát hiện té ngã đạt trên 85% trong điều kiện ánh sáng phòng.',
                                            ].map((result, idx) => (
                                                <div key={idx} className="flex items-start gap-3 p-3 rounded-lg border border-slate-200 hover:border-primary/50 transition-colors bg-white">
                                                    <span className="bg-primary text-white text-xs font-bold size-5 flex items-center justify-center rounded flex-shrink-0 mt-0.5">
                                                        {idx + 1}
                                                    </span>
                                                    <p className="text-sm text-slate-700">{result}</p>
                                                </div>
                                            ))}
                                        </div>
                                    </section>
                                </div>
                            </div>
                        </motion.div>

                        {/* Feedback History */}
                        <motion.div variants={item} className="lg:col-span-1">
                            <div className="bg-white rounded-xl border border-slate-200 shadow-sm flex flex-col sticky top-6">
                                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                                    <h3 className="font-bold text-slate-900 text-lg flex items-center gap-2">
                                        <span className="material-symbols-outlined text-primary">history</span>
                                        Lịch sử Thẩm định
                                    </h3>
                                </div>
                                <div className="p-6 overflow-y-auto max-h-[calc(100vh-350px)]">
                                    <div className="relative pl-6 border-l-2 border-slate-200 space-y-8">
                                        {feedbackHistory.map((fb, idx) => (
                                            <div key={idx} className="relative group">
                                                <div className={`absolute -left-[31px] bg-white border-2 rounded-full size-4 mt-1.5 z-10 shadow-sm ${fb.status === 'revision' ? 'border-rose-500' : fb.status === 'approved' ? 'border-emerald-500' : 'border-slate-400'
                                                    }`} />
                                                <div className={`flex flex-col gap-3 ${idx > 0 ? 'opacity-80 group-hover:opacity-100 transition-opacity' : ''}`}>
                                                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                                                        <div>
                                                            <p className="text-sm font-bold text-slate-900">{fb.author}</p>
                                                            <p className="text-xs text-slate-400">{fb.time}</p>
                                                        </div>
                                                        <span className={`self-start sm:self-auto px-2 py-0.5 rounded text-[10px] font-bold uppercase border ${fb.status === 'revision'
                                                            ? 'bg-rose-100 text-rose-700 border-rose-200'
                                                            : fb.status === 'approved'
                                                                ? 'bg-emerald-100 text-emerald-700 border-emerald-200'
                                                                : 'bg-slate-100 text-slate-600 border-slate-200'
                                                            }`}>
                                                            {fb.status === 'revision' ? 'Yêu cầu sửa' : fb.status === 'approved' ? 'Đã duyệt' : 'Đã gửi'}
                                                        </span>
                                                    </div>
                                                    {fb.status === 'revision' ? (
                                                        <div className="bg-rose-50 rounded-lg p-3 text-sm text-slate-700 border border-rose-100">
                                                            <p className="font-medium text-rose-800 mb-1">{fb.content}</p>
                                                            <p>{fb.details}</p>
                                                            {fb.points && (
                                                                <ul className="list-disc list-inside mt-2 ml-1 text-xs space-y-1.5 text-slate-600">
                                                                    {fb.points.map((point, pIdx) => (
                                                                        <li key={pIdx}>{point}</li>
                                                                    ))}
                                                                </ul>
                                                            )}
                                                        </div>
                                                    ) : fb.status === 'sent' ? (
                                                        <div className="text-sm text-slate-500 italic">
                                                            <p>{fb.content}</p>
                                                        </div>
                                                    ) : (
                                                        <div className="bg-slate-50 rounded-lg p-3 text-sm text-slate-600 border border-slate-100">
                                                            <p>{fb.content}</p>
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                                <div className="p-4 border-t border-slate-100 bg-slate-50 rounded-b-xl">
                                    <button className="w-full flex items-center justify-center gap-2 bg-primary hover:bg-primary/90 text-white py-3 rounded-lg font-bold shadow-md hover:shadow-lg transition-all text-sm transform active:scale-[0.98]">
                                        <span className="material-symbols-outlined text-[20px]">edit_note</span>
                                        <span>Cập nhật nội dung mới</span>
                                    </button>
                                    <p className="text-xs text-rose-600 text-center mt-3 flex items-center justify-center gap-1">
                                        <span className="material-symbols-outlined text-[14px]">warning</span>
                                        Hạn chót phản hồi: 15/10/2023
                                    </p>
                                </div>
                            </div>
                        </motion.div>
                    </div>
                </motion.div>
            </div>
        </>
    )
}
