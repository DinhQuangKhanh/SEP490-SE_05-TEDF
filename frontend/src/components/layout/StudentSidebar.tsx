import { useAuth } from '@/contexts/AuthContext'
import { useUnreadSupportCount } from '@/hooks/useUnreadSupportCount'
import { SidebarShell, type SidebarItem } from './SidebarShell'

export function StudentSidebar() {
    const { user } = useAuth()
    const unreadSupportCount = useUnreadSupportCount()

    const navItems: SidebarItem[] = [
        { label: 'Trang chủ', icon: 'dashboard', path: '/student', exact: true },
        { label: 'Đề tài của tôi', icon: 'book_2', path: '/student/my-topic' },
        { label: 'Kho đề tài đề xuất', icon: 'inventory_2', path: '/student/topics' },
        { label: 'Nhóm', icon: 'group', path: '/student/groups' },
    ]

    const footerItems: SidebarItem[] = [
        {
            label: 'Hỗ trợ',
            icon: 'support_agent',
            path: '/student/support',
            badge: unreadSupportCount > 0 ? unreadSupportCount.toString() : undefined,
        },
    ]

    return (
        <SidebarShell
            navItems={navItems}
            footerItems={footerItems}
            profile={{
                name: user?.name || 'Sinh viên',
                subtitle: 'Sinh viên',
                avatarUrl: user?.avatar,
            }}
        />
    )
}
