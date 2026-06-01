import { motion } from 'framer-motion'
import { NotificationDropdown } from '@/components/layout'

const container = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { staggerChildren: 0.05 } }
}

const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 }
}

const members = [
    { name: 'Trần Hoàng Nam (Trưởng nhóm)', mssv: '20150231 - CNTT-01' },
    { name: 'Lê Thị Thanh Mai', mssv: '20150452 - CNTT-02' },
    { name: 'Nguyễn Minh Quân', mssv: '20150118 - CNTT-01' },
]

const files = [
    { name: 'Bao_cao_de_cuong_v1.pdf', type: 'pdf', size: '1.2 MB', time: '2 ngày trước' },
    { name: 'Phan_tich_yeu_cau_nguoi_dung.docx', type: 'doc', size: '450 KB', time: 'Hôm nay' },
    { name: 'Ban_thao_giao_dien_UI.png', type: 'image', size: '2.8 MB', time: '5 giờ trước' },
]

const messages = [
    { author: 'Mentor Nguyễn Văn A', time: '08:30 - 20/10', content: 'Nhóm nên bổ sung thêm phần phân tích tính khả thi khi tích hợp AI vào mobile app nhé. Hiện tại phần mô tả còn hơi sơ sài.', isMentor: true },
    { author: 'Nam (Trưởng nhóm)', time: '09:15 - 20/10', content: 'Dạ vâng ạ, chúng em đang bổ sung và sẽ nộp bản thảo mới vào chiều nay ạ.', isMentor: false },
]

export function MentorTopicDetailPage() {

    return (
        <>
            {/* Header */}
            <header className="h-16 flex items-center justify-between px-8 bg-slate-800 border-b border-slate-700 flex-shrink-0 z-50 shadow-md">
                <div className="flex items-center gap-2 text-white">
                    <span className="text-slate-400 font-medium text-sm">Nhóm của tôi</span>
                    <span className="material-symbols-outlined text-sm text-slate-500">chevron_right</span>
                    <h2 className="text-lg font-bold">Chi tiết đề tài - Nhóm 01</h2>
                </div>
                <div className="flex items-center gap-3">
                    <button className="flex items-center gap-2 px-4 py-2 bg-slate-700 hover:bg-slate-600 text-white rounded-lg text-sm font-medium transition-colors">
                        <span className="material-symbols-outlined text-[20px]">download</span>
                        <span>Tải xuống tất cả tài liệu</span>
                    </button>
                    <button className="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-sm font-medium transition-colors">
                        <span className="material-symbols-outlined text-[20px]">check_circle</span>
                        <span>Phê duyệt nội dung</span>
                    </button>
                    <div className="w-px h-6 bg-slate-700 mx-2" />
                    <NotificationDropdown role="mentor" isNavy={true} />
                </div>
            </header>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-8 bg-slate-100">
                <motion.div variants={container} initial="hidden" animate="show" className="grid grid-cols-12 gap-8">
                    {/* Left Column - Topic Details */}
                    <motion.div variants={item} className="col-span-12 lg:col-span-8 space-y-6">
                        <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
                            <div className="p-6 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
                                <div className="flex items-center gap-3">
                                    <span className="material-symbols-outlined text-primary text-[28px]">description</span>
                                    <h3 className="text-xl font-bold text-slate-900 tracking-tight">Mô tả chi tiết đề tài</h3>
                                </div>
                                <span className="px-3 py-1 bg-amber-100 text-amber-700 text-xs font-bold rounded-full uppercase">
                                    Đang chờ thẩm định
                                </span>
                            </div>
                            <div className="p-8 space-y-10">
                                <section>
                                    <h4 className="text-sm font-bold text-slate-400 uppercase tracking-widest mb-3">Tên đề tài</h4>
                                    <p className="text-2xl font-bold text-slate-900 leading-tight">
                                        Xây dựng nền tảng học trực tuyến E-Learning tích hợp trí tuệ nhân tạo (AI) hỗ trợ học tập cá nhân hóa
                                    </p>
                                </section>

                                <section>
                                    <div className="flex items-center gap-2 mb-4">
                                        <span className="size-2 rounded-full bg-primary" />
                                        <h4 className="text-base font-bold text-slate-800">1. Mục tiêu đề tài</h4>
                                    </div>
                                    <div className="text-slate-600 space-y-3 leading-relaxed ml-4">
                                        <p>Nghiên cứu và ứng dụng các thuật toán Machine Learning để phân tích hành vi học tập của sinh viên.</p>
                                        <p>Phát triển hệ thống khuyến nghị nội dung học tập thông minh dựa trên lộ trình cá nhân hóa.</p>
                                        <p>Tối ưu hóa trải nghiệm người dùng trên các thiết bị di động và máy tính thông qua giao diện hiện đại.</p>
                                    </div>
                                </section>

                                <section>
                                    <div className="flex items-center gap-2 mb-4">
                                        <span className="size-2 rounded-full bg-primary" />
                                        <h4 className="text-base font-bold text-slate-800">2. Phạm vi thực hiện</h4>
                                    </div>
                                    <div className="text-slate-600 space-y-3 leading-relaxed ml-4">
                                        <p><strong className="text-slate-900">Về công nghệ:</strong> Sử dụng ReactJS cho Frontend, Node.js cho Backend, Python (TensorFlow/PyTorch) cho module AI, và PostgreSQL cho cơ sở dữ liệu.</p>
                                        <p><strong className="text-slate-900">Về nội dung:</strong> Tập trung vào các khóa học thuộc lĩnh vực Công nghệ thông tin và Ngoại ngữ.</p>
                                        <p><strong className="text-slate-900">Đối tượng:</strong> Thử nghiệm trên quy mô 500 sinh viên tại khoa CNTT của trường.</p>
                                    </div>
                                </section>

                                <section>
                                    <div className="flex items-center gap-2 mb-4">
                                        <span className="size-2 rounded-full bg-primary" />
                                        <h4 className="text-base font-bold text-slate-800">3. Kết quả dự kiến</h4>
                                    </div>
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 ml-4">
                                        <div className="p-4 bg-slate-50 border border-slate-200 rounded-lg">
                                            <div className="flex items-center gap-2 mb-2 text-primary">
                                                <span className="material-symbols-outlined text-[20px]">inventory_2</span>
                                                <span className="font-bold text-sm">Sản phẩm phần mềm</span>
                                            </div>
                                            <p className="text-sm text-slate-600">Ứng dụng Web hoàn chỉnh và Ứng dụng Mobile (Android &amp; iOS) có tích hợp Chatbot AI.</p>
                                        </div>
                                        <div className="p-4 bg-slate-50 border border-slate-200 rounded-lg">
                                            <div className="flex items-center gap-2 mb-2 text-primary">
                                                <span className="material-symbols-outlined text-[20px]">article</span>
                                                <span className="font-bold text-sm">Báo cáo khoa học</span>
                                            </div>
                                            <p className="text-sm text-slate-600">Quyển báo cáo chi tiết về quy trình xây dựng hệ thống và kết quả thử nghiệm thực tế.</p>
                                        </div>
                                    </div>
                                </section>
                            </div>
                        </div>
                    </motion.div>

                    {/* Right Column - Sidebar */}
                    <motion.div variants={item} className="col-span-12 lg:col-span-4 space-y-6">
                        {/* Team Info */}
                        <section className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
                            <div className="p-4 border-b border-slate-100 bg-slate-50/50 flex items-center gap-2">
                                <span className="material-symbols-outlined text-slate-500 text-[20px]">groups</span>
                                <h3 className="font-bold text-slate-900 text-sm">Thông tin nhóm thực hiện</h3>
                            </div>
                            <div className="p-4 space-y-4">
                                {members.map((member) => (
                                    <div key={member.mssv} className="flex items-center gap-3 p-3 border border-slate-100 rounded-lg hover:border-primary/30 transition-colors">
                                        <div className="size-10 rounded-full bg-slate-200 flex items-center justify-center text-slate-500 text-xs font-bold">
                                            {member.name.charAt(0)}
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <p className="text-sm font-bold text-slate-900 truncate">{member.name}</p>
                                            <p className="text-xs text-slate-500">MSSV: {member.mssv}</p>
                                        </div>
                                        <span className="material-symbols-outlined text-slate-400 text-[18px]">contact_mail</span>
                                    </div>
                                ))}
                            </div>
                        </section>

                        {/* Files */}
                        <section className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
                            <div className="p-4 border-b border-slate-100 bg-slate-50/50 flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                    <span className="material-symbols-outlined text-slate-500 text-[20px]">folder</span>
                                    <h3 className="font-bold text-slate-900 text-sm">Kho tài liệu của nhóm</h3>
                                </div>
                                <span className="text-[10px] font-bold text-slate-400 uppercase tracking-tight">{files.length} files</span>
                            </div>
                            <div className="p-4 space-y-2">
                                {files.map((file) => (
                                    <div key={file.name} className="flex items-center gap-3 p-2 hover:bg-slate-50 rounded-lg transition-colors cursor-pointer group">
                                        <div className={`size-9 rounded flex items-center justify-center ${file.type === 'pdf' ? 'bg-rose-50 text-rose-600' : file.type === 'doc' ? 'bg-blue-50 text-blue-600' : 'bg-amber-50 text-amber-600'
                                            }`}>
                                            <span className="material-symbols-outlined text-[20px]">{file.type === 'pdf' ? 'picture_as_pdf' : file.type === 'doc' ? 'description' : 'draft'}</span>
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <p className="text-xs font-bold text-slate-800 truncate">{file.name}</p>
                                            <p className="text-[10px] text-slate-400 uppercase font-medium">{file.size} • {file.time}</p>
                                        </div>
                                        <span className="material-symbols-outlined text-slate-300 group-hover:text-primary transition-colors text-[18px]">download</span>
                                    </div>
                                ))}
                            </div>
                            <div className="p-3 bg-slate-50 border-t border-slate-100">
                                <button className="w-full py-2 text-primary hover:text-primary/80 text-xs font-bold transition-colors">
                                    Xem tất cả tài liệu
                                </button>
                            </div>
                        </section>

                        {/* Comments */}
                        <section className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                            <div className="p-4 border-b border-slate-100 bg-slate-50/50 flex items-center gap-2">
                                <span className="material-symbols-outlined text-slate-500 text-[20px]">forum</span>
                                <h3 className="font-bold text-slate-900 text-sm">Nhận xét &amp; Hướng dẫn</h3>
                            </div>
                            <div className="p-4 flex-1 space-y-4 min-h-[200px] max-h-[300px] overflow-y-auto">
                                {messages.map((msg, idx) => (
                                    <div key={idx} className={`flex gap-3 ${!msg.isMentor ? 'flex-row-reverse' : ''}`}>
                                        <div className="size-8 rounded-full bg-slate-200 flex-shrink-0 flex items-center justify-center text-xs font-bold text-slate-500">
                                            {msg.author.charAt(0)}
                                        </div>
                                        <div className={`rounded-2xl p-3 max-w-[90%] ${msg.isMentor ? 'bg-slate-100 rounded-tl-none' : 'bg-primary/10 rounded-tr-none'}`}>
                                            <div className="flex items-center justify-between mb-1 gap-4">
                                                <span className={`text-[11px] font-bold ${msg.isMentor ? 'text-slate-900' : 'text-primary'}`}>{msg.author}</span>
                                                <span className={`text-[10px] ${msg.isMentor ? 'text-slate-400' : 'text-primary/60'}`}>{msg.time}</span>
                                            </div>
                                            <p className="text-xs text-slate-700 leading-relaxed">{msg.content}</p>
                                        </div>
                                    </div>
                                ))}
                            </div>
                            <div className="p-4 border-t border-slate-100 bg-white">
                                <div className="relative">
                                    <textarea
                                        className="w-full border-slate-200 rounded-lg text-sm focus:ring-primary focus:border-primary placeholder:text-slate-400 min-h-[80px] pr-10 resize-none"
                                        placeholder="Để lại lời nhắn hoặc yêu cầu chỉnh sửa cho nhóm..."
                                    />
                                    <button className="absolute bottom-2 right-2 p-1.5 bg-primary hover:bg-primary/90 text-white rounded-md transition-colors">
                                        <span className="material-symbols-outlined text-[18px]">send</span>
                                    </button>
                                </div>
                                <div className="flex items-center gap-3 mt-3">
                                    <button className="flex items-center gap-1 text-[11px] font-bold text-slate-500 hover:text-primary transition-colors">
                                        <span className="material-symbols-outlined text-[16px]">attach_file</span>
                                        Đính kèm tài liệu
                                    </button>
                                    <button className="flex items-center gap-1 text-[11px] font-bold text-rose-500 hover:text-rose-700 transition-colors">
                                        <span className="material-symbols-outlined text-[16px]">report</span>
                                        Yêu cầu sửa gấp
                                    </button>
                                </div>
                            </div>
                        </section>
                    </motion.div>
                </motion.div>
            </div>
        </>
    )
}
