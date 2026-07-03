// ProjectStatus enum (backend) → Vietnamese label, badge colour, and a derived "defense result".

export const projectStatusLabels: Record<number, string> = {
  0: 'Nháp',
  1: 'Chờ thẩm định',
  2: 'Cần chỉnh sửa',
  3: 'Đã duyệt',
  4: 'Bị từ chối',
  5: 'Đang thực hiện',
  6: 'Hoàn thành',
  7: 'Đã hủy',
  8: 'Chờ GV duyệt',
};

const statusColors: Record<number, string> = {
  0: 'bg-slate-100 text-slate-600 border-slate-200',
  1: 'bg-amber-500/10 text-amber-600 border-amber-500/20',
  2: 'bg-orange-500/10 text-orange-600 border-orange-500/20',
  3: 'bg-emerald-500/10 text-emerald-600 border-emerald-500/20',
  4: 'bg-red-500/10 text-red-600 border-red-500/20',
  5: 'bg-blue-500/10 text-blue-600 border-blue-500/20',
  6: 'bg-green-500/10 text-green-600 border-green-500/20',
  7: 'bg-red-500/10 text-red-600 border-red-500/20',
  8: 'bg-violet-500/10 text-violet-600 border-violet-500/20',
};

export function projectStatusLabel(statusValue: number): string {
  return projectStatusLabels[statusValue] ?? 'Không rõ';
}

export function projectStatusColor(statusValue: number): string {
  return statusColors[statusValue] ?? 'bg-slate-100 text-slate-600 border-slate-200';
}

export interface DefenseResult {
  label: string;
  color: string;
  icon: string;
  decided: boolean;
}

/**
 * Derived "defense result" (kết quả bảo vệ). NOTE: the backend has no dedicated defense-result
 * field yet, so this is inferred from the project status: Completed → Đạt, Cancelled → Không đạt,
 * otherwise the project has not been defended yet.
 */
export function getDefenseResult(statusValue: number): DefenseResult {
  switch (statusValue) {
    case 6: // Completed
      return { label: 'Đạt (Pass)', color: 'bg-green-500/10 text-green-600 border-green-500/20', icon: 'check_circle', decided: true };
    case 7: // Cancelled
      return { label: 'Không đạt', color: 'bg-red-500/10 text-red-600 border-red-500/20', icon: 'cancel', decided: true };
    case 5: // InProgress
      return { label: 'Đang thực hiện (chưa bảo vệ)', color: 'bg-blue-500/10 text-blue-600 border-blue-500/20', icon: 'hourglass_top', decided: false };
    default:
      return { label: 'Chưa bảo vệ', color: 'bg-slate-100 text-slate-500 border-slate-200', icon: 'schedule', decided: false };
  }
}
