import { motion } from "framer-motion";
import { NotificationDropdown, UserRole } from "./NotificationDropdown";

interface HeaderProps {
  title: string;
  subtitle?: string;
  showSearch?: boolean;
  searchPlaceholder?: string;
  variant?: "default" | "navy" | "primary";
  breadcrumb?: { label: string; path?: string }[];
  actions?: React.ReactNode;
  role?: UserRole;
}

export function Header({
  title,
  subtitle,
  showSearch = true,
  searchPlaceholder = "Tìm kiếm nhanh...",
  variant = "default",
  breadcrumb,
  actions,
  role = "admin",
}: HeaderProps) {
  const isNavy = variant === "navy";
  const isPrimary = variant === "primary";
  const isDark = isNavy || isPrimary;

  return (
    <motion.header
      initial={{ opacity: 0, y: -20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className={`flex items-center justify-between px-8 py-4 sticky top-0 z-40 ${
        isPrimary
          ? "bg-primary shadow-md text-white"
          : isNavy
            ? "bg-navy-header border-b border-blue-900 shadow-sm text-white"
            : "bg-white/90 backdrop-blur-md border-b border-slate-200"
      }`}
    >
      <div className="flex items-center gap-4 flex-1">
        {breadcrumb ? (
          <div className={`flex items-center gap-2 text-sm ${isDark ? "text-blue-100/80" : "text-slate-500"}`}>
            <span className="material-symbols-outlined text-[20px]">home</span>
            {breadcrumb.map((item, index) => (
              <span key={index} className="flex items-center gap-2">
                <span>/</span>
                <span className={isDark ? "text-white font-medium" : "text-slate-800 font-medium"}>{item.label}</span>
              </span>
            ))}
          </div>
        ) : (
          <div>
            <h2 className={`text-xl font-bold tracking-tight ${isDark ? "text-white" : "text-slate-800"}`}>{title}</h2>
            {subtitle && <p className={`text-xs ${isDark ? "text-blue-100/80" : "text-slate-500"}`}>{subtitle}</p>}
          </div>
        )}
      </div>

      <div className="flex items-center gap-6">
        {actions}

        {showSearch && (
          <div className="relative hidden md:block w-64">
            <span
              className={`absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-[20px] ${
                isDark ? "text-blue-200" : "text-slate-400"
              }`}
            >
              search
            </span>
            <input
              className={`w-full text-sm rounded-md py-2 pl-10 pr-4 focus:outline-none transition-all ${
                isDark
                  ? "bg-white/10 border border-white/20 text-white placeholder-blue-200/70 focus:ring-1 focus:ring-white/40 focus:border-white/40 focus:bg-white/20"
                  : "bg-slate-100 border border-slate-200 text-slate-800 placeholder-slate-400 focus:ring-1 focus:ring-primary focus:border-primary focus:bg-white"
              }`}
              placeholder={searchPlaceholder}
              type="text"
            />
          </div>
        )}

        <NotificationDropdown role={role} isNavy={isDark} />
      </div>
    </motion.header>
  );
}
