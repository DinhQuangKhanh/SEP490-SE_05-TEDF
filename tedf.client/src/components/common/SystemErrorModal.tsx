import { motion, AnimatePresence } from "framer-motion";

interface Props {
  message: string | null;
  onClose: () => void;
}

export function SystemErrorModal({ message, onClose }: Props) {
  return (
    <AnimatePresence>
      {message !== null && (
        <>
          {/* Backdrop */}
          <motion.div
            key="backdrop"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="fixed inset-0 bg-black/40 z-50 backdrop-blur-sm"
          />

          {/* Dialog */}
          <motion.div
            key="dialog"
            initial={{ opacity: 0, scale: 0.95, y: 10 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 10 }}
            transition={{ type: "spring", duration: 0.3 }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none"
          >
            <div className="pointer-events-auto bg-white rounded-2xl shadow-xl w-full max-w-md border border-gray-100 overflow-hidden">
              {/* Header */}
              <div className="flex items-center gap-3 px-6 py-5 border-b border-gray-100">
                <div className="p-2 rounded-xl bg-red-50 text-red-600 shrink-0">
                  <span className="material-symbols-outlined text-xl">error</span>
                </div>
                <div className="flex-1 min-w-0">
                  <h3 className="text-slate-900 font-bold text-base">Lỗi hệ thống</h3>
                  <p className="text-slate-500 text-xs mt-0.5">Máy chủ trả về lỗi</p>
                </div>
                <button
                  onClick={onClose}
                  className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-gray-100 transition-colors"
                >
                  <span className="material-symbols-outlined text-xl">close</span>
                </button>
              </div>

              {/* Body */}
              <div className="px-6 py-5">
                <p className="text-slate-700 text-sm leading-relaxed break-words">{message}</p>
              </div>

              {/* Footer */}
              <div className="px-6 py-4 bg-gray-50 border-t border-gray-100 flex justify-end">
                <button
                  onClick={onClose}
                  className="h-9 px-5 text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-dark transition-colors shadow-sm"
                >
                  Đã hiểu
                </button>
              </div>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}
