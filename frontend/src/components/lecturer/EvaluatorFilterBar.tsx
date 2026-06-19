import { motion } from "framer-motion";
import { fadeItem } from "@/lib/common/ui";

// Literal classes so Tailwind keeps them (dynamic `md:col-span-${n}` would be purged).
const COL_SPAN: Record<number, string> = {
  2: "md:col-span-2",
  3: "md:col-span-3",
  4: "md:col-span-4",
};

export interface FilterSelect {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
  colSpan?: 2 | 3 | 4;
}

/**
 * Filter bar (search + configurable selects + clear) shared by the evaluation queue
 * and history pages. Each page wires its own state; this only renders the chrome.
 */
export function EvaluatorFilterBar({
  search,
  onSearch,
  searchPlaceholder,
  searchColSpan = 3,
  selects,
  onClear,
}: {
  search: string;
  onSearch: (value: string) => void;
  searchPlaceholder: string;
  searchColSpan?: 2 | 3 | 4;
  selects: FilterSelect[];
  onClear: () => void;
}) {
  return (
    <motion.div variants={fadeItem} className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
      <div className="grid grid-cols-1 md:grid-cols-12 gap-4 items-end">
        <div className={`${COL_SPAN[searchColSpan]} flex flex-col gap-1.5`}>
          <label className="text-xs font-bold text-slate-500 uppercase tracking-wide">Tìm kiếm</label>
          <div className="relative group">
            <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 group-focus-within:text-primary transition-colors">
              search
            </span>
            <input
              className="w-full pl-10 pr-4 py-2.5 rounded-lg border border-gray-200 bg-gray-50 text-sm font-medium focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all outline-none"
              placeholder={searchPlaceholder}
              type="text"
              value={search}
              onChange={(e) => onSearch(e.target.value)}
            />
          </div>
        </div>

        {selects.map((select) => (
          <div key={select.label} className={`${COL_SPAN[select.colSpan ?? 2]} flex flex-col gap-1.5`}>
            <label className="text-xs font-bold text-slate-500 uppercase tracking-wide">{select.label}</label>
            <select
              className="w-full px-3 py-2.5 rounded-lg border border-gray-200 bg-white text-sm font-medium focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none cursor-pointer"
              value={select.value}
              onChange={(e) => select.onChange(e.target.value)}
            >
              {select.options.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
        ))}

        <div className="md:col-span-3 flex justify-end gap-2">
          <button
            type="button"
            onClick={onClear}
            className="flex-1 md:flex-none h-[42px] px-4 rounded-lg border border-gray-200 text-slate-500 font-semibold text-sm hover:bg-gray-50 transition-colors flex items-center justify-center gap-2"
          >
            <span className="material-symbols-outlined text-[18px]">filter_alt_off</span>
            Xóa lọc
          </button>
        </div>
      </div>
    </motion.div>
  );
}
