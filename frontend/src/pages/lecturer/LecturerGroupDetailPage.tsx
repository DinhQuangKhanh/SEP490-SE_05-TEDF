import { motion } from "framer-motion";
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { NotificationDropdown } from "@/components/layout";
import { MemberProfileModal } from "@/components/common/MemberProfileModal";
import { RegistrationNoteView } from "@/components/student/RegistrationNoteEditor";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { statusConfig, studentGroupService, topicPoolService, topicService } from "@/lib";
import type { GroupMemberDto, GroupRegistrationDto, MentorGroupDto, TopicDetail, TopicDocument } from "@/types";

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.05 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

// MentorGroupDto.projectStatus is the backend ProjectStatus enum stringified; map it back to the
// numeric code so we can reuse the shared statusConfig() label/colour helper.
const PROJECT_STATUS_CODE: Record<string, number> = {
  Draft: 0,
  PendingEvaluation: 1,
  NeedsModification: 2,
  Approved: 3,
  Rejected: 4,
  InProgress: 5,
  Completed: 6,
  Cancelled: 7,
  PendingMentorReview: 8,
};

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("vi-VN");
}

function docIcon(fileType: string): { icon: string; cls: string } {
  const t = (fileType ?? "").toLowerCase();
  if (t.includes("pdf")) return { icon: "picture_as_pdf", cls: "bg-rose-50 text-rose-600" };
  if (t.includes("doc") || t.includes("word")) return { icon: "description", cls: "bg-blue-50 text-blue-600" };
  if (t.includes("png") || t.includes("jpg") || t.includes("jpeg") || t.includes("image"))
    return { icon: "image", cls: "bg-amber-50 text-amber-600" };
  return { icon: "draft", cls: "bg-slate-100 text-slate-500" };
}

export function LecturerGroupDetailPage() {
  const { id: groupId } = useParams();
  const navigate = useNavigate();
  const { showError } = useSystemError();

  const [group, setGroup] = useState<MentorGroupDto | null>(null);
  const [detail, setDetail] = useState<TopicDetail | null>(null);
  const [documents, setDocuments] = useState<TopicDocument[]>([]);
  const [registration, setRegistration] = useState<GroupRegistrationDto | null>(null);
  const [selectedMember, setSelectedMember] = useState<GroupMemberDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!groupId) return;
    let cancelled = false;
    setLoading(true);

    studentGroupService
      .getMentorGroups()
      .then(async (groups) => {
        const g = groups.find((x) => x.groupId === groupId) ?? null;
        if (cancelled) return;
        setGroup(g);
        if (g?.projectId) {
          const [d, docs, reg] = await Promise.all([
            topicService.getTopicDetail(g.projectId),
            topicService.getTopicDocuments(g.projectId),
            // A direct-registration topic has no confirmed pool registration → null; tolerate failure
            // so a missing note never blocks the rest of the page.
            topicPoolService.getProjectRegistration(g.projectId).catch(() => null),
          ]);
          if (cancelled) return;
          setDetail(d);
          setDocuments(docs);
          setRegistration(reg);
        }
      })
      .catch((err) => showError(err instanceof Error ? err.message : "Không thể tải chi tiết đề tài"))
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [groupId, showError]);

  const badge = group?.projectStatus ? statusConfig(PROJECT_STATUS_CODE[group.projectStatus] ?? -1) : null;

  const sections = detail
    ? [
        { label: "Tên tiếng Anh", value: detail.nameEn },
        { label: "Tên viết tắt", value: detail.nameAbbr },
        { label: "Mô tả", value: detail.description },
        { label: "Mục tiêu đề tài", value: detail.objectives },
        { label: "Phạm vi thực hiện", value: detail.scope },
        { label: "Công nghệ sử dụng", value: detail.technologies },
        { label: "Kết quả dự kiến", value: detail.expectedResults },
        { label: "Ngày tạo", value: detail.createdAt ? formatDate(detail.createdAt) : "" },
      ].filter((s) => s.value && s.value.trim().length > 0)
    : [];

  return (
    <>
      {/* Header */}
      <header className="z-10 px-8 py-5 shadow-lg bg-primary shrink-0">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3 text-white">
            <button
              type="button"
              onClick={() => navigate("/lecturer/groups")}
              className="flex items-center justify-center transition-colors rounded-lg size-9 hover:bg-white/10"
              aria-label="Quay lại"
            >
              <span className="material-symbols-outlined">arrow_back</span>
            </button>
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium text-blue-100/80">Nhóm của tôi</span>
              <span className="material-symbols-outlined text-[18px] text-blue-100/60">chevron_right</span>
              <h2 className="text-lg font-bold">
                Chi tiết đề tài{group ? ` — ${group.displayName || group.groupName || group.groupCode}` : ""}
              </h2>
            </div>
          </div>
          <NotificationDropdown role="mentor" isNavy={true} />
        </div>
      </header>

      {/* Content */}
      <div className="flex-1 p-8 overflow-y-auto bg-slate-100">
        {loading && (
          <div className="flex items-center justify-center py-12">
            <div className="w-8 h-8 border-b-2 rounded-full animate-spin border-primary" />
          </div>
        )}

        {!loading && !group && (
          <div className="p-12 text-center bg-white border rounded-xl border-slate-200">
            <span className="mb-3 text-5xl material-symbols-outlined text-slate-300">group_off</span>
            <h3 className="mb-1 text-lg font-bold text-slate-700">Không tìm thấy nhóm</h3>
            <p className="text-sm text-slate-500">Nhóm này không tồn tại hoặc bạn không hướng dẫn nhóm này.</p>
          </div>
        )}

        {!loading && group && (
          <motion.div variants={container} initial="hidden" animate="show" className="grid grid-cols-12 gap-8">
            {/* Left Column - Topic Details */}
            <motion.div variants={item} className="col-span-12 space-y-6 lg:col-span-8">
              <div className="overflow-hidden bg-white border shadow-sm rounded-xl border-slate-200">
                <div className="flex items-center justify-between p-6 border-b border-slate-100 bg-slate-50/50">
                  <div className="flex items-center gap-3">
                    <span className="material-symbols-outlined text-primary text-[28px]">description</span>
                    <h3 className="text-xl font-bold tracking-tight text-slate-900">Mô tả chi tiết đề tài</h3>
                  </div>
                  {badge && (
                    <span className={`px-3 py-1 text-xs font-bold rounded-full ${badge.bg} ${badge.text}`}>
                      {badge.label}
                    </span>
                  )}
                </div>

                {detail ? (
                  <div className="p-8 space-y-8">
                    <section>
                      <h4 className="mb-3 text-sm font-bold tracking-widest uppercase text-slate-400">Tên đề tài</h4>
                      <p className="text-2xl font-bold leading-tight text-slate-900">{detail.nameVi}</p>
                      {detail.code && <p className="mt-2 text-sm font-medium text-slate-400"># {detail.code}</p>}
                    </section>

                    {detail.mentorFeedback && (
                      <div className="p-4 border rounded-lg border-amber-200 bg-amber-50">
                        <p className="mb-1 text-xs font-bold tracking-wide uppercase text-amber-700">
                          Phản hồi của giảng viên
                        </p>
                        <p className="text-sm whitespace-pre-line text-amber-800">{detail.mentorFeedback}</p>
                      </div>
                    )}

                    {sections.map((s, idx) => (
                      <section key={s.label}>
                        <div className="flex items-center gap-2 mb-4">
                          <span className="rounded-full size-2 bg-primary" />
                          <h4 className="text-base font-bold text-slate-800">
                            {idx + 1}. {s.label}
                          </h4>
                        </div>
                        <p className="ml-4 leading-relaxed whitespace-pre-line text-slate-600">{s.value}</p>
                      </section>
                    ))}
                  </div>
                ) : (
                  <div className="p-12 text-center">
                    <span className="mb-3 text-5xl material-symbols-outlined text-slate-300">assignment</span>
                    <h3 className="mb-1 text-lg font-bold text-slate-700">Chưa có đề tài</h3>
                    <p className="text-sm text-slate-500">Nhóm này chưa đăng ký đề tài nào.</p>
                  </div>
                )}
              </div>
            </motion.div>

            {/* Right Column - Sidebar */}
            <motion.div variants={item} className="col-span-12 space-y-6 lg:col-span-4">
              {/* Team Info */}
              <section className="overflow-hidden bg-white border shadow-sm rounded-xl border-slate-200">
                <div className="flex items-center gap-2 p-4 border-b border-slate-100 bg-slate-50/50">
                  <span className="material-symbols-outlined text-slate-500 text-[20px]">groups</span>
                  <h3 className="text-sm font-bold text-slate-900">Thông tin nhóm thực hiện</h3>
                </div>
                <div className="p-4 space-y-4">
                  {group.members.length === 0 && (
                    <p className="text-sm italic text-slate-400">Nhóm chưa có thành viên.</p>
                  )}
                  {group.members.map((member) => (
                    <button
                      key={member.studentId}
                      type="button"
                      onClick={() => setSelectedMember(member)}
                      className="flex items-center w-full gap-3 p-3 text-left transition-colors border rounded-lg border-slate-100 hover:border-primary/30 hover:bg-slate-50"
                    >
                      <div className="flex items-center justify-center text-xs font-bold rounded-full size-10 bg-slate-200 text-slate-500">
                        {member.fullName.charAt(0)}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-bold truncate text-slate-900">
                          {member.fullName}
                          {member.role === "Leader" && (
                            <span className="ml-1 text-xs font-medium text-primary">(Trưởng nhóm)</span>
                          )}
                        </p>
                        {member.studentCode && <p className="text-xs text-slate-500">MSSV: {member.studentCode}</p>}
                      </div>
                      <span className="material-symbols-outlined text-slate-400 text-[18px]">contact_mail</span>
                    </button>
                  ))}
                </div>
              </section>

              {/* Files (documents uploaded onto the project) */}
              <section className="overflow-hidden bg-white border shadow-sm rounded-xl border-slate-200">
                <div className="flex items-center justify-between p-4 border-b border-slate-100 bg-slate-50/50">
                  <div className="flex items-center gap-2">
                    <span className="material-symbols-outlined text-slate-500 text-[20px]">folder</span>
                    <h3 className="text-sm font-bold text-slate-900">Kho tài liệu của nhóm</h3>
                  </div>
                  <span className="text-[10px] font-bold text-slate-400 uppercase tracking-tight">
                    {documents.length} files
                  </span>
                </div>
                <div className="p-4 space-y-2">
                  {documents.length === 0 && (
                    <p className="text-sm italic text-slate-400">Chưa có tài liệu nào.</p>
                  )}
                  {documents.map((file) => {
                    const { icon, cls } = docIcon(file.fileType);
                    return (
                      <div
                        key={file.id}
                        className="flex items-center gap-3 p-2 transition-colors rounded-lg hover:bg-slate-50"
                      >
                        <div className={`flex items-center justify-center rounded size-9 ${cls}`}>
                          <span className="material-symbols-outlined text-[20px]">{icon}</span>
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="text-xs font-bold truncate text-slate-800">{file.originalFileName}</p>
                          <p className="text-[10px] text-slate-400 uppercase font-medium">
                            {formatFileSize(file.fileSize)} • {formatDate(file.uploadedAt)}
                          </p>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </section>

              {/* Registration reason + attachments the group submitted (pool topics) */}
              {registration?.note && (
                <section className="overflow-hidden bg-white border shadow-sm rounded-xl border-slate-200">
                  <div className="flex items-center gap-2 p-4 border-b border-slate-100 bg-slate-50/50">
                    <span className="material-symbols-outlined text-slate-500 text-[20px]">assignment</span>
                    <h3 className="text-sm font-bold text-slate-900">Lý do đăng ký &amp; tài liệu đính kèm</h3>
                  </div>
                  <div className="p-4">
                    <RegistrationNoteView note={registration.note} />
                  </div>
                </section>
              )}
            </motion.div>
          </motion.div>
        )}
      </div>

      {selectedMember && <MemberProfileModal member={selectedMember} onClose={() => setSelectedMember(null)} />}
    </>
  );
}
