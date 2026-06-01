import { useState, useRef, useEffect } from 'react'
import { useAuth } from '@/contexts/AuthContext'

const roleConfig: Record<string, { label: string; icon: string }> = {
    mentor: { label: 'Giảng viên hướng dẫn', icon: 'school' },
    evaluator: { label: 'Người thẩm định', icon: 'fact_check' },
    admin: { label: 'Quản trị viên', icon: 'admin_panel_settings' },
    student: { label: 'Sinh viên', icon: 'person' },
    departmenthead: { label: 'Chủ nhiệm bộ môn', icon: 'supervisor_account' },
}

export function RoleSwitcher({ expanded }: { expanded: boolean }) {
    const { user, activeRole, switchRole } = useAuth()
    const [isOpen, setIsOpen] = useState(false)
    const dropdownRef = useRef<HTMLDivElement>(null)

    // Close dropdown when clicking outside
    useEffect(() => {
        function handleClickOutside(event: MouseEvent) {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
                setIsOpen(false)
            }
        }
        document.addEventListener('mousedown', handleClickOutside)
        return () => document.removeEventListener('mousedown', handleClickOutside)
    }, [])

    // Only show if user has multiple roles
    if (!user || !user.roles || user.roles.length <= 1) return null

    const currentRole = activeRole || user.role
    const currentConfig = roleConfig[currentRole] || roleConfig.mentor
    const otherRoles = user.roles.filter(r => r !== currentRole)

    return (
        <div ref={dropdownRef} className="relative mb-2">
            {/* Trigger Button */}
            <button
                onClick={() => setIsOpen(!isOpen)}
                title={!expanded ? 'Chuyển vai trò' : undefined}
                className={`flex items-center gap-3 w-full ${expanded ? 'px-4' : 'justify-center px-0'} py-3 rounded-lg font-medium transition-all duration-300 group
                    ${isOpen
                        ? 'bg-primary/10 text-primary'
                        : 'text-[#58698d] hover:bg-gray-50 hover:text-primary'
                    }`}
            >
                <span className="material-symbols-outlined shrink-0">swap_horiz</span>
                <span
                    className={`text-sm whitespace-nowrap flex-1 text-left ${expanded ? 'inline' : 'hidden'}`}
                >
                    {currentConfig.label}
                </span>
                {expanded && (
                    <span className={`material-symbols-outlined text-[16px] shrink-0 transition-transform duration-200 ${isOpen ? 'rotate-180' : ''}`}>
                        expand_more
                    </span>
                )}
            </button>

            {/* Dropdown Menu */}
            {isOpen && (
                <div
                    className={`absolute z-50 bg-white rounded-xl shadow-lg border border-[#e9ecf1] py-1.5 overflow-hidden
                        ${expanded
                            ? 'bottom-full left-0 right-0 mb-1'
                            : 'left-full bottom-0 ml-2 min-w-[200px]'
                        }`}
                    style={{
                        animation: 'fadeSlideIn 0.15s ease-out',
                    }}
                >
                    {/* Current role indicator */}
                    <div className="px-3 py-2 text-xs font-semibold text-[#58698d] uppercase tracking-wider border-b border-[#e9ecf1]">
                        Vai trò hiện tại
                    </div>
                    <div className="flex items-center gap-2.5 px-3 py-2.5 bg-primary/5 text-primary">
                        <span className="material-symbols-outlined text-[18px] fill-1">{currentConfig.icon}</span>
                        <span className="text-sm font-semibold">{currentConfig.label}</span>
                        <span className="ml-auto material-symbols-outlined text-[16px]">check_circle</span>
                    </div>

                    {/* Divider */}
                    <div className="px-3 py-2 text-xs font-semibold text-[#58698d] uppercase tracking-wider border-t border-[#e9ecf1]">
                        Chuyển sang
                    </div>

                    {/* Other roles */}
                    {otherRoles.map(role => {
                        const config = roleConfig[role]
                        if (!config) return null
                        return (
                            <button
                                key={role}
                                onClick={() => {
                                    switchRole(role)
                                    setIsOpen(false)
                                }}
                                className="flex items-center gap-2.5 px-3 py-2.5 w-full text-left text-[#58698d] hover:bg-gray-50 hover:text-primary transition-colors duration-150 group"
                            >
                                <span className="material-symbols-outlined text-[18px] group-hover:fill-1">{config.icon}</span>
                                <span className="text-sm font-medium">{config.label}</span>
                                <span className="ml-auto material-symbols-outlined text-[14px] opacity-0 group-hover:opacity-100 transition-opacity">
                                    arrow_forward
                                </span>
                            </button>
                        )
                    })}
                </div>
            )}

            {/* Inline animation keyframes */}
            <style>{`
                @keyframes fadeSlideIn {
                    from {
                        opacity: 0;
                        transform: translateY(4px);
                    }
                    to {
                        opacity: 1;
                        transform: translateY(0);
                    }
                }
            `}</style>
        </div>
    )
}
