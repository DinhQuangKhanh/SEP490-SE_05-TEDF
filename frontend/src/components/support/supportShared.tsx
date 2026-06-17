import { motion } from "framer-motion";

// Shared animation variants for the support screens.
export const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.1 } },
};

export const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

export function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diff / 60000);
  if (minutes < 1) return "Vừa xong";
  if (minutes < 60) return `${minutes} phút trước`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} giờ trước`;
  const days = Math.floor(hours / 24);
  return `${days} ngày trước`;
}

export function statusLabel(status: string): string {
  switch (status) {
    case "Open":
      return "Chưa đọc";
    case "InProgress":
      return "Đang xử lý";
    case "Resolved":
      return "Đã giải quyết";
    case "Closed":
      return "Đã đóng";
    default:
      return status;
  }
}

export function statusClass(status: string): string {
  switch (status) {
    case "Open":
      return "bg-error/10 text-error";
    case "InProgress":
      return "bg-blue-50 text-blue-600";
    case "Resolved":
      return "bg-success/10 text-success";
    case "Closed":
      return "bg-slate-100 text-slate-500";
    default:
      return "bg-slate-100 text-slate-600";
  }
}

export function priorityDot(priority: string): string {
  switch (priority) {
    case "High":
      return "bg-error";
    case "Medium":
      return "bg-yellow-500";
    default:
      return "bg-slate-300";
  }
}

export function StatCard({
  icon,
  iconColor,
  iconBg,
  value,
  label,
  valueColor = "text-slate-800",
}: {
  icon: string;
  iconColor: string;
  iconBg: string;
  value: number;
  label: string;
  valueColor?: string;
}) {
  return (
    <motion.div whileHover={{ scale: 1.02 }} className="bento-card p-5 rounded-md">
      <div className={`w-8 h-8 rounded-md ${iconBg} ${iconColor} flex items-center justify-center mb-2`}>
        <span className="material-symbols-outlined text-[20px]">{icon}</span>
      </div>
      <h3 className={`text-2xl font-bold ${valueColor} mt-1`}>{value}</h3>
      <p className="text-xs text-slate-500 font-medium mt-0.5">{label}</p>
    </motion.div>
  );
}
