import { useState, useEffect, useMemo } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Header } from "@/components/layout";
import { SuccessModal } from "@/components/common/SuccessModal";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { studentGroupService } from "@/lib";
import type {
  AvailableStudentDto,
  InvitationDto,
  JoinRequestDto,
  OpenGroupDto,
  PendingJoinRequestDto,
  StudentGroupDto,
} from "@/types";

// ── Types ──────────────────────────────────────────────────────────

type TabKey = "my-group" | "open-groups" | "invitations";

const tabs: { key: TabKey; label: string; icon: string }[] = [
  { key: "my-group", label: "Nhóm của tôi", icon: "group" },
  { key: "open-groups", label: "Nhóm khác", icon: "groups" },
  { key: "invitations", label: "Lời mời", icon: "mail" },
];

const PAGE_SIZE = 9;

// ── Main Page ──────────────────────────────────────────────────────

export function StudentGroupPage() {
  const [activeTab, setActiveTab] = useState<TabKey>("my-group");

  return (
    <div className="flex flex-col h-screen bg-gray-50">
      <Header variant="primary" title="Quản lý nhóm" showSearch={false} role="student" />

      {/* Tab Bar */}
      <div className="px-8 bg-white border-b border-gray-200">
        <div className="flex gap-1">
          {tabs.map((tab) => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              className={`relative flex items-center gap-2 px-5 py-3 text-sm font-medium transition-colors ${
                activeTab === tab.key ? "text-primary" : "text-gray-500 hover:text-gray-700"
              }`}
            >
              <span className={`material-symbols-outlined text-[18px] ${activeTab === tab.key ? "fill-1" : ""}`}>
                {tab.icon}
              </span>
              {tab.label}
              {activeTab === tab.key && (
                <motion.div
                  layoutId="tab-underline"
                  className="absolute bottom-0 left-0 right-0 h-0.5 bg-primary rounded-full"
                  transition={{ type: "spring", stiffness: 500, damping: 35 }}
                />
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Tab Content */}
      <main className="flex-1 p-8 overflow-y-auto">
        <AnimatePresence mode="wait">
          <motion.div
            key={activeTab}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -8 }}
            transition={{ duration: 0.15 }}
          >
            {activeTab === "my-group" && <MyGroupContent />}
            {activeTab === "open-groups" && <OpenGroupsContent />}
            {activeTab === "invitations" && <InvitationsContent />}
          </motion.div>
        </AnimatePresence>
      </main>
    </div>
  );
}

// ── Tab 1: Nhóm của tôi ────────────────────────────────────────────

function MyGroupContent() {
  const { showError } = useSystemError();
  const [myGroup, setMyGroup] = useState<StudentGroupDto | null>(null);
  const [joinRequests, setJoinRequests] = useState<JoinRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [inviteData, setInviteData] = useState({ studentCode: "", message: "" });
  const [inviting, setInviting] = useState(false);
  const [creatingGroup, setCreatingGroup] = useState(false);
  const [groupName, setGroupName] = useState("");
  const [pendingJoinRequest, setPendingJoinRequest] = useState<PendingJoinRequestDto | null>(null);
  const [showSuccessModal, setShowSuccessModal] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [selectedJoinRequests, setSelectedJoinRequests] = useState<Set<number>>(new Set());
  const [bulkProcessing, setBulkProcessing] = useState(false);

  // Invite picker (students not yet in a group)
  const [invitableStudents, setInvitableStudents] = useState<AvailableStudentDto[]>([]);
  const [loadingStudents, setLoadingStudents] = useState(false);
  const [studentSearch, setStudentSearch] = useState("");
  const [showStudentDropdown, setShowStudentDropdown] = useState(false);

  useEffect(() => {
    fetchGroupData();
  }, []);

  // Load the invitable-students list whenever the invite modal opens.
  useEffect(() => {
    if (!showInviteModal || !myGroup) return;
    setLoadingStudents(true);
    setStudentSearch("");
    setInviteData({ studentCode: "", message: "" });
    studentGroupService
      .getInvitableStudents(myGroup.groupId)
      .then(setInvitableStudents)
      .catch(() => setInvitableStudents([]))
      .finally(() => setLoadingStudents(false));
  }, [showInviteModal, myGroup]);

  const filteredStudents = useMemo(() => {
    const q = studentSearch.trim().toLowerCase();
    const list = q
      ? invitableStudents.filter((s) => s.studentCode.toLowerCase().includes(q) || s.fullName.toLowerCase().includes(q))
      : invitableStudents;
    return list.slice(0, 50);
  }, [invitableStudents, studentSearch]);

  const selectStudent = (s: AvailableStudentDto) => {
    setInviteData((prev) => ({ ...prev, studentCode: s.studentCode }));
    setStudentSearch(`${s.studentCode} - ${s.fullName}`);
    setShowStudentDropdown(false);
  };

  const closeInviteModal = () => {
    setShowInviteModal(false);
    setStudentSearch("");
    setShowStudentDropdown(false);
    setInviteData({ studentCode: "", message: "" });
  };

  const fetchGroupData = async () => {
    try {
      setLoading(true);
      const [group, pendingRequest] = await Promise.all([
        studentGroupService.getMyGroup(),
        studentGroupService.getMyPendingJoinRequest(),
      ]);
      setPendingJoinRequest(pendingRequest);

      if (group && group.groupId) {
        setMyGroup(group);
        try {
          const requests = await studentGroupService.getJoinRequests(group.groupId);
          setJoinRequests(requests);
        } catch {
          setJoinRequests([]);
        }
      } else {
        setMyGroup(null);
        setJoinRequests([]);
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : "Có lỗi khi tải dữ liệu");
    } finally {
      setLoading(false);
    }
  };

  const handleCreateGroup = async () => {
    if (pendingJoinRequest) {
      showError("Bạn đang có yêu cầu tham gia chờ phê duyệt, chưa thể tạo nhóm mới.");
      return;
    }
    try {
      setCreatingGroup(true);
      const result = await studentGroupService.createGroup(groupName || undefined);
      if (result.id) {
        fetchGroupData();
        setGroupName("");
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể tạo nhóm");
    } finally {
      setCreatingGroup(false);
    }
  };

  const handleInviteMember = async () => {
    if (!myGroup || !inviteData.studentCode) return;
    const invitedCode = inviteData.studentCode;
    try {
      setInviting(true);
      await studentGroupService.inviteMember(myGroup.groupId, invitedCode, inviteData.message || undefined);
      closeInviteModal();
      setSuccessMessage(`Đã mời sinh viên ${invitedCode.toUpperCase()} thành công. Hãy chờ phản hồi từ họ.`);
      setShowSuccessModal(true);
      fetchGroupData();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể gửi lời mời");
    } finally {
      setInviting(false);
    }
  };

  const handleApproveRequest = async (requestId: number) => {
    if (!myGroup) return;
    try {
      await studentGroupService.approveJoinRequest(myGroup.groupId, requestId);
      fetchGroupData();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể chấp nhận yêu cầu");
    }
  };

  const handleRejectRequest = async (requestId: number) => {
    if (!myGroup) return;
    try {
      await studentGroupService.rejectJoinRequest(myGroup.groupId, requestId);
      fetchGroupData();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể từ chối yêu cầu");
    }
  };

  const handleSelectAllRequests = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.checked) {
      const allIds = joinRequests.map((r) => r.id);
      setSelectedJoinRequests(new Set(allIds));
    } else {
      setSelectedJoinRequests(new Set());
    }
  };

  const handleSelectRequest = (requestId: number, checked: boolean) => {
    const newSelected = new Set(selectedJoinRequests);
    if (checked) {
      newSelected.add(requestId);
    } else {
      newSelected.delete(requestId);
    }
    setSelectedJoinRequests(newSelected);
  };

  const handleBulkApproveRequests = async () => {
    if (!myGroup || selectedJoinRequests.size === 0) return;

    try {
      setBulkProcessing(true);
      const result = await studentGroupService.bulkApproveJoinRequests(myGroup.groupId, Array.from(selectedJoinRequests));
      setSuccessMessage(result.message);
      setShowSuccessModal(true);
      setSelectedJoinRequests(new Set());
      fetchGroupData();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Có lỗi xảy ra khi chấp nhận hàng loạt");
    } finally {
      setBulkProcessing(false);
    }
  };

  const handleBulkRejectRequests = async () => {
    if (!myGroup || selectedJoinRequests.size === 0) return;
    try {
      setBulkProcessing(true);
      const result = await studentGroupService.bulkRejectJoinRequests(myGroup.groupId, Array.from(selectedJoinRequests));
      setSuccessMessage(result.message);
      setShowSuccessModal(true);
      setSelectedJoinRequests(new Set());
      fetchGroupData();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Có lỗi xảy ra khi từ chối hàng loạt");
    } finally {
      setBulkProcessing(false);
    }
  };

  const availableSlots = myGroup ? myGroup.maxMembers - (myGroup.members?.length || 0) : 0;

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-gray-500">Đang tải...</div>
      </div>
    );
  }

  if (!myGroup) {
    return (
      <>
        <div className="max-w-2xl mx-auto">
          <div className="p-8 text-center bg-white border border-gray-200 rounded-lg shadow-sm">
            <div className="mb-4 text-5xl">👥</div>
            <h2 className="mb-2 text-2xl font-bold text-gray-800">Bạn chưa có nhóm</h2>
            <p className="mb-6 text-gray-500">Tạo nhóm của bạn ngay để bắt đầu hợp tác với những sinh viên khác</p>

            {pendingJoinRequest && (
              <div className="px-4 py-3 mb-6 text-left border rounded-lg bg-amber-50 border-amber-200 text-amber-800">
                <p className="font-semibold">Đang chờ phê duyệt yêu cầu tham gia nhóm</p>
                <p className="mt-1 text-sm">
                  Nhóm: {pendingJoinRequest.groupName || pendingJoinRequest.groupCode} • Hết hạn lúc{" "}
                  {new Date(pendingJoinRequest.expiresAt).toLocaleString("vi-VN")}
                </p>
              </div>
            )}

            <div className="mb-6 space-y-4">
              <div>
                <label className="block mb-2 text-sm font-medium text-gray-700">Tên nhóm (tùy chọn)</label>
                <input
                  type="text"
                  value={groupName}
                  onChange={(e) => setGroupName(e.target.value)}
                  placeholder="VD: Team TEDF..."
                  maxLength={100}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
            </div>

            <button
              onClick={handleCreateGroup}
              disabled={creatingGroup || !!pendingJoinRequest}
              className="w-full px-6 py-3 font-semibold text-white transition rounded-lg bg-primary hover:bg-primary/90 disabled:opacity-50"
            >
              {pendingJoinRequest ? "Đang chờ phê duyệt yêu cầu" : creatingGroup ? "Đang tạo..." : "Tạo nhóm mới"}
            </button>
          </div>
        </div>
      </>
    );
  }

  return (
    <>
      <div className="max-w-4xl mx-auto space-y-6">
        {/* Group Info Card */}
        <div className="p-6 bg-white border border-gray-200 rounded-lg shadow-sm">
          <div className="flex items-start justify-between mb-6">
            <div>
              <h2 className="mb-1 text-2xl font-bold text-gray-800">{myGroup.groupName || myGroup.groupCode}</h2>
              <p className="text-sm text-gray-500">Mã nhóm: {myGroup.groupCode}</p>
              {myGroup.projectName && (
                <p className="mt-1 text-sm text-gray-600">
                  <span className="material-symbols-outlined text-[16px] align-text-bottom mr-1">description</span>
                  Đề tài: {myGroup.projectName}
                </p>
              )}
            </div>
            <span
              className={`px-4 py-2 rounded-full text-sm font-semibold ${myGroup.isOpenForRequests ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800"}`}
            >
              {myGroup.isOpenForRequests ? "Mở" : "Kín"}
            </span>
          </div>

          <div className="grid grid-cols-2 gap-4 p-4 mb-6 rounded-lg bg-gray-50">
            <div>
              <p className="text-sm text-gray-600">Số thành viên</p>
              <p className="text-2xl font-bold text-gray-800">
                {myGroup.members?.length ?? 0}/{myGroup.maxMembers}
              </p>
            </div>
            <div>
              <p className="text-sm text-gray-600">Ngày tạo</p>
              <p className="text-lg font-semibold text-gray-800">
                {new Date(myGroup.createdAt).toLocaleDateString("vi-VN")}
              </p>
            </div>
          </div>

          <button
            onClick={() => setShowInviteModal(true)}
            className="w-full px-4 py-3 font-semibold text-blue-700 transition rounded-lg bg-blue-50 hover:bg-blue-100"
          >
            <span className="material-symbols-outlined text-[18px] align-text-bottom mr-1">person_add</span>
            Mời thành viên mới
          </button>
        </div>

        {/* Members List */}
        <div className="p-6 bg-white border border-gray-200 rounded-lg shadow-sm">
          <h3 className="mb-4 text-lg font-bold text-gray-800">Thành viên nhóm</h3>
          <div className="space-y-3">
            {myGroup.members && myGroup.members.length > 0 ? (
              myGroup.members.map((member) => (
                <div
                  key={member.studentId}
                  className="flex items-center justify-between p-3 border border-gray-200 rounded-lg hover:bg-gray-50"
                >
                  <div>
                    <p className="font-semibold text-gray-800">{member.fullName}</p>
                    <p className="text-sm text-gray-500">
                      {member.studentCode} • {member.email}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-medium text-gray-600">{member.role}</p>
                    <p className="text-xs text-gray-500">
                      Tham gia: {new Date(member.joinedAt).toLocaleDateString("vi-VN")}
                    </p>
                  </div>
                </div>
              ))
            ) : (
              <p className="py-4 text-center text-gray-500">Chưa có thành viên nào</p>
            )}
          </div>
        </div>

        {/* Join Requests */}
        {joinRequests.length > 0 && (
          <div className="p-6 bg-white border border-gray-200 rounded-lg shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-3">
                <input
                  type="checkbox"
                  disabled={joinRequests.length > availableSlots}
                  checked={selectedJoinRequests.size === joinRequests.length && joinRequests.length > 0}
                  onChange={handleSelectAllRequests}
                  className="w-4 h-4 text-blue-600 rounded border-gray-300 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                />
                <h3 className="text-lg font-bold text-gray-800">Yêu cầu tham gia ({joinRequests.length})</h3>
              </div>
              {selectedJoinRequests.size > 0 && (
                <div className="flex gap-2">
                  <button
                    disabled={bulkProcessing}
                    onClick={handleBulkRejectRequests}
                    className="px-3 py-1.5 text-sm font-semibold text-red-700 transition rounded-lg bg-red-50 hover:bg-red-100 disabled:opacity-50"
                  >
                    Từ chối đã chọn ({selectedJoinRequests.size})
                  </button>
                  <button
                    disabled={bulkProcessing}
                    onClick={handleBulkApproveRequests}
                    className="px-3 py-1.5 text-sm font-semibold text-green-700 transition rounded-lg bg-green-50 hover:bg-green-100 disabled:opacity-50"
                  >
                    Chấp nhận đã chọn ({selectedJoinRequests.size})
                  </button>
                </div>
              )}
            </div>
            <div className="space-y-3">
              {joinRequests.map((request) => (
                <div key={request.id} className="p-4 border border-yellow-200 rounded-lg bg-yellow-50 flex gap-4">
                  <div className="pt-1">
                    <input
                      type="checkbox"
                      disabled={!selectedJoinRequests.has(request.id) && selectedJoinRequests.size >= availableSlots}
                      checked={selectedJoinRequests.has(request.id)}
                      onChange={(e) => handleSelectRequest(request.id, e.target.checked)}
                      className="w-4 h-4 text-blue-600 rounded border-gray-300 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                    />
                  </div>
                  <div className="flex-1">
                    <div className="flex items-start justify-between mb-3">
                      <div>
                        <p className="font-semibold text-gray-800">{request.studentName}</p>
                        <p className="text-sm text-gray-600">{request.studentCode}</p>
                        {request.message && <p className="mt-2 text-sm italic text-gray-700">"{request.message}"</p>}
                      </div>
                      <span className="text-xs text-gray-500">
                        {new Date(request.createdAt).toLocaleDateString("vi-VN")}
                      </span>
                    </div>
                    <div className="flex justify-end gap-2">
                    <button
                      onClick={() => handleRejectRequest(request.id)}
                      className="px-4 py-2 text-sm font-semibold text-red-700 transition rounded-lg bg-red-50 hover:bg-red-100"
                    >
                      Từ chối
                    </button>
                    <button
                      onClick={() => handleApproveRequest(request.id)}
                      className="px-4 py-2 text-sm font-semibold text-green-700 transition rounded-lg bg-green-50 hover:bg-green-100"
                    >
                      Chấp nhận
                    </button>
                  </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Invite Modal */}
      {showInviteModal && myGroup && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="w-full max-w-md p-6 bg-white rounded-lg shadow-lg">
            <h3 className="mb-4 text-xl font-bold text-gray-800">Mời thành viên mới</h3>
            <div className="mb-6 space-y-4">
              <div className="relative">
                <label className="block mb-2 text-sm font-medium text-gray-700">Chọn sinh viên</label>
                <input
                  type="text"
                  value={studentSearch}
                  onChange={(e) => {
                    setStudentSearch(e.target.value);
                    setInviteData((prev) => ({ ...prev, studentCode: "" }));
                    setShowStudentDropdown(true);
                  }}
                  onFocus={() => setShowStudentDropdown(true)}
                  onBlur={() => setTimeout(() => setShowStudentDropdown(false), 150)}
                  placeholder="Tìm theo mã hoặc tên sinh viên..."
                  autoComplete="off"
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
                {showStudentDropdown && (
                  <div className="absolute z-10 w-full mt-1 overflow-y-auto bg-white border border-gray-200 rounded-lg shadow-lg max-h-60">
                    {loadingStudents ? (
                      <div className="px-4 py-3 text-sm text-gray-500">Đang tải danh sách sinh viên...</div>
                    ) : filteredStudents.length === 0 ? (
                      <div className="px-4 py-3 text-sm text-gray-500">Không tìm thấy sinh viên phù hợp.</div>
                    ) : (
                      filteredStudents.map((s) => (
                        <button
                          key={s.studentId}
                          type="button"
                          onMouseDown={(e) => e.preventDefault()}
                          onClick={() => selectStudent(s)}
                          className={`w-full text-left px-4 py-2 text-sm hover:bg-primary/10 transition ${
                            inviteData.studentCode === s.studentCode ? "bg-primary/10" : ""
                          }`}
                        >
                          <span className="font-semibold text-gray-800">{s.studentCode}</span>
                          <span className="text-gray-500"> - {s.fullName}</span>
                        </button>
                      ))
                    )}
                  </div>
                )}
              </div>
              <div>
                <label className="block mb-2 text-sm font-medium text-gray-700">Lời nhắn (tùy chọn)</label>
                <textarea
                  value={inviteData.message}
                  onChange={(e) => setInviteData({ ...inviteData, message: e.target.value })}
                  placeholder="Viết lời mời..."
                  maxLength={500}
                  rows={3}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
            </div>
            <div className="flex gap-3">
              <button
                onClick={closeInviteModal}
                className="flex-1 px-4 py-2 font-semibold text-gray-700 transition bg-gray-100 rounded-lg hover:bg-gray-200"
              >
                Hủy
              </button>
              <button
                onClick={handleInviteMember}
                disabled={!inviteData.studentCode || inviting}
                className="flex-1 px-4 py-2 font-semibold text-white transition rounded-lg bg-primary hover:bg-primary/90 disabled:opacity-50"
              >
                {inviting ? "Đang gửi..." : "Gửi lời mời"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Success Modal */}
      <SuccessModal
        isOpen={showSuccessModal}
        onClose={() => setShowSuccessModal(false)}
        title="Mời thành viên thành công"
        message={successMessage}
        icon="person_add"
        autoClose={3000}
      />
    </>
  );
}

// ── Tab 2: Nhóm khác ───────────────────────────────────────────────

function OpenGroupsContent() {
  const { showError } = useSystemError();
  const [openGroups, setOpenGroups] = useState<OpenGroupDto[]>([]);
  const [myGroup, setMyGroup] = useState<{ groupCode: string; groupName?: string } | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedGroup, setSelectedGroup] = useState<OpenGroupDto | null>(null);
  const [showRequestModal, setShowRequestModal] = useState(false);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [requestMessage, setRequestMessage] = useState("");
  const [requesting, setRequesting] = useState(false);
  const [pendingJoinRequest, setPendingJoinRequest] = useState<PendingJoinRequestDto | null>(null);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [currentPage, setCurrentPage] = useState(1);

  useEffect(() => {
    fetchOpenGroups();
  }, []);

  const fetchOpenGroups = async () => {
    try {
      setLoading(true);
      const [groups, pending, currentGroup] = await Promise.all([
        studentGroupService.getOpenGroups(),
        studentGroupService.getMyPendingJoinRequest(),
        studentGroupService.getMyGroup(),
      ]);
      setOpenGroups(groups);
      setPendingJoinRequest(pending);
      setMyGroup(currentGroup ? { groupCode: currentGroup.groupCode, groupName: currentGroup.groupName } : null);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Có lỗi khi tải dữ liệu");
    } finally {
      setLoading(false);
    }
  };

  const hasGroupInCurrentSemester = !!myGroup;

  // Client-side search + pagination
  const filteredGroups = useMemo(() => {
    if (!searchQuery.trim()) return openGroups;
    const q = searchQuery.toLowerCase().trim();
    return openGroups.filter(
      (g) => (g.groupName || "").toLowerCase().includes(q) || g.groupCode.toLowerCase().includes(q),
    );
  }, [openGroups, searchQuery]);

  const totalPages = Math.max(1, Math.ceil(filteredGroups.length / PAGE_SIZE));
  const paginatedGroups = filteredGroups.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  // Reset page khi search thay đổi
  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery]);

  const handleRequestJoin = async () => {
    if (!selectedGroup || pendingJoinRequest) return;
    try {
      setRequesting(true);
      await studentGroupService.requestJoin(selectedGroup.groupId, requestMessage || undefined);
      setShowRequestModal(false);
      setRequestMessage("");
      setSelectedGroup(null);
      setToastMessage("Gửi yêu cầu thành công, hãy chờ nhóm trưởng phê duyệt yêu cầu của bạn");
      setTimeout(() => setToastMessage(null), 4000);
      await fetchOpenGroups();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể gửi yêu cầu");
    } finally {
      setRequesting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-gray-500">Đang tải dữ liệu...</div>
      </div>
    );
  }

  return (
    <>
      {toastMessage && (
        <div className="fixed z-50 max-w-md px-5 py-3 text-white bg-green-600 rounded-lg shadow-lg top-6 right-6">
          {toastMessage}
        </div>
      )}

      {pendingJoinRequest && (
        <div className="px-4 py-3 mb-6 border rounded-lg bg-amber-50 border-amber-200 text-amber-800">
          <p className="font-semibold">Bạn đang chờ phê duyệt yêu cầu tham gia nhóm</p>
          <p className="mt-1 text-sm">
            Nhóm: {pendingJoinRequest.groupName || pendingJoinRequest.groupCode} • Hết hạn lúc{" "}
            {new Date(pendingJoinRequest.expiresAt).toLocaleString("vi-VN")}
          </p>
        </div>
      )}

      {hasGroupInCurrentSemester && (
        <div className="px-4 py-3 mb-6 border rounded-lg bg-slate-100 border-slate-300 text-slate-700">
          <p className="font-semibold">Bạn đã có nhóm ở kỳ này</p>
          <p className="mt-1 text-sm">Nhóm hiện tại: {myGroup!.groupName || myGroup!.groupCode}</p>
        </div>
      )}

      {/* Search Bar */}
      <div className="mb-6">
        <div className="relative max-w-md">
          <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-[20px]">
            search
          </span>
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Tìm kiếm theo tên hoặc mã nhóm..."
            className="w-full pl-10 pr-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary"
          />
          {searchQuery && (
            <button
              onClick={() => setSearchQuery("")}
              className="absolute text-gray-400 -translate-y-1/2 right-3 top-1/2 hover:text-gray-600"
            >
              <span className="material-symbols-outlined text-[18px]">close</span>
            </button>
          )}
        </div>
        {searchQuery && <p className="mt-2 text-sm text-gray-500">Tìm thấy {filteredGroups.length} nhóm</p>}
      </div>

      {filteredGroups.length === 0 ? (
        <div className="max-w-2xl mx-auto">
          <div className="p-8 text-center bg-white border border-gray-200 rounded-lg shadow-sm">
            <div className="mb-4 text-5xl">{searchQuery ? "🔍" : "🔍"}</div>
            <h2 className="mb-2 text-2xl font-bold text-gray-800">
              {searchQuery ? "Không tìm thấy nhóm nào" : "Không có nhóm mở"}
            </h2>
            <p className="text-gray-500">
              {searchQuery
                ? "Thử tìm kiếm với từ khóa khác."
                : "Hiện tại không có nhóm mới để tham gia. Hãy kiểm tra lại sau hoặc tạo nhóm của riêng bạn."}
            </p>
          </div>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
            {paginatedGroups.map((group) => (
              <div
                key={group.groupId}
                onClick={() => {
                  setSelectedGroup(group);
                  setShowDetailModal(true);
                }}
                className="p-6 transition bg-white border border-gray-200 rounded-lg shadow-sm cursor-pointer hover:shadow-md hover:border-primary/30"
              >
                <div className="mb-4">
                  <h3 className="mb-1 text-lg font-bold text-gray-800">{group.groupName || group.groupCode}</h3>
                  <p className="text-sm text-gray-600">Mã: {group.groupCode}</p>
                </div>

                <div className="p-3 mb-4 rounded-lg bg-blue-50">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-700">Số thành viên</span>
                    <span className="text-xl font-bold text-blue-600">
                      {group.memberCount}/{group.maxMembers}
                    </span>
                  </div>
                  <div className="w-full h-2 mt-2 bg-gray-200 rounded-full">
                    <div
                      className="h-2 transition-all bg-blue-600 rounded-full"
                      style={{ width: `${(group.memberCount / group.maxMembers) * 100}%` }}
                    />
                  </div>
                </div>

                {group.members && group.members.length > 0 && (
                  <div className="mb-4">
                    <p className="mb-2 text-sm font-semibold text-gray-700">Thành viên hiện tại:</p>
                    <div className="space-y-1">
                      {group.members.slice(0, 3).map((member) => (
                        <div key={member.studentId} className="text-sm text-gray-600">
                          <span className="font-medium">{member.fullName}</span>
                          <span className="text-gray-500"> ({member.studentCode})</span>
                        </div>
                      ))}
                      {group.members.length > 3 && (
                        <p className="text-sm italic text-gray-500">...và {group.members.length - 3} người khác</p>
                      )}
                    </div>
                  </div>
                )}

                <p className="text-xs text-gray-500">
                  Tạo ngày: {new Date(group.createdAt).toLocaleDateString("vi-VN")}
                </p>
              </div>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-center gap-2 mt-8">
              <button
                onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                disabled={currentPage === 1}
                className="px-3 py-2 text-sm font-medium transition border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
              >
                <span className="material-symbols-outlined text-[18px]">chevron_left</span>
              </button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                <button
                  key={page}
                  onClick={() => setCurrentPage(page)}
                  className={`w-10 h-10 rounded-lg text-sm font-medium transition ${
                    currentPage === page ? "bg-primary text-white" : "border border-gray-300 hover:bg-gray-50"
                  }`}
                >
                  {page}
                </button>
              ))}
              <button
                onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                disabled={currentPage === totalPages}
                className="px-3 py-2 text-sm font-medium transition border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
              >
                <span className="material-symbols-outlined text-[18px]">chevron_right</span>
              </button>
            </div>
          )}
        </>
      )}

      {/* Group Detail Modal */}
      {showDetailModal && selectedGroup && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50"
          onClick={() => setShowDetailModal(false)}
        >
          <div className="w-full max-w-lg p-6 bg-white rounded-lg shadow-lg" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-start justify-between mb-6">
              <div>
                <h3 className="text-xl font-bold text-gray-800">
                  {selectedGroup.groupName || selectedGroup.groupCode}
                </h3>
                <p className="text-sm text-gray-500">Mã nhóm: {selectedGroup.groupCode}</p>
              </div>
              <button onClick={() => setShowDetailModal(false)} className="text-gray-400 hover:text-gray-600">
                <span className="material-symbols-outlined">close</span>
              </button>
            </div>

            <div className="grid grid-cols-2 gap-4 p-4 mb-6 rounded-lg bg-gray-50">
              <div>
                <p className="text-sm text-gray-600">Số thành viên</p>
                <p className="text-2xl font-bold text-gray-800">
                  {selectedGroup.memberCount}/{selectedGroup.maxMembers}
                </p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Ngày tạo</p>
                <p className="text-lg font-semibold text-gray-800">
                  {new Date(selectedGroup.createdAt).toLocaleDateString("vi-VN")}
                </p>
              </div>
            </div>

            {/* Full Members List */}
            <div className="mb-6">
              <h4 className="mb-3 text-sm font-bold text-gray-700">Danh sách thành viên</h4>
              <div className="space-y-2">
                {selectedGroup.members.map((member) => (
                  <div
                    key={member.studentId}
                    className="flex items-center justify-between p-3 border border-gray-200 rounded-lg"
                  >
                    <div>
                      <p className="font-semibold text-gray-800">{member.fullName}</p>
                      <p className="text-sm text-gray-500">{member.studentCode}</p>
                    </div>
                    <span className="px-2 py-1 text-xs font-medium text-gray-500 bg-gray-100 rounded">
                      {member.role}
                    </span>
                  </div>
                ))}
              </div>
            </div>

            <button
              onClick={() => {
                setShowDetailModal(false);
                if (!pendingJoinRequest && !hasGroupInCurrentSemester) {
                  setShowRequestModal(true);
                }
              }}
              disabled={!!pendingJoinRequest || hasGroupInCurrentSemester}
              className="w-full px-4 py-3 font-semibold text-white transition rounded-lg bg-primary hover:bg-primary/90 disabled:bg-gray-300 disabled:cursor-not-allowed"
            >
              {hasGroupInCurrentSemester
                ? "Bạn đã có nhóm ở kỳ này"
                : pendingJoinRequest
                  ? "Đang chờ phê duyệt"
                  : "Yêu cầu tham gia"}
            </button>
          </div>
        </div>
      )}

      {/* Request Join Modal */}
      {showRequestModal && selectedGroup && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="w-full max-w-md p-6 bg-white rounded-lg shadow-lg">
            <h3 className="mb-2 text-xl font-bold text-gray-800">Yêu cầu tham gia nhóm</h3>
            <p className="mb-4 text-gray-600">{selectedGroup.groupName || selectedGroup.groupCode}</p>

            <div className="mb-6">
              <label className="block mb-2 text-sm font-medium text-gray-700">Lời nhắn (tùy chọn)</label>
              <textarea
                value={requestMessage}
                onChange={(e) => setRequestMessage(e.target.value)}
                placeholder="Giới thiệu về bản thân hoặc để trống..."
                maxLength={500}
                rows={4}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
              <p className="mt-1 text-xs text-gray-500">{requestMessage.length}/500</p>
            </div>

            <div className="flex gap-3">
              <button
                onClick={() => {
                  setShowRequestModal(false);
                  setRequestMessage("");
                  setSelectedGroup(null);
                }}
                className="flex-1 px-4 py-2 font-semibold text-gray-700 transition bg-gray-100 rounded-lg hover:bg-gray-200"
              >
                Hủy
              </button>
              <button
                onClick={handleRequestJoin}
                disabled={requesting}
                className="flex-1 px-4 py-2 font-semibold text-white transition rounded-lg bg-primary hover:bg-primary/90 disabled:opacity-50"
              >
                {requesting ? "Đang gửi..." : "Gửi yêu cầu"}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

// ── Tab 3: Lời mời ─────────────────────────────────────────────────

function InvitationsContent() {
  const { showError } = useSystemError();
  const [invitations, setInvitations] = useState<InvitationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [acting, setActing] = useState<number | null>(null);
  const [showSuccessModal, setShowSuccessModal] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    fetchInvitations();
  }, []);

  const fetchInvitations = async () => {
    try {
      setLoading(true);
      const data = await studentGroupService.getMyInvitations();
      setInvitations(data);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Có lỗi khi tải dữ liệu");
    } finally {
      setLoading(false);
    }
  };

  const handleAccept = async (groupId: string, invitationId: number) => {
    try {
      setActing(invitationId);
      const invitation = invitations.find((i) => i.id === invitationId);
      await studentGroupService.acceptInvitation(groupId, invitationId);
      setSuccessMessage(`Đã chấp nhận lời mời từ nhóm ${invitation?.groupCode || "nhóm"}. Chúc mừng bạn!`);
      setShowSuccessModal(true);
      setTimeout(() => fetchInvitations(), 3500);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể chấp nhận lời mời");
    } finally {
      setActing(null);
    }
  };

  const handleReject = async (groupId: string, invitationId: number) => {
    try {
      setActing(invitationId);
      const invitation = invitations.find((i) => i.id === invitationId);
      await studentGroupService.rejectInvitation(groupId, invitationId);
      setSuccessMessage(`Đã từ chối lời mời từ nhóm ${invitation?.groupCode || "nhóm"}.`);
      setShowSuccessModal(true);
      setTimeout(() => fetchInvitations(), 3500);
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể từ chối lời mời");
    } finally {
      setActing(null);
    }
  };

  const getInvitationStatus = (status: string) => {
    switch (status.toLowerCase()) {
      case "pending":
        return { label: "Chờ xử lý", color: "bg-yellow-100 text-yellow-800" };
      case "accepted":
        return { label: "Đã chấp nhận", color: "bg-green-100 text-green-800" };
      case "rejected":
        return { label: "Đã từ chối", color: "bg-red-100 text-red-800" };
      case "expired":
        return { label: "Hết hạn", color: "bg-gray-100 text-gray-800" };
      default:
        return { label: status, color: "bg-gray-100 text-gray-800" };
    }
  };

  const isPending = (invitation: InvitationDto) => invitation.status.toLowerCase() === "pending";
  const isExpired = (invitation: InvitationDto) => new Date(invitation.expiresAt) < new Date();

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-gray-500">Đang tải...</div>
      </div>
    );
  }

  if (invitations.length === 0) {
    return (
      <div className="max-w-2xl mx-auto">
        <div className="p-8 text-center bg-white border border-gray-200 rounded-lg shadow-sm">
          <div className="mb-4 text-5xl">
            <span className="text-gray-300 material-symbols-outlined" style={{ fontSize: 64 }}>
              mail
            </span>
          </div>
          <h2 className="mb-2 text-2xl font-bold text-gray-800">Không có lời mời nào</h2>
          <p className="text-gray-500">Bạn sẽ nhận được lời mời khi các nhóm khác mời bạn tham gia.</p>
        </div>
      </div>
    );
  }

  return (
    <>
      <div className="max-w-4xl mx-auto space-y-4">
        {/* Pending Invitations */}
        {invitations.some((inv) => isPending(inv)) && (
          <div>
            <h2 className="mb-3 text-lg font-bold text-gray-800">Lời mời chờ xử lý</h2>
            <div className="mb-6 space-y-3">
              {invitations
                .filter((inv) => isPending(inv))
                .map((invitation) => (
                  <div
                    key={invitation.id}
                    className="p-6 transition bg-white border-l-4 border-yellow-500 rounded-lg shadow-sm hover:shadow-md"
                  >
                    <div className="flex items-start justify-between mb-4">
                      <div>
                        <h3 className="text-lg font-bold text-gray-800">
                          {invitation.groupName || invitation.groupCode}
                        </h3>
                        <p className="text-sm text-gray-600">Mã nhóm: {invitation.groupCode}</p>
                      </div>
                      <div className="text-right">
                        <span className="inline-block px-3 py-1 mb-2 text-xs font-semibold text-yellow-800 bg-yellow-100 rounded-full">
                          Chờ xử lý
                        </span>
                        <p className="text-xs text-gray-500">
                          Hết hạn: {new Date(invitation.expiresAt).toLocaleDateString("vi-VN")}
                        </p>
                      </div>
                    </div>

                    <div className="mb-4">
                      <p className="mb-1 text-sm text-gray-600">
                        <span className="font-medium">Từ:</span> {invitation.inviterName}
                      </p>
                      {invitation.message && (
                        <div className="p-3 mt-2 rounded-lg bg-gray-50">
                          <p className="text-sm italic text-gray-700">"{invitation.message}"</p>
                        </div>
                      )}
                    </div>

                    {!isExpired(invitation) ? (
                      <div className="flex gap-3">
                        <button
                          onClick={() => handleReject(invitation.groupId, invitation.id)}
                          disabled={acting === invitation.id}
                          className="flex-1 px-4 py-2 font-semibold text-red-700 transition border border-red-300 rounded-lg hover:bg-red-50 disabled:opacity-50"
                        >
                          {acting === invitation.id ? "Đang xử lý..." : "Từ chối"}
                        </button>
                        <button
                          onClick={() => handleAccept(invitation.groupId, invitation.id)}
                          disabled={acting === invitation.id}
                          className="flex-1 px-4 py-2 font-semibold text-white transition bg-green-600 rounded-lg hover:bg-green-700 disabled:opacity-50"
                        >
                          {acting === invitation.id ? "Đang xử lý..." : "Chấp nhận"}
                        </button>
                      </div>
                    ) : (
                      <div className="px-4 py-2 font-semibold text-center text-gray-600 bg-gray-100 rounded-lg">
                        Lời mời đã hết hạn
                      </div>
                    )}
                  </div>
                ))}
            </div>
          </div>
        )}

        {/* Other Invitations */}
        {invitations.some((inv) => !isPending(inv)) && (
          <div>
            <h2 className="mb-3 text-lg font-bold text-gray-800">Lịch sử lời mời</h2>
            <div className="space-y-3">
              {invitations
                .filter((inv) => !isPending(inv))
                .map((invitation) => {
                  const statusInfo = getInvitationStatus(invitation.status);
                  return (
                    <div
                      key={invitation.id}
                      className="p-6 bg-white border border-gray-200 rounded-lg shadow-sm opacity-75"
                    >
                      <div className="flex items-start justify-between">
                        <div>
                          <h3 className="text-lg font-bold text-gray-800">
                            {invitation.groupName || invitation.groupCode}
                          </h3>
                          <p className="text-sm text-gray-600">Mã nhóm: {invitation.groupCode}</p>
                          <p className="mt-2 text-sm text-gray-500">
                            Từ: {invitation.inviterName} • {new Date(invitation.createdAt).toLocaleDateString("vi-VN")}
                          </p>
                        </div>
                        <span
                          className={`inline-block px-3 py-1 rounded-full text-xs font-semibold ${statusInfo.color}`}
                        >
                          {statusInfo.label}
                        </span>
                      </div>
                    </div>
                  );
                })}
            </div>
          </div>
        )}
      </div>

      {/* Success Modal */}
      <SuccessModal
        isOpen={showSuccessModal}
        onClose={() => setShowSuccessModal(false)}
        title="Thành công"
        message={successMessage}
        icon="check_circle"
        autoClose={3000}
      />
    </>
  );
}
