import { Outlet } from 'react-router-dom'
import { MentorSidebar } from './MentorSidebar'

export function MentorLayout() {
    return (
        <div className="flex h-screen w-full overflow-hidden bg-slate-100">
            <MentorSidebar />
            <main className="flex-1 flex flex-col min-w-0 overflow-hidden">
                <Outlet />
            </main>
        </div>
    )
}
