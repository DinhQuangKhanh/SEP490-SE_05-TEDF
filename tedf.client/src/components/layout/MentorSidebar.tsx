import { useState } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import { motion } from 'framer-motion'
import { useAuth } from '@/contexts/AuthContext'
import { RoleSwitcher } from './RoleSwitcher'
import { useUnreadSupportCount } from '@/hooks/useUnreadSupportCount'

const navItems = [
    { label: 'Dashboard', icon: 'dashboard', path: '/mentor' },
    { label: 'Nhóm của tôi', icon: 'groups', path: '/mentor/groups' },
    { label: 'Danh sách đề tài', icon: 'folder_open', path: '/mentor/topics' },
    { label: 'Kho đề tài', icon: 'inventory_2', path: '/mentor/topic-pools' },
    { label: 'Lịch trình', icon: 'calendar_month', path: '/mentor/schedule' },
]

const systemItems = [
    { label: 'Cài đặt', icon: 'settings', path: '/mentor/settings' },
    { label: 'Hỗ trợ', icon: 'support_agent', path: '/mentor/support' },
]

export function MentorSidebar() {
    const location = useLocation()
    const { user, logout } = useAuth()
    const unreadSupportCount = useUnreadSupportCount()
    const [isHovered, setIsHovered] = useState(false)

    const isActive = (path: string) => {
        if (path === '/mentor') {
            return location.pathname === '/mentor'
        }
        return location.pathname.startsWith(path)
    }

    return (
        <motion.aside
            initial={{ x: -64 }}
            animate={{ x: 0 }}
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            onMouseEnter={() => setIsHovered(true)}
            onMouseLeave={() => setIsHovered(false)}
            className={`${isHovered ? 'w-64' : 'w-[72px]'} h-full bg-white border-r border-[#e9ecf1] flex flex-col shrink-0 z-20 transition-all duration-300 ease-in-out overflow-hidden`}
        >
            {/* Logo */}
            <div className={`p-6 flex items-center ${isHovered ? 'gap-3' : 'justify-center'}`}>
                <div className="bg-primary/10 p-2 rounded-lg text-primary shrink-0">
                    <span className="material-symbols-outlined fill-1">school</span>
                </div>
                <h1 className={`text-primary text-xl font-bold tracking-tight whitespace-nowrap transition-all duration-300 ${isHovered ? 'opacity-100 w-auto' : 'opacity-0 w-0 overflow-hidden'}`}>UniThesis</h1>
            </div>

            {/* Navigation */}
            <nav className={`flex-1 ${isHovered ? 'px-4' : 'px-2'} flex flex-col gap-1.5 overflow-y-auto transition-all duration-300`}>
                {navItems.map((item) => (
                    <NavItem key={item.path} {...item} active={isActive(item.path)} expanded={isHovered} />
                ))}
            </nav>

            {/* Footer */}
            <div className={`p-4 border-t border-[#e9ecf1] ${isHovered ? '' : 'px-2'}`}>
                <RoleSwitcher expanded={isHovered} />
                {systemItems.map((item) => (
                    <NavItem 
                        key={item.path} 
                        {...item} 
                        badge={item.path === '/mentor/support' && unreadSupportCount > 0 ? unreadSupportCount.toString() : undefined}
                        active={isActive(item.path)} 
                        expanded={isHovered} 
                    />
                ))}
                {/* User Profile */}
                <div className={`mt-4 flex items-center ${isHovered ? 'gap-3 px-4' : 'justify-center px-0'} py-2 transition-all duration-300`}>
                    <div
                        className="h-10 w-10 rounded-full bg-gray-200 bg-cover bg-center shrink-0"
                        style={{
                            backgroundImage: user?.avatar ? `url('${user.avatar}')` : undefined,
                        }}
                    />
                    <div className={`flex flex-col overflow-hidden text-left transition-all duration-300 ${isHovered ? 'opacity-100 w-auto' : 'opacity-0 w-0'}`}>
                        <p className="text-sm font-bold text-[#101319] truncate whitespace-nowrap">{user?.name || 'TS. Trần Minh Tuấn'}</p>
                        <p className="text-xs text-[#58698d] truncate whitespace-nowrap">Giảng viên hướng dẫn</p>
                    </div>
                </div>
                {/* Logout Button */}
                <button
                    onClick={logout}
                    title="Đăng xuất"
                    className={`mt-2 flex items-center gap-3 w-full ${isHovered ? 'px-4' : 'justify-center px-0'} py-2.5 text-red-600 hover:bg-red-50 rounded-lg transition-all duration-300 group`}
                >
                    <span className="material-symbols-outlined text-[20px] shrink-0">logout</span>
                    <span className={`text-sm font-medium whitespace-nowrap transition-all duration-300 ${isHovered ? 'opacity-100 w-auto' : 'opacity-0 w-0 overflow-hidden'}`}>Đăng xuất</span>
                </button>
            </div>
        </motion.aside>
    )
}

function NavItem({
    label,
    icon,
    path,
    badge,
    active,
    expanded
}: {
    label: string
    icon: string
    path: string
    badge?: string
    active: boolean
    expanded: boolean
}) {
    return (
        <NavLink
            to={path}
            title={!expanded ? label : undefined}
            className={`flex items-center gap-3 ${expanded ? 'px-4' : 'justify-center px-0'} py-3 rounded-lg font-medium transition-all duration-300 group ${active
                ? 'bg-primary/10 text-primary font-semibold'
                : 'text-[#58698d] hover:bg-gray-50 hover:text-primary'
                }`}
        >
            <span className={`material-symbols-outlined shrink-0 ${active ? 'fill-1' : 'group-hover:fill-1'}`}>{icon}</span>
            <span className={`text-sm whitespace-nowrap transition-all duration-300 ${expanded ? 'opacity-100 w-auto' : 'opacity-0 w-0 overflow-hidden'}`}>{label}</span>
            {badge && expanded && (
                <span className="ml-auto bg-red-100 text-red-600 text-xs font-bold px-2 py-0.5 rounded-full">
                    {badge}
                </span>
            )}
        </NavLink>
    )
}
