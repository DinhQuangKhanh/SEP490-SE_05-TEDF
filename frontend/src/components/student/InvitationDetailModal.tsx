import { InvitationDto } from "@/types";
import { motion, AnimatePresence } from "framer-motion";

interface InvitationDetailModalProps {
  invitation: InvitationDto | null;
  isOpen: boolean;
  onClose: () => void;
  onAccept: (groupId: string, invitationId: number) => Promise<void>;
  onReject: (groupId: string, invitationId: number) => Promise<void>;
  isLoading?: boolean;
}

export function InvitationDetailModal({
  invitation,
  isOpen,
  onClose,
  onAccept,
  onReject,
  isLoading = false,
}: InvitationDetailModalProps) {
  if (!invitation) return null;

  const isExpired = new Date(invitation.expiresAt) < new Date();
  const isPending = invitation.status.toLowerCase() === "pending";
  const canAction = isPending && !isExpired;

  const handleAccept = async () => {
    await onAccept(invitation.groupId, invitation.id);
    onClose();
  };

  const handleReject = async () => {
    await onReject(invitation.groupId, invitation.id);
    onClose();
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            key="backdrop"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="fixed inset-0 z-50 bg-black/40 backdrop-blur-sm"
          />

          {/* Modal */}
          <motion.div
            key="modal"
            initial={{ opacity: 0, scale: 0.95, y: 10 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 10 }}
            transition={{ type: "spring", duration: 0.3 }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none"
          >
            <div className="w-full max-w-md overflow-hidden bg-white border border-gray-100 shadow-xl pointer-events-auto rounded-2xl">
              {/* Header */}
              <div className="flex items-center justify-between px-6 py-5 border-b border-gray-100 bg-gradient-to-r from-blue-50 to-blue-100/50">
                <div className="flex items-center gap-3">
                  <div className="flex items-center justify-center w-10 h-10 bg-blue-200 rounded-full">
                    <span className="text-lg text-blue-600 material-symbols-outlined">group_add</span>
                  </div>
                  <div>
                    <h3 className="text-base font-bold text-slate-900">Lời mời tham gia nhóm</h3>
                  </div>
                </div>
                <button
                  onClick={onClose}
                  className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-gray-100 transition-colors"
                >
                  <span className="text-xl material-symbols-outlined">close</span>
                </button>
              </div>

              {/* Body */}
              <div className="px-6 py-5 space-y-5">
                {/* Group Info */}
                <div className="p-4 border border-blue-100 rounded-lg bg-blue-50">
                  <p className="mb-1 text-xs font-semibold text-blue-600">NHÓM</p>
                  <h4 className="text-lg font-bold text-slate-900">{invitation.groupName || invitation.groupCode}</h4>
                  <p className="mt-1 text-sm text-slate-600">Mã nhóm: {invitation.groupCode}</p>
                </div>

                {/* Inviter */}
                <div>
                  <p className="mb-1 text-xs font-semibold text-slate-500">NGƯỜI MỜI</p>
                  <p className="text-sm font-medium text-slate-800">{invitation.inviterName}</p>
                </div>

                {/* Message */}
                {invitation.message && (
                  <div className="p-3 border border-gray-100 rounded-lg bg-gray-50">
                    <p className="mb-2 text-xs font-semibold text-slate-500">LỜI NHẮN</p>
                    <p className="text-sm italic text-slate-700">"{invitation.message}"</p>
                  </div>
                )}

                {/* Expiry Info */}
                <div className="flex items-center gap-2 text-xs">
                  <span className="text-lg material-symbols-outlined text-slate-400">schedule</span>
                  <div>
                    <p className="text-slate-600">
                      {isExpired ? (
                        <>
                          <span className="font-semibold text-red-600">Lời mời đã hết hạn</span>
                        </>
                      ) : (
                        <>
                          Hết hạn lúc{" "}
                          <span className="font-semibold text-slate-800">
                            {new Date(invitation.expiresAt).toLocaleString("vi-VN")}
                          </span>
                        </>
                      )}
                    </p>
                  </div>
                </div>

                {/* Status Badge */}
                <div>
                  <span
                    className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold ${
                      invitation.status.toLowerCase() === "pending"
                        ? "bg-yellow-100 text-yellow-800"
                        : invitation.status.toLowerCase() === "accepted"
                          ? "bg-green-100 text-green-800"
                          : "bg-red-100 text-red-800"
                    }`}
                  >
                    <span className="text-sm material-symbols-outlined">
                      {invitation.status.toLowerCase() === "pending"
                        ? "schedule"
                        : invitation.status.toLowerCase() === "accepted"
                          ? "check_circle"
                          : "cancel"}
                    </span>
                    {invitation.status.toLowerCase() === "pending"
                      ? "Chờ xử lý"
                      : invitation.status.toLowerCase() === "accepted"
                        ? "Đã chấp nhận"
                        : "Đã từ chối"}
                  </span>
                </div>
              </div>

              {/* Footer */}
              <div className="flex gap-3 px-6 py-4 border-t border-gray-100 bg-gray-50">
                {canAction ? (
                  <>
                    <button
                      onClick={() => handleReject()}
                      disabled={isLoading}
                      className="flex-1 px-4 py-2 font-semibold text-red-700 transition-colors border border-red-300 rounded-lg hover:bg-red-50 disabled:opacity-50"
                    >
                      {isLoading ? "Đang xử lý..." : "Từ chối"}
                    </button>
                    <button
                      onClick={() => handleAccept()}
                      disabled={isLoading}
                      className="flex-1 px-4 py-2 font-semibold text-white transition-colors bg-green-600 rounded-lg hover:bg-green-700 disabled:opacity-50"
                    >
                      {isLoading ? "Đang xử lý..." : "Chấp nhận"}
                    </button>
                  </>
                ) : (
                  <button
                    onClick={onClose}
                    className="w-full px-4 py-2 font-semibold transition-colors rounded-lg bg-slate-200 text-slate-700 hover:bg-slate-300"
                  >
                    Đóng
                  </button>
                )}
              </div>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}
