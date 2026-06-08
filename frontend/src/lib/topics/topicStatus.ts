// UI label/colour helpers for topic & project status enums (used by mentor topic screens).

export function sourceTypeLabel(sourceType: number): string {
  return sourceType === 0 ? "Trong kho" : "Đăng ký trực tiếp";
}

export function statusConfig(status: number): { label: string; bg: string; text: string; dot: string } {
  switch (status) {
    case 1:
      return { label: "Chờ duyệt", bg: "bg-amber-50", text: "text-amber-700", dot: "bg-amber-500" };
    case 2:
      return { label: "Yêu cầu sửa", bg: "bg-rose-50", text: "text-rose-700", dot: "bg-rose-500" };
    case 3:
      return { label: "Đã duyệt", bg: "bg-emerald-50", text: "text-emerald-700", dot: "bg-emerald-500" };
    case 4:
      return { label: "Từ chối", bg: "bg-red-50", text: "text-red-700", dot: "bg-red-500" };
    case 5:
      return { label: "Đang thực hiện", bg: "bg-blue-50", text: "text-blue-700", dot: "bg-blue-500" };
    case 6:
      return { label: "Hoàn thành", bg: "bg-teal-50", text: "text-teal-700", dot: "bg-teal-500" };
    case 7:
      return { label: "Đã hủy", bg: "bg-slate-100", text: "text-slate-500", dot: "bg-slate-400" };
    case 8:
      return { label: "Chờ GV duyệt", bg: "bg-violet-50", text: "text-violet-700", dot: "bg-violet-500" };
    default:
      return { label: "Nháp", bg: "bg-slate-100", text: "text-slate-600", dot: "bg-slate-400" };
  }
}

export function evaluationStatusConfig(status: number): { label: string; bg: string; text: string; dot: string } {
  switch (status) {
    case 0:
    case 2:
    case 8:
      return { label: "Chưa gửi thẩm định", bg: "bg-slate-50", text: "text-slate-600", dot: "bg-slate-400" };
    case 1:
      return { label: "Chờ thẩm định", bg: "bg-amber-50", text: "text-amber-700", dot: "bg-amber-500" };
    case 3:
      return { label: "Đã duyệt", bg: "bg-emerald-50", text: "text-emerald-700", dot: "bg-emerald-500" };
    case 4:
      return { label: "Từ chối", bg: "bg-red-50", text: "text-red-700", dot: "bg-red-500" };
    case 5:
      return { label: "Đang thực hiện", bg: "bg-blue-50", text: "text-blue-700", dot: "bg-blue-500" };
    case 6:
      return { label: "Hoàn thành", bg: "bg-teal-50", text: "text-teal-700", dot: "bg-teal-500" };
    case 7:
      return { label: "Đã hủy", bg: "bg-slate-100", text: "text-slate-500", dot: "bg-slate-400" };
    default:
      return { label: "Chưa xác định", bg: "bg-slate-100", text: "text-slate-600", dot: "bg-slate-400" };
  }
}
