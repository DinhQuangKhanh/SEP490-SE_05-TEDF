import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'

export function AdminLayout() {
    return (
        <div className="bg-slate-50 text-slate-900 overflow-hidden h-screen flex">
            <Sidebar />
            <main className="flex-1 flex flex-col h-full overflow-y-auto relative">
                <Outlet />
            </main>
        </div>
    )
}
