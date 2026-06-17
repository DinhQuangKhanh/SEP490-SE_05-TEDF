import { Outlet } from 'react-router-dom'
import { LecturerSidebar } from './LecturerSidebar'

export function LecturerLayout() {
    return (
        <div className="flex h-screen w-full overflow-hidden bg-slate-100">
            <LecturerSidebar />
            <main className="flex-1 flex flex-col min-w-0 overflow-hidden">
                <Outlet />
            </main>
        </div>
    )
}
