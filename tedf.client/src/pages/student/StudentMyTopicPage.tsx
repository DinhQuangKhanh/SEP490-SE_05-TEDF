import { motion } from "framer-motion";
import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Header } from "@/components/layout";
import { apiClient } from "@/lib/apiClient";
import { studentGroupService, type StudentGroupDto } from "@/lib/studentGroupService";
import { topicPoolService, type TopicDetail, type TopicDocument } from "@/lib/topicPoolService";
import { directTopicService } from "@/lib/directTopicService";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { useAuth } from "@/contexts/AuthContext";
import { CreateDirectTopicForm } from "@/components/student/CreateDirectTopicForm";
import { EditDirectTopicForm } from "@/components/student/EditDirectTopicForm";

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.05 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

function statusLabel(status: string | undefined | null): string {
  switch (status) {
    case "Draft":
      return "Nháp";
    case "PendingEvaluation":
      return "Chờ thẩm định";
    case "NeedsModification":
      return "Cần chỉnh sửa";
    case "Approved":
      return "Đã duyệt";
    case "Rejected":
      return "Từ chối";
    case "InProgress":
      return "Đang thực hiện";
    case "Completed":
      return "Hoàn thành";
    case "Cancelled":
      return "Đã hủy";
    case "PendingMentorReview":
      return "Chờ GV duyệt";
    case "Pending":
      return "Chờ xác nhận";
    default:
      return status ?? "Chưa xác định";
  }
}

function statusColor(status: string | undefined | null): string {
  switch (status) {
    case "Draft":
      return "bg-slate-100 text-slate-600";
    case "PendingEvaluation":
      return "bg-amber-100 text-amber-700";
    case "NeedsModification":
      return "bg-rose-100 text-rose-700";
    case "Approved":
      return "bg-emerald-100 text-emerald-700";
    case "Rejected":
      return "bg-red-100 text-red-700";
    case "InProgress":
      return "bg-green-100 text-green-700";
    case "Completed":
      return "bg-blue-100 text-blue-700";
    case "Cancelled":
      return "bg-gray-100 text-gray-500";
    case "PendingMentorReview":
      return "bg-violet-100 text-violet-700";
    default:
      return "bg-gray-100 text-gray-600";
  }
}

function formatDate(isoString: string | null | undefined): string {
  if (!isoString) return "—";
  const d = new Date(isoString);
  return d.toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

function formatFileSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  return `${Math.ceil(bytes / 1024)} KB`;
}

const MAX_ATTACHMENTS = 5;
const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;
const MAX_TOTAL_SIZE_BYTES = 25 * 1024 * 1024;
const ACCEPTED_TYPES = [
  ".pdf",
  ".doc",
  ".docx",
  ".xls",
  ".xlsx",
  ".ppt",
  ".pptx",
  ".zip",
  ".rar",
  ".jpg",
  ".jpeg",
  ".png",
];
const DANGEROUS_EXTENSIONS = ["php", "phtml", "asp", "aspx", "jsp", "exe", "dll", "bat", "cmd", "ps1", "sh", "js"];

function isSuspiciousDoubleExtension(fileName: string): boolean {
  const parts = fileName.toLowerCase().split(".").filter(Boolean);
  if (parts.length < 3) return false;
  return DANGEROUS_EXTENSIONS.includes(parts[parts.length - 2]);
}

function validateFiles(current: File[], incoming: File[]): { accepted: File[]; rejected: string[] } {
  const accepted: File[] = [];
  const rejected: string[] = [];
  const currentTotal = current.reduce((sum, f) => sum + f.size, 0);
  let acceptedTotal = 0;
  incoming.forEach((file) => {
    if (current.length + accepted.length >= MAX_ATTACHMENTS) {
      rejected.push(`Không thể thêm '${file.name}': tối đa ${MAX_ATTACHMENTS} file.`);
      return;
    }
    const ext = `.${file.name.split(".").pop()?.toLowerCase() ?? ""}`;
    if (!ACCEPTED_TYPES.includes(ext)) {
      rejected.push(`'${file.name}' không đúng định dạng cho phép.`);
      return;
    }
    if (isSuspiciousDoubleExtension(file.name)) {
      rejected.push(`'${file.name}' có tên file không an toàn.`);
      return;
    }
    if (file.size > MAX_FILE_SIZE_BYTES) {
      rejected.push(`'${file.name}' vượt quá ${formatFileSize(MAX_FILE_SIZE_BYTES)}.`);
      return;
    }
    if (currentTotal + acceptedTotal + file.size > MAX_TOTAL_SIZE_BYTES) {
      rejected.push(`Tổng dung lượng vượt quá ${formatFileSize(MAX_TOTAL_SIZE_BYTES)} khi thêm '${file.name}'.`);
      return;
    }
    accepted.push(file);
    acceptedTotal += file.size;
  });
  return { accepted, rejected };
}

const FILE_ICON_MAP: Record<string, string> = {
  ".pdf": "picture_as_pdf",
  ".doc": "description",
  ".docx": "description",
  ".xls": "table_chart",
  ".xlsx": "table_chart",
  ".ppt": "slideshow",
  ".pptx": "slideshow",
  ".zip": "folder_zip",
  ".rar": "folder_zip",
  ".jpg": "image",
  ".jpeg": "image",
  ".png": "image",
};

export function StudentMyTopicPage() {
  const navigate = useNavigate();
  const { showError } = useSystemError();
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [myGroup, setMyGroup] = useState<StudentGroupDto | null>(null);
  const [topicDetail, setTopicDetail] = useState<TopicDetail | null>(null);
  const [documents, setDocuments] = useState<TopicDocument[]>([]);
  const [pendingFiles, setPendingFiles] = useState<File[]>([]);
  const [fileWarnings, setFileWarnings] = useState<string[]>([]);
  const [uploading, setUploading] = useState(false);
  const [uploadSuccess, setUploadSuccess] = useState(false);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [showEditForm, setShowEditForm] = useState(false);
  const [submittingToMentor, setSubmittingToMentor] = useState(false);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const currentUserId = user?.id?.toLowerCase();
  const currentUserEmail = user?.email?.toLowerCase();
  const isLeader =
    myGroup?.members.some((m) => {
      const roleMatched = m.role?.toLowerCase() === "leader";
      if (!roleMatched) return false;

      // Real login uses Firebase UID on client while API returns DB Guid for studentId,
      // so email is the most reliable identity match in UI.
      const idMatched = m.studentId?.toLowerCase() === currentUserId;
      const emailMatched = m.email?.toLowerCase() === currentUserEmail;
      return idMatched || emailMatched;
    }) ?? false;

  const stopPolling = useCallback(() => {
    if (pollRef.current) {
      clearInterval(pollRef.current);
      pollRef.current = null;
    }
  }, []);

  const loadDocuments = useCallback((projectId: string) => {
    topicPoolService
      .getTopicDocuments(projectId)
      .then(setDocuments)
      .catch(() => {
        /* silently fail — documents section just shows empty */
      });
  }, []);

  useEffect(() => {
    studentGroupService
      .getMyGroup()
      .then(async (group) => {
        setMyGroup(group);
        if (group?.projectId) {
          try {
            const detail = await topicPoolService.getTopicDetail(group.projectId);
            setTopicDetail(detail);
          } catch (err) {
            console.error("Error fetching topic detail:", err);
            showError("Không thể tải thông tin đề tài. Vui lòng thử lại sau.");
          }
          loadDocuments(group.projectId);
        }
      })
      .catch((err) => {
        console.error("Error fetching group:", err);
        showError("Không thể tải thông tin nhóm. Vui lòng thử lại sau.");
      })
      .finally(() => setLoading(false));
  }, []);

  const handleFiles = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files) return;
    const incoming = Array.from(e.target.files);
    const { accepted, rejected } = validateFiles(pendingFiles, incoming);
    if (accepted.length > 0) setPendingFiles((prev) => [...prev, ...accepted]);
    setFileWarnings(rejected);
    e.target.value = "";
  };

  const removePendingFile = (idx: number) => {
    setPendingFiles((prev) => prev.filter((_, i) => i !== idx));
    setFileWarnings([]);
  };

  const handleUpload = async () => {
    if (pendingFiles.length === 0 || !myGroup?.projectId) return;
    setUploading(true);
    try {
      await topicPoolService.uploadTopicDocuments(myGroup.projectId, pendingFiles);
      setPendingFiles([]);
      setFileWarnings([]);
      setUploadSuccess(true);

      // Poll every 3s for up to 30s until new documents appear or error notification arrives
      const prevCount = documents.length;
      const projectId = myGroup.projectId!;
      stopPolling();
      let elapsed = 0;
      pollRef.current = setInterval(async () => {
        elapsed += 3000;
        try {
          const [docs, notifs] = await Promise.all([
            topicPoolService.getTopicDocuments(projectId),
            apiClient.get<{
              items: Array<{ type: string; content: string; category: string; isRead: boolean; createdAt: string }>;
            }>("/api/notifications?limit=5"),
          ]);
          setDocuments(docs);

          // Check if backend sent an error notification about this upload (within last 60s)
          const recentError = notifs.items.find(
            (n) =>
              n.type === "Error" &&
              n.category === "Project" &&
              !n.isRead &&
              Date.now() - new Date(n.createdAt).getTime() < 60_000,
          );
          if (recentError) {
            stopPolling();
            setUploadSuccess(false);
            showError(recentError.content);
            return;
          }

          if (docs.length > prevCount || elapsed >= 30000) {
            stopPolling();
            setUploadSuccess(false);
            if (elapsed >= 30000 && docs.length <= prevCount) {
              showError("File đang được xử lý. Vui lòng kiểm tra lại sau vài phút.");
            }
          }
        } catch {
          /* ignore poll errors */
        }
      }, 3000);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Tải lên thất bại. Vui lòng thử lại.");
    } finally {
      setUploading(false);
    }
  };

  // Cleanup polling on unmount
  useEffect(() => stopPolling, [stopPolling]);

  return (
    <>
      <Header
        variant="primary"
        title="Đề tài của tôi"
        searchPlaceholder="Tìm kiếm thông tin đề tài..."
        role="student"
      />

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-8">
        {loading ? (
          <div className="flex items-center justify-center h-64">
            <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-primary" />
          </div>
        ) : !myGroup ? (
          /* No group */
          <div className="flex flex-col items-center justify-center h-64 text-center gap-4">
            <span className="material-symbols-outlined text-5xl text-[#58698d]">group_add</span>
            <p className="text-[#101319] font-bold text-lg">Bạn chưa tham gia nhóm nào</p>
            <p className="text-[#58698d] text-sm">Tham gia hoặc tạo nhóm để được gán đề tài.</p>
            <button
              onClick={() => navigate("/student/groups")}
              className="px-5 py-2 bg-primary text-white rounded-lg text-sm font-bold hover:bg-primary-light transition-colors"
            >
              Quản lý nhóm
            </button>
          </div>
        ) : !myGroup.projectId ? (
          /* Has group but no project */
          showCreateForm ? (
            <div className="max-w-3xl mx-auto">
              <CreateDirectTopicForm
                groupId={myGroup.groupId}
                onCreated={() => {
                  setShowCreateForm(false);
                  setLoading(true);
                  studentGroupService
                    .getMyGroup()
                    .then(async (group) => {
                      setMyGroup(group);
                      if (group?.projectId) {
                        const detail = await topicPoolService.getTopicDetail(group.projectId);
                        setTopicDetail(detail);
                        loadDocuments(group.projectId);
                      }
                    })
                    .finally(() => setLoading(false));
                }}
                onCancel={() => setShowCreateForm(false)}
              />
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center h-64 text-center gap-4">
              <span className="material-symbols-outlined text-5xl text-[#58698d]">topic</span>
              <p className="text-[#101319] font-bold text-lg">
                Nhóm <span className="text-primary">{myGroup.groupName ?? myGroup.groupCode}</span> chưa có đề tài
              </p>
              <p className="text-[#58698d] text-sm">
                Đăng ký đề tài từ kho hoặc đề xuất đề tài mới cho giảng viên duyệt.
              </p>
              <div className="flex items-center gap-3">
                <button
                  onClick={() => navigate("/student/topics")}
                  className="px-5 py-2 bg-primary text-white rounded-lg text-sm font-bold hover:bg-primary-light transition-colors"
                >
                  Xem kho đề tài
                </button>
                {isLeader && (
                  <button
                    onClick={() => setShowCreateForm(true)}
                    className="px-5 py-2 border-2 border-primary text-primary rounded-lg text-sm font-bold hover:bg-primary/5 transition-colors flex items-center gap-1.5"
                  >
                    <span className="material-symbols-outlined text-lg">edit_note</span>
                    Đề xuất đề tài mới
                  </button>
                )}
              </div>
            </div>
          )
        ) : (
          /* Full view */
          <motion.div variants={container} initial="hidden" animate="show" className="flex flex-col gap-6">
            {/* Breadcrumb */}
            <motion.div variants={item} className="flex flex-col md:flex-row md:items-center justify-between gap-4">
              <div>
                <div className="flex items-center gap-2 text-sm text-[#58698d] mb-1">
                  <span>Hệ thống</span>
                  <span className="material-symbols-outlined text-sm">chevron_right</span>
                  <span className="text-primary font-semibold">Đề tài của tôi</span>
                </div>
                <h2 className="text-2xl font-bold text-[#101319]">Chi tiết Nội dung Đề tài</h2>
              </div>
            </motion.div>

            {/* Topic Header Card */}
            <motion.section
              variants={item}
              className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm overflow-hidden"
            >
              <div className="p-8 border-b border-[#e9ecf1]">
                <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
                  <div className="max-w-4xl">
                    <div className="flex items-center gap-2 mb-3 flex-wrap">
                      {myGroup.projectCode && (
                        <span className="bg-blue-100 text-primary px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider">
                          Mã đề tài: {myGroup.projectCode}
                        </span>
                      )}
                      <span
                        className={`px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider ${statusColor(myGroup.projectStatus)}`}
                      >
                        {statusLabel(myGroup.projectStatus)}
                      </span>
                      {/* Action buttons based on status */}
                      {isLeader &&
                        (myGroup.projectStatus === "Draft" || myGroup.projectStatus === "NeedsModification") && (
                          <>
                            <button
                              onClick={async () => {
                                if (!myGroup.projectId) return;
                                setSubmittingToMentor(true);
                                try {
                                  await directTopicService.submitToMentor(myGroup.groupId, myGroup.projectId);
                                  // Reload group data
                                  const group = await studentGroupService.getMyGroup();
                                  setMyGroup(group);
                                  if (group?.projectId) {
                                    const detail = await topicPoolService.getTopicDetail(group.projectId);
                                    setTopicDetail(detail);
                                  }
                                } catch (err) {
                                  showError(err instanceof Error ? err.message : "Không thể gửi đề tài.");
                                } finally {
                                  setSubmittingToMentor(false);
                                }
                              }}
                              disabled={submittingToMentor}
                              className="ml-2 px-4 py-1.5 bg-primary text-white rounded-full text-xs font-bold hover:bg-primary-light transition-colors disabled:opacity-50 flex items-center gap-1.5"
                            >
                              {submittingToMentor ? (
                                <div className="animate-spin rounded-full h-3 w-3 border-2 border-white border-t-transparent" />
                              ) : (
                                <span className="material-symbols-outlined text-sm">send</span>
                              )}
                              {myGroup.projectStatus === "Draft" ? "Gửi cho giảng viên" : "Gửi lại cho giảng viên"}
                            </button>
                            {myGroup.projectStatus === "NeedsModification" && (
                              <button
                                onClick={() => setShowEditForm(true)}
                                className="ml-2 px-4 py-1.5 bg-amber-500 text-white rounded-full text-xs font-bold hover:bg-amber-600 transition-colors flex items-center gap-1.5"
                              >
                                <span className="material-symbols-outlined text-sm">edit_note</span>
                                Chỉnh sửa
                              </button>
                            )}
                          </>
                        )}
                      {myGroup.projectStatus === "PendingMentorReview" && (
                        <span className="ml-2 px-3 py-1.5 bg-violet-50 text-violet-700 rounded-full text-xs font-semibold flex items-center gap-1.5">
                          <span className="material-symbols-outlined text-sm">hourglass_top</span>
                          Đang chờ giảng viên duyệt
                        </span>
                      )}
                    </div>
                    <h1 className="text-2xl font-extrabold text-[#101319] leading-tight mb-4">
                      {topicDetail?.nameVi ?? "—"}
                    </h1>
                    <div className="flex flex-wrap gap-6 text-sm">
                      {myGroup.mentorName && (
                        <div className="flex items-center gap-3">
                          <div className="h-10 w-10 rounded-full bg-gray-100 flex items-center justify-center text-primary">
                            <span className="material-symbols-outlined">person</span>
                          </div>
                          <div>
                            <p className="text-[#58698d] text-xs">Giảng viên hướng dẫn</p>
                            <p className="font-bold text-[#101319]">{myGroup.mentorName}</p>
                          </div>
                        </div>
                      )}
                      {topicDetail?.createdAt && (
                        <div className="flex items-center gap-3">
                          <div className="h-10 w-10 rounded-full bg-gray-100 flex items-center justify-center text-primary">
                            <span className="material-symbols-outlined">calendar_today</span>
                          </div>
                          <div>
                            <p className="text-[#58698d] text-xs">Ngày tạo đề tài</p>
                            <p className="font-bold text-[#101319]">{formatDate(topicDetail.createdAt)}</p>
                          </div>
                        </div>
                      )}
                      {topicDetail?.majorName && (
                        <div className="flex items-center gap-3">
                          <div className="h-10 w-10 rounded-full bg-gray-100 flex items-center justify-center text-primary">
                            <span className="material-symbols-outlined">school</span>
                          </div>
                          <div>
                            <p className="text-[#58698d] text-xs">Ngành</p>
                            <p className="font-bold text-[#101319]">{topicDetail.majorName}</p>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            </motion.section>

            {/* Main Content Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              {/* Left Column - Topic Details */}
              <motion.div variants={item} className="lg:col-span-2 space-y-6">
                <div className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm flex flex-col">
                  <div className="p-5 border-b border-[#e9ecf1] flex items-center justify-between">
                    <h3 className="font-bold text-[#101319] flex items-center gap-2">
                      <span className="material-symbols-outlined text-primary">description</span>
                      Mô tả chi tiết đề tài
                    </h3>
                  </div>
                  <div className="p-8 space-y-8">
                    {/* Description */}
                    {topicDetail?.description && (
                      <div>
                        <h4 className="text-sm font-bold text-primary uppercase tracking-wider mb-3 flex items-center gap-2">
                          <span className="w-1.5 h-6 bg-primary rounded-full" />
                          Tổng quan
                        </h4>
                        <p className="text-[#101319] text-sm leading-relaxed whitespace-pre-wrap pl-3.5">
                          {topicDetail.description}
                        </p>
                      </div>
                    )}

                    {/* Objective */}
                    {topicDetail?.objectives && (
                      <div>
                        <h4 className="text-sm font-bold text-primary uppercase tracking-wider mb-3 flex items-center gap-2">
                          <span className="w-1.5 h-6 bg-primary rounded-full" />
                          Mục tiêu đề tài
                        </h4>
                        <p className="text-[#101319] text-sm leading-relaxed whitespace-pre-wrap pl-3.5">
                          {topicDetail.objectives}
                        </p>
                      </div>
                    )}

                    {/* Scope & Technologies */}
                    {(topicDetail?.scope || topicDetail?.technologies) && (
                      <div>
                        <h4 className="text-sm font-bold text-primary uppercase tracking-wider mb-3 flex items-center gap-2">
                          <span className="w-1.5 h-6 bg-primary rounded-full" />
                          Phạm vi nghiên cứu
                        </h4>
                        <div className="pl-3.5 space-y-4">
                          {topicDetail?.scope && (
                            <p className="text-[#101319] text-sm leading-relaxed whitespace-pre-wrap">
                              {topicDetail.scope}
                            </p>
                          )}
                          {topicDetail?.technologies && (
                            <p className="text-[#101319] text-sm leading-relaxed whitespace-pre-wrap">
                              {topicDetail.technologies}
                            </p>
                          )}
                        </div>
                      </div>
                    )}

                    {/* Expected Results */}
                    {topicDetail?.expectedResults && (
                      <div>
                        <h4 className="text-sm font-bold text-primary uppercase tracking-wider mb-3 flex items-center gap-2">
                          <span className="w-1.5 h-6 bg-primary rounded-full" />
                          Kết quả dự kiến
                        </h4>
                        <p className="text-[#101319] text-sm leading-relaxed whitespace-pre-wrap pl-3.5">
                          {topicDetail.expectedResults}
                        </p>
                      </div>
                    )}

                    {/* Fallback if no detail content */}
                    {!topicDetail?.description &&
                      !topicDetail?.objectives &&
                      !topicDetail?.scope &&
                      !topicDetail?.expectedResults && (
                        <div className="flex flex-col items-center justify-center py-8 text-center">
                          <span className="material-symbols-outlined text-3xl text-[#58698d] mb-2">info</span>
                          <p className="text-[#58698d] text-sm">Chưa có thông tin mô tả cho đề tài này.</p>
                        </div>
                      )}
                  </div>
                </div>
              </motion.div>

              {/* Right Column - Files */}
              <motion.div variants={item} className="lg:col-span-1 space-y-6">
                <div className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm flex flex-col">
                  <div className="p-5 border-b border-[#e9ecf1] flex items-center justify-between">
                    <h3 className="font-bold text-[#101319] flex items-center gap-2">
                      <span className="material-symbols-outlined text-primary">cloud_upload</span>
                      Tài liệu đã tải lên
                    </h3>
                    {documents.length > 0 && (
                      <span className="text-xs text-[#58698d] bg-gray-100 px-2 py-0.5 rounded-full">
                        {documents.length} file
                      </span>
                    )}
                  </div>
                  <div className="p-4 flex flex-col gap-3">
                    {/* Upload success banner */}
                    {uploadSuccess && (
                      <div className="flex items-center gap-2 p-3 bg-green-50 border border-green-200 rounded-lg animate-pulse">
                        <div className="animate-spin rounded-full h-4 w-4 border-2 border-green-500 border-t-transparent shrink-0" />
                        <p className="text-xs text-green-700 font-medium">
                          Tải lên thành công! File đang được quét mã độc và sẽ xuất hiện sau vài giây...
                        </p>
                      </div>
                    )}

                    {/* Uploaded documents list */}
                    {documents.length > 0 ? (
                      <div className="flex flex-col gap-2 max-h-64 overflow-y-auto">
                        {documents.map((doc) => {
                          const ext = `.${doc.originalFileName.split(".").pop()?.toLowerCase() ?? ""}`;
                          const icon = FILE_ICON_MAP[ext] ?? "draft";
                          return (
                            <div
                              key={doc.id}
                              className="flex items-center gap-3 p-2.5 rounded-lg bg-gray-50 hover:bg-gray-100 transition-colors"
                            >
                              <span className="material-symbols-outlined text-primary text-xl shrink-0">{icon}</span>
                              <div className="flex-1 min-w-0">
                                <p className="text-xs font-semibold text-[#101319] truncate">{doc.originalFileName}</p>
                                <p className="text-[10px] text-[#58698d]">
                                  {formatFileSize(doc.fileSize)} · {formatDate(doc.uploadedAt)} · {doc.uploadedByName}
                                </p>
                              </div>
                            </div>
                          );
                        })}
                      </div>
                    ) : (
                      <div className="flex flex-col items-center justify-center py-6 text-center gap-2">
                        <span className="material-symbols-outlined text-3xl text-gray-300">folder_open</span>
                        <p className="text-xs text-[#58698d]">Chưa có tài liệu nào được tải lên.</p>
                      </div>
                    )}

                    {/* Pending files */}
                    {pendingFiles.length > 0 && (
                      <div className="flex flex-col gap-1.5 pt-2 border-t border-[#e9ecf1]">
                        <p className="text-[10px] font-bold text-[#58698d] uppercase tracking-wider">
                          Đang chờ tải lên
                        </p>
                        {pendingFiles.map((file, idx) => {
                          const ext = `.${file.name.split(".").pop()?.toLowerCase() ?? ""}`;
                          const icon = FILE_ICON_MAP[ext] ?? "draft";
                          return (
                            <div key={idx} className="flex items-center gap-2 p-2 rounded-lg bg-blue-50">
                              <span className="material-symbols-outlined text-primary text-lg shrink-0">{icon}</span>
                              <div className="flex-1 min-w-0">
                                <p className="text-xs font-medium text-[#101319] truncate">{file.name}</p>
                                <p className="text-[10px] text-[#58698d]">{formatFileSize(file.size)}</p>
                              </div>
                              <button
                                onClick={() => removePendingFile(idx)}
                                className="text-gray-400 hover:text-red-500 transition-colors shrink-0"
                              >
                                <span className="material-symbols-outlined text-lg">close</span>
                              </button>
                            </div>
                          );
                        })}
                      </div>
                    )}

                    {/* File warnings */}
                    {fileWarnings.length > 0 && (
                      <div className="p-2.5 bg-amber-50 border border-amber-200 rounded-lg">
                        {fileWarnings.map((w, i) => (
                          <p key={i} className="text-[10px] text-amber-700 flex items-start gap-1">
                            <span className="material-symbols-outlined text-xs mt-px shrink-0">warning</span>
                            {w}
                          </p>
                        ))}
                      </div>
                    )}

                    {/* Upload drop zone */}
                    <label
                      className="w-full mt-1 py-4 border-2 border-dashed border-gray-200 rounded-lg flex flex-col items-center justify-center gap-1 hover:border-primary hover:bg-primary/5 transition-all text-[#58698d] hover:text-primary cursor-pointer"
                      onDragOver={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                      }}
                      onDrop={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        const files = Array.from(e.dataTransfer.files);
                        const { accepted, rejected } = validateFiles(pendingFiles, files);
                        if (accepted.length > 0) setPendingFiles((prev) => [...prev, ...accepted]);
                        setFileWarnings(rejected);
                      }}
                    >
                      <span className="material-symbols-outlined text-2xl">cloud_upload</span>
                      <span className="text-xs font-bold">Kéo thả hoặc nhấn để chọn file</span>
                      <span className="text-[10px] text-[#58698d]">
                        Tối đa {MAX_ATTACHMENTS} file · {formatFileSize(MAX_FILE_SIZE_BYTES)}/file
                      </span>
                      <input
                        type="file"
                        className="hidden"
                        multiple
                        accept={ACCEPTED_TYPES.join(",")}
                        onChange={handleFiles}
                      />
                    </label>

                    {/* Upload button */}
                    {pendingFiles.length > 0 && (
                      <button
                        onClick={handleUpload}
                        disabled={uploading}
                        className="w-full py-2.5 bg-primary text-white rounded-lg text-sm font-bold hover:bg-primary-light transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
                      >
                        {uploading ? (
                          <>
                            <div className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent" />
                            Đang tải lên...
                          </>
                        ) : (
                          <>
                            <span className="material-symbols-outlined text-lg">upload</span>
                            Tải lên {pendingFiles.length} file
                          </>
                        )}
                      </button>
                    )}
                  </div>
                </div>

                {/* Group members */}
                {myGroup.members && myGroup.members.length > 0 && (
                  <div className="bg-white rounded-xl border border-[#e9ecf1] shadow-sm flex flex-col">
                    <div className="p-5 border-b border-[#e9ecf1]">
                      <h3 className="font-bold text-[#101319] flex items-center gap-2">
                        <span className="material-symbols-outlined text-primary">group</span>
                        Thành viên nhóm
                      </h3>
                    </div>
                    <div className="p-4 flex flex-col gap-2">
                      {myGroup.members.map((m) => (
                        <div
                          key={m.studentId}
                          className="flex items-center gap-3 p-2 rounded-lg hover:bg-gray-50 transition-colors"
                        >
                          <div className="h-9 w-9 rounded-full bg-primary/10 flex items-center justify-center text-primary font-bold text-sm shrink-0">
                            {m.fullName.charAt(0).toUpperCase()}
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-semibold text-[#101319] truncate">{m.fullName}</p>
                            <p className="text-xs text-[#58698d]">{m.studentCode ?? m.role}</p>
                          </div>
                          {m.role === "Leader" && (
                            <span className="text-xs font-bold text-primary bg-primary/10 px-2 py-0.5 rounded-full shrink-0">
                              Nhóm trưởng
                            </span>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </motion.div>
            </div>

            {/* Footer */}
            <footer className="mt-12 pt-6 border-t border-[#e9ecf1] flex flex-col md:flex-row justify-between items-center text-[#58698d] text-sm pb-8">
              <p>© 2025 University Thesis Management System.</p>
              <div className="flex gap-4 mt-2 md:mt-0">
                <a className="hover:text-primary" href="#">
                  Quy định bảo mật
                </a>
                <a className="hover:text-primary" href="#">
                  Điều khoản sử dụng
                </a>
              </div>
            </footer>
          </motion.div>
        )}
      </div>

      {/* Edit Topic Modal */}
      {showEditForm && myGroup?.projectId && topicDetail && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
          <div className="max-w-2xl w-full max-h-[90vh] overflow-y-auto">
            <EditDirectTopicForm
              projectId={myGroup.projectId}
              initialData={{
                nameVi: topicDetail.nameVi,
                nameEn: topicDetail.nameEn,
                nameAbbr: topicDetail.nameAbbr,
                description: topicDetail.description,
                objectives: topicDetail.objectives,
                scope: topicDetail.scope || undefined,
                technologies: topicDetail.technologies || undefined,
                expectedResults: topicDetail.expectedResults || undefined,
                maxStudents: topicDetail.maxStudents,
              }}
              onUpdated={() => {
                setShowEditForm(false);
                // Reload topic detail
                studentGroupService
                  .getMyGroup()
                  .then(async (group) => {
                    setMyGroup(group);
                    if (group?.projectId) {
                      const detail = await topicPoolService.getTopicDetail(group.projectId);
                      setTopicDetail(detail);
                    }
                  })
                  .catch((err) => {
                    console.error("Error reloading topic:", err);
                    showError("Không thể tải lại thông tin đề tài.");
                  });
              }}
              onCancel={() => setShowEditForm(false)}
            />
          </div>
        </div>
      )}
    </>
  );
}
