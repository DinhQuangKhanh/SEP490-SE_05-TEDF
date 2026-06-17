import { useAuth } from '@/contexts/AuthContext'
import { useUnreadSupportCount } from '@/hooks/useUnreadSupportCount'
import { SidebarShell, type SidebarItem } from './SidebarShell'

const STUDENT_AVATAR =
    'https://lh3.googleusercontent.com/aida-public/AB6AXuDcmhFbaP0vMcYOP70wqwwwzqaJSKf-3DBianrl7cMsyN3laUMyvlWs8wnYaX1nGPLIGVInAdzQXNsHKrfv82HbPyOEqqiste4qnOBNZlC9pOaZrSLZZg71hleEKDcTJeHR_GYWsO-keITdsHRIzw7R3rcP9y3adyO2PToD2nxURK0Afp67TENb5qrmoqmXYEQBi2m4pco1pHmYWtV4YOH6-TyoYeaerHqpC6lTitLFtQp4Ir5u8J_xlQdQDj7ofOfugeih7FL2vNVY'

export function StudentSidebar() {
    const { user } = useAuth()
    const unreadSupportCount = useUnreadSupportCount()

    const navItems: SidebarItem[] = [
        { label: 'Trang chủ', icon: 'dashboard', path: '/student', exact: true },
        { label: 'Đề tài của tôi', icon: 'book_2', path: '/student/my-topic' },
        { label: 'Kho đề tài đề xuất', icon: 'inventory_2', path: '/student/topics' },
        { label: 'Nhóm', icon: 'group', path: '/student/groups' },
        { label: 'Lịch trình chung', icon: 'calendar_month', path: '/student/schedule' },
        {
            label: 'Hỗ trợ',
            icon: 'support_agent',
            path: '/student/support',
            badge: unreadSupportCount > 0 ? unreadSupportCount.toString() : undefined,
        },
    ]

    const footerItems: SidebarItem[] = [{ label: 'Cài đặt', icon: 'settings', path: '/student/settings' }]

    return (
        <SidebarShell
            navItems={navItems}
            footerItems={footerItems}
            profile={{
                name: user?.name || 'Nguyễn Văn An',
                subtitle: 'K62 - CNTT',
                avatarUrl: STUDENT_AVATAR,
            }}
        />
    )
}
