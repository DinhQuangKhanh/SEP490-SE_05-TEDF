import { useAuth } from "@/contexts/AuthContext";

/**
 * Full-screen takeover shown when the signed-in account may not use the system
 * (locked / inactive account, or a student not on any active/upcoming eligible list).
 */
export function IneligiblePage() {
  const { access, user, logout } = useAuth();
  const kind = access?.kind ?? null;

  let title: string;
  let icon: string;
  if (kind === "locked") { title = "Tài khoản đã bị khóa"; icon = "lock"; }
  else if (kind === "inactive") { title = "Tài khoản đã bị vô hiệu hóa"; icon = "block"; }
  else if (kind === "mentor_not_eligible") { title = "Bạn chưa được phân công trong học kỳ này"; icon = "school"; }
  else { title = "Bạn chưa đủ điều kiện truy cập"; icon = "school"; }

  const reason =
    access?.reason ??
    "Bạn không thuộc danh sách sinh viên đủ điều kiện làm đồ án trong học kỳ hiện tại hoặc sắp tới.";

  return (
    <div className="flex items-center justify-center min-h-screen p-4 bg-slate-50">
      <div className="w-full max-w-md p-8 text-center bg-white border shadow-sm rounded-2xl border-slate-200">
        <div className="flex items-center justify-center w-16 h-16 mx-auto mb-5 rounded-full bg-amber-100 text-amber-600">
          <span className="material-symbols-outlined text-[34px]">{icon}</span>
        </div>
        <h1 className="mb-2 text-xl font-bold text-slate-800">{title}</h1>
        <p className="text-sm leading-relaxed text-slate-500">{reason}</p>
        {user?.email && (
          <p className="mt-3 text-xs text-slate-400">
            Đăng nhập bằng: <span className="font-medium text-slate-600">{user.email}</span>
          </p>
        )}
        <p className="mt-4 text-xs text-slate-400">
          Nếu bạn cho rằng đây là nhầm lẫn, vui lòng liên hệ quản trị viên hoặc giáo vụ.
        </p>
        <button
          type="button"
          onClick={logout}
          className="flex items-center justify-center w-full gap-2 px-4 py-2.5 mt-6 text-sm font-bold text-white transition-colors rounded-md bg-primary hover:bg-primary/90"
        >
          <span className="material-symbols-outlined text-[18px]">logout</span>{" "}
          Đăng xuất
        </button>
      </div>
    </div>
  );
}
