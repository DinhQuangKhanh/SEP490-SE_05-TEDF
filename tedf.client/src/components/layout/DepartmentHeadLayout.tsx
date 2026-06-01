import { Outlet } from "react-router-dom";
import { DepartmentHeadSidebar } from "./DepartmentHeadSidebar";

export function DepartmentHeadLayout() {
  return (
    <div className="flex h-screen overflow-hidden bg-slate-50">
      <DepartmentHeadSidebar />
      <main className="flex-1 h-full overflow-y-auto flex flex-col">
        <Outlet />
      </main>
    </div>
  );
}
