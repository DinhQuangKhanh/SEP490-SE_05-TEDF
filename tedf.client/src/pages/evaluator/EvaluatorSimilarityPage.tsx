import { useState, useRef, useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { useNavigate } from 'react-router-dom'

// Mock similarity data for the two PDFs
const similaritySections = [
    {
        id: 'name',
        title: 'Tên đề tài (Capstone Project Name)',
        similarity: 78,
        level: 'high',
        leftContent: {
            english: 'LocalHub - Community Connecting Platform',
            vietnamese: 'LocalHub - Nền tảng kết nối cộng đồng',
            abbreviation: 'LocalHub'
        },
        rightContent: {
            english: 'BusDN - Bus Management System Da Nang',
            vietnamese: 'BusDN - Ứng dụng quản lý xe buýt Đà Nẵng',
            abbreviation: 'BusDN'
        },
        explanation: 'Cả hai đều là nền tảng ứng dụng mobile/web với cấu trúc tên tương tự: Viết tắt + Mô tả chức năng'
    },
    {
        id: 'context',
        title: 'Bối cảnh & Giới thiệu (Context / Brief Introduction)',
        similarity: 65,
        level: 'medium',
        leftContent: {
            text: 'Trong bối cảnh đô thị hóa nhanh chóng, nhu cầu kết nối cộng đồng địa phương ngày càng tăng. LocalHub ra đời nhằm tạo cầu nối giữa người dân trong cùng khu vực, giúp chia sẻ thông tin, sự kiện và dịch vụ địa phương.',
            objective: 'Xây dựng nền tảng kết nối cư dân trong cùng khu vực để chia sẻ thông tin và hỗ trợ lẫn nhau.'
        },
        rightContent: {
            text: 'Với sự phát triển của thành phố Đà Nẵng, nhu cầu di chuyển bằng xe buýt công cộng ngày càng tăng. BusDN được phát triển để giúp người dân dễ dàng tra cứu lộ trình, thời gian xe buýt và thanh toán điện tử.',
            objective: 'Xây dựng ứng dụng hỗ trợ người dân tra cứu và sử dụng dịch vụ xe buýt công cộng hiệu quả hơn.'
        },
        explanation: 'Cấu trúc mô tả bối cảnh tương tự: Nêu vấn đề đô thị → Giải pháp công nghệ → Mục tiêu hỗ trợ cộng đồng'
    },
    {
        id: 'features',
        title: 'Tính năng dự kiến (Expected Features)',
        similarity: 42,
        level: 'medium',
        leftContent: {
            features: [
                'Đăng ký/Đăng nhập tài khoản',
                'Bản đồ hiển thị sự kiện địa phương',
                'Đăng tin/thông báo cộng đồng',
                'Chat nhóm theo khu vực',
                'Đánh giá dịch vụ địa phương'
            ]
        },
        rightContent: {
            features: [
                'Đăng ký/Đăng nhập tài khoản',
                'Bản đồ hiển thị tuyến xe buýt',
                'Tra cứu lộ trình và thời gian',
                'Thanh toán vé điện tử',
                'Đánh giá chất lượng dịch vụ'
            ]
        },
        explanation: 'Có 2 tính năng giống nhau: Hệ thống đăng nhập và tính năng đánh giá. Cả hai đều sử dụng bản đồ nhưng mục đích khác nhau.'
    }
]

// Calculate overall similarity
const overallSimilarity = Math.round(
    similaritySections.reduce((sum, s) => sum + s.similarity, 0) / similaritySections.length
)

export function EvaluatorSimilarityPage() {
    const navigate = useNavigate()
    const [syncScroll, setSyncScroll] = useState(true)
    const [showHighlights, setShowHighlights] = useState(true)
    const [selectedSection, setSelectedSection] = useState<string | null>(null)
    const [hoveredSection, setHoveredSection] = useState<string | null>(null)
    const [tooltipPosition, setTooltipPosition] = useState({ x: 0, y: 0 })
    const [leftPage] = useState(1)
    const [rightPage] = useState(1)
    const [leftNumPages] = useState<number>(24)
    const [rightNumPages] = useState<number>(28)
    const [leftZoom, setLeftZoom] = useState(1)
    const [rightZoom, setRightZoom] = useState(1)
    const leftContainerRef = useRef<HTMLDivElement>(null)
    const rightContainerRef = useRef<HTMLDivElement>(null)

    // Sync scroll between two panels
    useEffect(() => {
        if (!syncScroll) return

        const leftContainer = leftContainerRef.current
        const rightContainer = rightContainerRef.current

        if (!leftContainer || !rightContainer) return

        const handleLeftScroll = () => {
            if (syncScroll) {
                const scrollRatio = leftContainer.scrollTop / (leftContainer.scrollHeight - leftContainer.clientHeight)
                rightContainer.scrollTop = scrollRatio * (rightContainer.scrollHeight - rightContainer.clientHeight)
            }
        }

        const handleRightScroll = () => {
            if (syncScroll) {
                const scrollRatio = rightContainer.scrollTop / (rightContainer.scrollHeight - rightContainer.clientHeight)
                leftContainer.scrollTop = scrollRatio * (leftContainer.scrollHeight - leftContainer.clientHeight)
            }
        }

        leftContainer.addEventListener('scroll', handleLeftScroll)
        rightContainer.addEventListener('scroll', handleRightScroll)

        return () => {
            leftContainer.removeEventListener('scroll', handleLeftScroll)
            rightContainer.removeEventListener('scroll', handleRightScroll)
        }
    }, [syncScroll])

    const handleSectionClick = (sectionId: string) => {
        setSelectedSection(sectionId)
        // Auto scroll both panels to show the section
        const section = similaritySections.find(s => s.id === sectionId)
        if (section) {
            // Simulate scrolling to section
            const leftContainer = leftContainerRef.current
            const rightContainer = rightContainerRef.current
            if (leftContainer && rightContainer) {
                const scrollTarget = sectionId === 'name' ? 100 : sectionId === 'context' ? 300 : 500
                leftContainer.scrollTo({ top: scrollTarget, behavior: 'smooth' })
                rightContainer.scrollTo({ top: scrollTarget, behavior: 'smooth' })
            }
        }
    }

    const handleSectionHover = (sectionId: string | null, event?: React.MouseEvent) => {
        setHoveredSection(sectionId)
        if (event && sectionId) {
            setTooltipPosition({ x: event.clientX, y: event.clientY })
        }
    }

    const getSimilarityColor = (level: string) => {
        switch (level) {
            case 'high': return { bg: 'bg-red-100', border: 'border-red-300', text: 'text-red-700', badge: 'bg-red-500' }
            case 'medium': return { bg: 'bg-amber-100', border: 'border-amber-300', text: 'text-amber-700', badge: 'bg-amber-500' }
            default: return { bg: 'bg-green-100', border: 'border-green-300', text: 'text-green-700', badge: 'bg-green-500' }
        }
    }

    const getOverallLevel = () => {
        if (overallSimilarity >= 70) return 'high'
        if (overallSimilarity >= 30) return 'medium'
        return 'low'
    }

    return (
        <div className="flex flex-col h-full bg-slate-100">
            {/* Header */}
            <motion.header initial={{ opacity: 0, y: -20 }} animate={{ opacity: 1, y: 0 }} className="bg-white border-b border-gray-200 px-6 py-4 shrink-0 shadow-sm z-10">
                <div className="max-w-full mx-auto flex items-center justify-between">
                    <div className="flex items-center gap-4">
                        <button onClick={() => navigate(-1)} className="size-10 rounded-xl border border-gray-200 flex items-center justify-center hover:bg-gray-50 transition-colors">
                            <span className="material-symbols-outlined text-slate-500">arrow_back</span>
                        </button>
                        <div>
                            <h1 className="text-lg font-bold text-slate-900 flex items-center gap-2">
                                <span className="material-symbols-outlined text-primary">compare</span>
                                Kiểm tra trùng lặp ngữ nghĩa
                            </h1>
                            <p className="text-xs text-slate-500">So sánh độ tương đồng giữa hai đề tài Capstone</p>
                        </div>
                    </div>
                    <div className="flex items-center gap-3">
                        <button
                            onClick={() => setSyncScroll(!syncScroll)}
                            className={`flex items-center gap-2 px-3 py-2 rounded-lg border transition-colors ${syncScroll ? 'bg-amber-50 border-amber-200' : 'bg-gray-50 border-gray-200'}`}
                        >
                            <span className={`material-symbols-outlined text-[18px] ${syncScroll ? 'text-amber-600' : 'text-gray-400'}`}>sync</span>
                            <span className={`text-sm font-medium ${syncScroll ? 'text-amber-700' : 'text-gray-600'}`}>
                                Đồng bộ cuộn: <span className="font-bold">{syncScroll ? 'BẬT' : 'TẮT'}</span>
                            </span>
                        </button>
                        <button
                            onClick={() => setShowHighlights(!showHighlights)}
                            className={`h-10 px-4 rounded-lg border text-sm font-semibold transition-colors flex items-center gap-2 ${showHighlights ? 'border-primary bg-primary/10 text-primary' : 'border-gray-200 bg-white text-slate-700 hover:bg-gray-50'}`}
                        >
                            <span className="material-symbols-outlined text-[18px]">{showHighlights ? 'visibility' : 'visibility_off'}</span>
                            {showHighlights ? 'Ẩn đánh dấu' : 'Hiện đánh dấu'}
                        </button>
                    </div>
                </div>
            </motion.header>

            {/* Summary Bar */}
            <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.1 }} className="bg-white border-b border-gray-200 px-6 py-3">
                <div className="max-w-full mx-auto flex items-center justify-between">
                    <div className="flex items-center gap-6">
                        <div className="flex items-center gap-3">
                            <div className="relative size-12">
                                <svg className="size-full -rotate-90" viewBox="0 0 36 36">
                                    <path className="text-gray-200" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" fill="none" stroke="currentColor" strokeWidth="4" />
                                    <path className={getOverallLevel() === 'high' ? 'text-red-500' : getOverallLevel() === 'medium' ? 'text-amber-500' : 'text-green-500'} d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" fill="none" stroke="currentColor" strokeDasharray={`${overallSimilarity}, 100`} strokeLinecap="round" strokeWidth="4" />
                                </svg>
                                <div className="absolute inset-0 flex items-center justify-center"><span className="text-sm font-bold text-slate-900">{overallSimilarity}%</span></div>
                            </div>
                            <div>
                                <p className="text-sm font-bold text-slate-900">Tổng độ tương đồng</p>
                                <p className={`text-xs font-medium ${getOverallLevel() === 'high' ? 'text-red-600' : getOverallLevel() === 'medium' ? 'text-amber-600' : 'text-green-600'}`}>
                                    {getOverallLevel() === 'high' ? 'Mức cao - Cần xem xét' : getOverallLevel() === 'medium' ? 'Mức trung bình' : 'Mức thấp - Đạt yêu cầu'}
                                </p>
                            </div>
                        </div>
                        <div className="h-8 w-px bg-gray-200" />
                        <div className="flex items-center gap-4">
                            <div className="flex items-center gap-2"><div className="size-3 rounded bg-red-500" /><span className="text-xs text-slate-600">Cao (&gt;70%)</span></div>
                            <div className="flex items-center gap-2"><div className="size-3 rounded bg-amber-500" /><span className="text-xs text-slate-600">TB (30-70%)</span></div>
                            <div className="flex items-center gap-2"><div className="size-3 rounded bg-green-500" /><span className="text-xs text-slate-600">Thấp (&lt;30%)</span></div>
                        </div>
                    </div>
                    <div className="flex items-center gap-2">
                        <span className="text-xs text-slate-500">Phân tích theo:</span>
                        <select className="h-9 px-3 rounded-lg border border-gray-200 text-sm font-medium text-slate-700 bg-white">
                            <option>Tất cả phần</option>
                            <option>Chỉ phần có độ tương đồng cao</option>
                        </select>
                    </div>
                </div>
            </motion.div>

            {/* Main Content - PDF Comparison Area */}
            <div className="flex-1 flex overflow-hidden p-4 gap-4">
                {/* Left PDF Panel */}
                <motion.div initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: 0.2 }} className="flex-1 bg-white rounded-xl border border-gray-200 shadow-sm flex flex-col overflow-hidden">
                    <div className="bg-primary/5 border-b border-primary/20 px-4 py-3 flex items-center justify-between shrink-0">
                        <div className="flex items-center gap-3">
                            <div className="size-8 rounded-lg bg-primary text-white flex items-center justify-center text-sm font-bold">A</div>
                            <div>
                                <p className="text-sm font-bold text-slate-900">LocalHub - Nền tảng kết nối cộng đồng</p>
                                <p className="text-xs text-slate-500">SP26_LocalHub-HanhNT54.pdf</p>
                            </div>
                        </div>
                        <div className="flex items-center gap-2">
                            <button onClick={() => setLeftZoom(z => Math.max(0.5, z - 0.1))} className="size-7 rounded hover:bg-gray-100 flex items-center justify-center"><span className="material-symbols-outlined text-slate-500 text-[18px]">remove</span></button>
                            <span className="text-xs font-medium text-slate-600 w-12 text-center">{Math.round(leftZoom * 100)}%</span>
                            <button onClick={() => setLeftZoom(z => Math.min(2, z + 0.1))} className="size-7 rounded hover:bg-gray-100 flex items-center justify-center"><span className="material-symbols-outlined text-slate-500 text-[18px]">add</span></button>
                            <div className="w-px h-4 bg-gray-300 mx-1" />
                            <span className="text-xs text-slate-500">Trang {leftPage}/{leftNumPages || '?'}</span>
                        </div>
                    </div>
                    <div ref={leftContainerRef} className="flex-1 bg-gray-100 overflow-auto p-4">
                        {/* Simulated PDF Content with Highlights */}
                        <div className="bg-white rounded-lg shadow-sm p-8 max-w-2xl mx-auto space-y-6">
                            {/* Header */}
                            <div className="text-center border-b pb-6">
                                <p className="text-xs text-gray-500 mb-2">ĐỀ XUẤT ĐỀ TÀI CAPSTONE</p>
                                <h2 className="text-xl font-bold text-gray-900">LOCALHUB - NỀN TẢNG KẾT NỐI CỘNG ĐỒNG</h2>
                                <p className="text-sm text-gray-600 mt-1">LocalHub - Community Connecting Platform</p>
                            </div>

                            {/* Section 1: Project Name */}
                            {showHighlights && (
                                <div
                                    className={`p-4 rounded-lg border-2 cursor-pointer transition-all ${getSimilarityColor('high').bg} ${getSimilarityColor('high').border} ${selectedSection === 'name' ? 'ring-2 ring-red-400' : ''}`}
                                    onClick={() => handleSectionClick('name')}
                                    onMouseEnter={(e) => handleSectionHover('name', e)}
                                    onMouseLeave={() => handleSectionHover(null)}
                                >
                                    <div className="flex items-center justify-between mb-2">
                                        <span className="text-xs font-bold text-red-700 uppercase">1. Tên đề tài</span>
                                        <span className="text-xs font-bold text-white bg-red-500 px-2 py-0.5 rounded">78% tương đồng</span>
                                    </div>
                                    <div className="space-y-2 text-sm">
                                        <p><strong>Tiếng Anh:</strong> LocalHub - Community Connecting Platform</p>
                                        <p><strong>Tiếng Việt:</strong> LocalHub - Nền tảng kết nối cộng đồng</p>
                                        <p><strong>Viết tắt:</strong> LocalHub</p>
                                    </div>
                                </div>
                            )}

                            {/* Section 2: Context */}
                            {showHighlights && (
                                <div
                                    className={`p-4 rounded-lg border-2 cursor-pointer transition-all ${getSimilarityColor('medium').bg} ${getSimilarityColor('medium').border} ${selectedSection === 'context' ? 'ring-2 ring-amber-400' : ''}`}
                                    onClick={() => handleSectionClick('context')}
                                    onMouseEnter={(e) => handleSectionHover('context', e)}
                                    onMouseLeave={() => handleSectionHover(null)}
                                >
                                    <div className="flex items-center justify-between mb-2">
                                        <span className="text-xs font-bold text-amber-700 uppercase">2. Bối cảnh & Mục tiêu</span>
                                        <span className="text-xs font-bold text-white bg-amber-500 px-2 py-0.5 rounded">65% tương đồng</span>
                                    </div>
                                    <div className="space-y-2 text-sm text-gray-700">
                                        <p>Trong bối cảnh đô thị hóa nhanh chóng, nhu cầu kết nối cộng đồng địa phương ngày càng tăng. LocalHub ra đời nhằm tạo cầu nối giữa người dân trong cùng khu vực, giúp chia sẻ thông tin, sự kiện và dịch vụ địa phương.</p>
                                        <p className="font-medium">Mục tiêu: Xây dựng nền tảng kết nối cư dân trong cùng khu vực để chia sẻ thông tin và hỗ trợ lẫn nhau.</p>
                                    </div>
                                </div>
                            )}

                            {/* Section 3: Features */}
                            {showHighlights && (
                                <div
                                    className={`p-4 rounded-lg border-2 cursor-pointer transition-all ${getSimilarityColor('medium').bg} ${getSimilarityColor('medium').border} ${selectedSection === 'features' ? 'ring-2 ring-amber-400' : ''}`}
                                    onClick={() => handleSectionClick('features')}
                                    onMouseEnter={(e) => handleSectionHover('features', e)}
                                    onMouseLeave={() => handleSectionHover(null)}
                                >
                                    <div className="flex items-center justify-between mb-2">
                                        <span className="text-xs font-bold text-amber-700 uppercase">3. Tính năng dự kiến</span>
                                        <span className="text-xs font-bold text-white bg-amber-500 px-2 py-0.5 rounded">42% tương đồng</span>
                                    </div>
                                    <ul className="space-y-1 text-sm text-gray-700 list-disc pl-5">
                                        <li className="text-red-700 font-medium">Đăng ký/Đăng nhập tài khoản ⚠️</li>
                                        <li>Bản đồ hiển thị sự kiện địa phương</li>
                                        <li>Đăng tin/thông báo cộng đồng</li>
                                        <li>Chat nhóm theo khu vực</li>
                                        <li className="text-red-700 font-medium">Đánh giá dịch vụ địa phương ⚠️</li>
                                    </ul>
                                </div>
                            )}

                            {/* Non-highlighted content */}
                            <div className="space-y-4 text-sm text-gray-700">
                                <h3 className="font-bold text-gray-900">4. Công nghệ sử dụng</h3>
                                <ul className="list-disc pl-5 space-y-1">
                                    <li>Frontend: React Native</li>
                                    <li>Backend: Node.js, Express</li>
                                    <li>Database: MongoDB</li>
                                    <li>Map: Google Maps API</li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </motion.div>

                {/* Right PDF Panel */}
                <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: 0.3 }} className="flex-1 bg-white rounded-xl border border-gray-200 shadow-sm flex flex-col overflow-hidden">
                    <div className="bg-amber-50 border-b border-amber-200 px-4 py-3 flex items-center justify-between shrink-0">
                        <div className="flex items-center gap-3">
                            <div className="size-8 rounded-lg bg-amber-500 text-white flex items-center justify-center text-sm font-bold">B</div>
                            <div>
                                <p className="text-sm font-bold text-slate-900">BusDN - Ứng dụng quản lý xe buýt Đà Nẵng</p>
                                <p className="text-xs text-slate-500">SP26_BusDN-HanhNT54.pdf</p>
                            </div>
                        </div>
                        <div className="flex items-center gap-2">
                            <button onClick={() => setRightZoom(z => Math.max(0.5, z - 0.1))} className="size-7 rounded hover:bg-gray-100 flex items-center justify-center"><span className="material-symbols-outlined text-slate-500 text-[18px]">remove</span></button>
                            <span className="text-xs font-medium text-slate-600 w-12 text-center">{Math.round(rightZoom * 100)}%</span>
                            <button onClick={() => setRightZoom(z => Math.min(2, z + 0.1))} className="size-7 rounded hover:bg-gray-100 flex items-center justify-center"><span className="material-symbols-outlined text-slate-500 text-[18px]">add</span></button>
                            <div className="w-px h-4 bg-gray-300 mx-1" />
                            <span className="text-xs text-slate-500">Trang {rightPage}/{rightNumPages || '?'}</span>
                        </div>
                    </div>
                    <div ref={rightContainerRef} className="flex-1 bg-gray-100 overflow-auto p-4">
                        {/* Simulated PDF Content with Highlights */}
                        <div className="bg-white rounded-lg shadow-sm p-8 max-w-2xl mx-auto space-y-6">
                            {/* Header */}
                            <div className="text-center border-b pb-6">
                                <p className="text-xs text-gray-500 mb-2">ĐỀ XUẤT ĐỀ TÀI CAPSTONE</p>
                                <h2 className="text-xl font-bold text-gray-900">BUSDN - ỨNG DỤNG QUẢN LÝ XE BUÝT ĐÀ NẴNG</h2>
                                <p className="text-sm text-gray-600 mt-1">BusDN - Bus Management System Da Nang</p>
                            </div>

                            {/* Section 1: Project Name */}
                            {showHighlights && (
                                <div
                                    className={`p-4 rounded-lg border-2 cursor-pointer transition-all ${getSimilarityColor('high').bg} ${getSimilarityColor('high').border} ${selectedSection === 'name' ? 'ring-2 ring-red-400' : ''}`}
                                    onClick={() => handleSectionClick('name')}
                                    onMouseEnter={(e) => handleSectionHover('name', e)}
                                    onMouseLeave={() => handleSectionHover(null)}
                                >
                                    <div className="flex items-center justify-between mb-2">
                                        <span className="text-xs font-bold text-red-700 uppercase">1. Tên đề tài</span>
                                        <span className="text-xs font-bold text-white bg-red-500 px-2 py-0.5 rounded">78% tương đồng</span>
                                    </div>
                                    <div className="space-y-2 text-sm">
                                        <p><strong>Tiếng Anh:</strong> BusDN - Bus Management System Da Nang</p>
                                        <p><strong>Tiếng Việt:</strong> BusDN - Ứng dụng quản lý xe buýt Đà Nẵng</p>
                                        <p><strong>Viết tắt:</strong> BusDN</p>
                                    </div>
                                </div>
                            )}

                            {/* Section 2: Context */}
                            {showHighlights && (
                                <div
                                    className={`p-4 rounded-lg border-2 cursor-pointer transition-all ${getSimilarityColor('medium').bg} ${getSimilarityColor('medium').border} ${selectedSection === 'context' ? 'ring-2 ring-amber-400' : ''}`}
                                    onClick={() => handleSectionClick('context')}
                                    onMouseEnter={(e) => handleSectionHover('context', e)}
                                    onMouseLeave={() => handleSectionHover(null)}
                                >
                                    <div className="flex items-center justify-between mb-2">
                                        <span className="text-xs font-bold text-amber-700 uppercase">2. Bối cảnh & Mục tiêu</span>
                                        <span className="text-xs font-bold text-white bg-amber-500 px-2 py-0.5 rounded">65% tương đồng</span>
                                    </div>
                                    <div className="space-y-2 text-sm text-gray-700">
                                        <p>Với sự phát triển của thành phố Đà Nẵng, nhu cầu di chuyển bằng xe buýt công cộng ngày càng tăng. BusDN được phát triển để giúp người dân dễ dàng tra cứu lộ trình, thời gian xe buýt và thanh toán điện tử.</p>
                                        <p className="font-medium">Mục tiêu: Xây dựng ứng dụng hỗ trợ người dân tra cứu và sử dụng dịch vụ xe buýt công cộng hiệu quả hơn.</p>
                                    </div>
                                </div>
                            )}

                            {/* Section 3: Features */}
                            {showHighlights && (
                                <div
                                    className={`p-4 rounded-lg border-2 cursor-pointer transition-all ${getSimilarityColor('medium').bg} ${getSimilarityColor('medium').border} ${selectedSection === 'features' ? 'ring-2 ring-amber-400' : ''}`}
                                    onClick={() => handleSectionClick('features')}
                                    onMouseEnter={(e) => handleSectionHover('features', e)}
                                    onMouseLeave={() => handleSectionHover(null)}
                                >
                                    <div className="flex items-center justify-between mb-2">
                                        <span className="text-xs font-bold text-amber-700 uppercase">3. Tính năng dự kiến</span>
                                        <span className="text-xs font-bold text-white bg-amber-500 px-2 py-0.5 rounded">42% tương đồng</span>
                                    </div>
                                    <ul className="space-y-1 text-sm text-gray-700 list-disc pl-5">
                                        <li className="text-red-700 font-medium">Đăng ký/Đăng nhập tài khoản ⚠️</li>
                                        <li>Bản đồ hiển thị tuyến xe buýt</li>
                                        <li>Tra cứu lộ trình và thời gian</li>
                                        <li>Thanh toán vé điện tử</li>
                                        <li className="text-red-700 font-medium">Đánh giá chất lượng dịch vụ ⚠️</li>
                                    </ul>
                                </div>
                            )}

                            {/* Non-highlighted content */}
                            <div className="space-y-4 text-sm text-gray-700">
                                <h3 className="font-bold text-gray-900">4. Công nghệ sử dụng</h3>
                                <ul className="list-disc pl-5 space-y-1">
                                    <li>Frontend: Flutter</li>
                                    <li>Backend: Firebase, Cloud Functions</li>
                                    <li>Database: Firestore</li>
                                    <li>Map: Mapbox API</li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </motion.div>
            </div>

            {/* Similarity Section List */}
            <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.4 }} className="bg-white border-t border-gray-200 px-6 py-4 shrink-0">
                <div className="max-w-full mx-auto">
                    <h3 className="text-sm font-bold text-slate-900 mb-3 flex items-center gap-2">
                        <span className="material-symbols-outlined text-[18px] text-primary">format_list_bulleted</span>
                        Các phần đã phân tích - Click để xem chi tiết
                    </h3>
                    <div className="flex gap-3 overflow-x-auto pb-2">
                        {similaritySections.map((section) => {
                            const colors = getSimilarityColor(section.level)
                            return (
                                <button
                                    key={section.id}
                                    onClick={() => handleSectionClick(section.id)}
                                    className={`shrink-0 px-4 py-3 rounded-xl border-2 transition-all hover:shadow-md ${colors.bg} ${colors.border} ${selectedSection === section.id ? 'ring-2 ring-offset-1 ring-slate-400' : ''}`}
                                >
                                    <p className="text-sm font-bold text-slate-900">{section.title}</p>
                                    <p className={`text-xs font-bold mt-1 ${colors.text}`}>{section.similarity}% tương đồng</p>
                                </button>
                            )
                        })}
                    </div>
                </div>
            </motion.div>

            {/* Tooltip */}
            <AnimatePresence>
                {hoveredSection && (
                    <motion.div
                        initial={{ opacity: 0, scale: 0.9 }}
                        animate={{ opacity: 1, scale: 1 }}
                        exit={{ opacity: 0, scale: 0.9 }}
                        className="fixed z-50 bg-slate-900 text-white p-4 rounded-xl shadow-2xl max-w-sm"
                        style={{ left: tooltipPosition.x + 10, top: tooltipPosition.y + 10 }}
                    >
                        {(() => {
                            const section = similaritySections.find(s => s.id === hoveredSection)
                            if (!section) return null
                            const colors = getSimilarityColor(section.level)
                            return (
                                <>
                                    <div className="flex items-center justify-between mb-2">
                                        <span className="text-xs font-bold text-slate-400 uppercase">{section.title}</span>
                                        <span className={`text-xs font-bold px-2 py-0.5 rounded ${colors.badge} text-white`}>{section.similarity}%</span>
                                    </div>
                                    <p className="text-sm text-slate-300">{section.explanation}</p>
                                </>
                            )
                        })()}
                    </motion.div>
                )}
            </AnimatePresence>
        </div>
    )
}
