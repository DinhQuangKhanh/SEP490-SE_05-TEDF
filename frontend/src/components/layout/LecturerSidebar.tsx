import { useAuth } from '@/contexts/AuthContext'
import { useUnreadSupportCount } from '@/hooks/useUnreadSupportCount'
import { SidebarShell, type SidebarItem } from './SidebarShell'

export function LecturerSidebar() {
    const { user } = useAuth()
    const unreadSupportCount = useUnreadSupportCount()
    const isDeptHead = !!user?.roles?.includes('departmenthead')

    const navItems: SidebarItem[] = [
        { label: 'Kho đề tài nghiên cứu', icon: 'inventory_2', path: '/lecturer', exact: true },
        { label: 'Nhóm của tôi', icon: 'groups', path: '/lecturer/groups' },
        { label: 'Tạo đề tài', icon: 'add_circle', path: '/lecturer/create' },
        { label: 'Đề tài cần thẩm định', icon: 'fact_check', path: '/lecturer/moderate' },
        { label: 'Lịch sử thẩm định', icon: 'history', path: '/lecturer/history' },
        // Department-Head-only entries
        ...(isDeptHead
            ? [
                  { label: 'Tổng quan', icon: 'dashboard', path: '/lecturer/dashboard' },
                  { label: 'Phân công thẩm định', icon: 'assignment_ind', path: '/lecturer/assign' },
                  { label: 'Checklist thẩm định', icon: 'checklist', path: '/lecturer/checklist-config' },
                  { label: 'Thống kê', icon: 'bar_chart', path: '/lecturer/statistics' },
              ]
            : []),
    ]

    const footerItems: SidebarItem[] = [
        {
            label: 'Hỗ trợ',
            icon: 'support_agent',
            path: '/lecturer/support',
            badge: unreadSupportCount > 0 ? unreadSupportCount.toString() : undefined,
        },
    ]

    return (
        <SidebarShell
            navItems={navItems}
            footerItems={footerItems}
            profile={{
                name: user?.name || 'Giảng viên',
                subtitle: isDeptHead ? 'Chủ nhiệm bộ môn' : 'Giảng viên',
                avatarUrl: user?.avatar,
            }}
        />
    )
}
