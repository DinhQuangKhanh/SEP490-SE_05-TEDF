import { Modal } from "@/components/common/Modal";
import type { GroupMemberDto } from "@/types";

const DETAIL_CONTENT_CLASS = "w-full max-w-md p-0 bg-white shadow-2xl rounded-xl overflow-hidden";

function InfoRow({ icon, label, value }: { icon: string; label: string; value: string }) {
  return (
    <div className="flex items-center gap-3 px-5 py-3 border-b border-slate-100 last:border-b-0">
      <span className="material-symbols-outlined text-slate-400 text-[20px]">{icon}</span>
      <div className="min-w-0">
        <p className="text-[11px] font-semibold tracking-wide uppercase text-slate-400">{label}</p>
        <p className="text-sm font-medium break-words text-slate-800">{value}</p>
      </div>
    </div>
  );
}

/**
 * Read-only popup showing a group member's basic info — everything already carried by
 * GroupMemberDto (no extra fetch). Shared by the lecturer and student group/topic pages.
 */
export function MemberProfileModal({ member, onClose }: { member: GroupMemberDto; onClose: () => void }) {
  const isLeader = member.role === "Leader";

  return (
    <Modal onClose={onClose} contentClassName={DETAIL_CONTENT_CLASS}>
      {/* Header */}
      <div className="flex items-center gap-4 px-5 py-5 bg-primary">
        <div className="flex items-center justify-center text-xl font-bold rounded-full size-14 bg-white/20 text-white shrink-0">
          {member.fullName.charAt(0)}
        </div>
        <div className="min-w-0">
          <h3 className="text-lg font-bold text-white truncate">{member.fullName}</h3>
          <span className="inline-block px-2 py-0.5 mt-1 text-xs font-semibold text-white rounded-full bg-white/20">
            {isLeader ? "Trưởng nhóm" : "Thành viên"}
          </span>
        </div>
        <button
          onClick={onClose}
          className="flex items-center justify-center ml-auto transition-colors rounded-lg size-8 text-white/80 hover:bg-white/10 hover:text-white shrink-0"
          aria-label="Đóng"
        >
          <span className="material-symbols-outlined text-[20px]">close</span>
        </button>
      </div>

      {/* Details */}
      <div className="py-1">
        {member.studentCode && <InfoRow icon="badge" label="Mã số sinh viên" value={member.studentCode} />}
        {member.email && <InfoRow icon="mail" label="Email" value={member.email} />}
        <InfoRow
          icon="calendar_today"
          label="Ngày tham gia"
          value={new Date(member.joinedAt).toLocaleDateString("vi-VN")}
        />
        {member.status && <InfoRow icon="check_circle" label="Trạng thái" value={member.status} />}
      </div>
    </Modal>
  );
}
