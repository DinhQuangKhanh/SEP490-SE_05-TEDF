import { Outlet } from "react-router-dom";
import { EvaluatorSidebar } from "./EvaluatorSidebar";

export function EvaluatorLayout() {
  return (
    <div className="flex h-screen overflow-hidden bg-slate-50">
      <EvaluatorSidebar />
      <main className="flex-1 h-full overflow-y-auto flex flex-col">
        <Outlet />
      </main>
    </div>
  );
}
