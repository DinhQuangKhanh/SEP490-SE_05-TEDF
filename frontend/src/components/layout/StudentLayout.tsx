import { Outlet } from 'react-router-dom'
import { StudentSidebar } from './StudentSidebar'

export function StudentLayout() {
    return (
        <div className="flex h-screen overflow-hidden bg-[#f9f9fb]">
            <StudentSidebar />
            <main className="flex-1 h-full overflow-y-auto flex flex-col">
                <Outlet />
            </main>
        </div>
    )
}
