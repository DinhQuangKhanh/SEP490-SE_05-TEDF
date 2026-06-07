import { useState, useEffect, useCallback, useRef } from "react";
import { motion } from "framer-motion";
import { Header } from "@/components/layout";
import { useSystemError } from "@/contexts/SystemErrorContext";
import { CreateTicketModal } from "@/components/support/CreateTicketModal";
import { apiClient } from "@/lib/apiClient";
import { TicketListDto, TicketDto, TicketStatsDto } from "@/types/support.types";

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.1 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diff / 60000);
  if (minutes < 1) return "Vừa xong";
  if (minutes < 60) return `${minutes} phút trước`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} giờ trước`;
  const days = Math.floor(hours / 24);
  return `${days} ngày trước`;
}

function statusLabel(status: string) {
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

function statusClass(status: string) {
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

function priorityDot(priority: string) {
  switch (priority) {
    case "High":
      return "bg-error";
    case "Medium":
      return "bg-yellow-500";
    default:
      return "bg-slate-300";
  }
}

export function StudentSupportPage() {
  const { showError } = useSystemError();
  const [tickets, setTickets] = useState<TicketListDto[]>([]);
  const [selectedTicketId, setSelectedTicketId] = useState<string | null>(null);
  const [ticketDetail, setTicketDetail] = useState<TicketDto | null>(null);
  const [stats, setStats] = useState<TicketStatsDto | null>(null);
  const [newMessage, setNewMessage] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [filter, setFilter] = useState("all");
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const fetchTickets = useCallback(async () => {
    try {
      setIsLoading(true);
      const [ticketsData, statsData] = await Promise.all([
        apiClient.get<TicketListDto[]>("/api/supports"),
        apiClient.get<TicketStatsDto>("/api/supports/stats"),
      ]);
      setTickets(ticketsData);
      setStats(statsData);
      // Auto-select first ticket if none selected
      if (!selectedTicketId && ticketsData.length > 0) {
        setSelectedTicketId(ticketsData[0].id);
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : "Không thể tải danh sách yêu cầu.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  const fetchTicketDetail = useCallback(async (id: string) => {
    try {
      setIsDetailLoading(true);
      const data = await apiClient.get<TicketDto>(`/api/supports/${id}`);
      setTicketDetail(data);
    } catch {
      setTicketDetail(null);
    } finally {
      setIsDetailLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTickets();
  }, [fetchTickets]);

  useEffect(() => {
    if (selectedTicketId) {
      fetchTicketDetail(selectedTicketId);
    }
  }, [selectedTicketId, fetchTicketDetail]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [ticketDetail?.messages]);

  const handleReply = async () => {
    if (!newMessage.trim() || !selectedTicketId) return;
    setIsSending(true);
    try {
      await apiClient.post(`/api/supports/${selectedTicketId}/reply`, {
        content: newMessage.trim(),
      });
      setNewMessage("");
      await fetchTicketDetail(selectedTicketId);
    } catch {
      // silent
    } finally {
      setIsSending(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleReply();
    }
  };

  const filteredTickets = tickets.filter((t) => {
    if (filter === "unread") return t.status === "Open";
    if (filter === "resolved") return t.status === "Resolved" || t.status === "Closed";
    return true;
  });

  const currentUser = (() => {
    try {
      return JSON.parse(localStorage.getItem("user") || "{}");
    } catch {
      return {};
    }
  })();
  const currentUserId = currentUser?.id;

  return (
    <>
      <Header variant="primary" title="Hỗ trợ & Liên hệ Admin" showSearch={false} role="student" />

      <div className="flex-1 overflow-hidden p-8 bg-slate-50">
        <motion.div variants={container} initial="hidden" animate="show" className="flex flex-col h-full">
          {/* Stats */}
          <motion.div variants={item} className="grid grid-cols-3 gap-4 mb-6 shrink-0">
            <StatCard
              icon="confirmation_number"
              iconColor="text-slate-600"
              iconBg="bg-slate-100"
              value={stats?.totalTickets ?? 0}
              label="Tổng yêu cầu"
            />
            <StatCard
              icon="mark_email_unread"
              iconColor="text-error"
              iconBg="bg-error/10"
              value={stats?.unread ?? 0}
              label="Chờ phản hồi"
              valueColor="text-error"
            />
            <StatCard
              icon="check_circle"
              iconColor="text-success"
              iconBg="bg-success/10"
              value={stats?.resolved ?? 0}
              label="Đã giải quyết"
            />
          </motion.div>

          {/* Main Content */}
          <motion.div variants={item} className="flex-1 grid grid-cols-1 lg:grid-cols-12 gap-6 min-h-0">
            {/* Ticket List */}
            <div className="lg:col-span-4 bento-card rounded-md overflow-hidden flex flex-col">
              <div className="p-4 border-b border-slate-200 shrink-0">
                <div className="flex items-center gap-2 mb-3">
                  <button
                    onClick={() => setFilter("all")}
                    className={`text-xs font-medium px-2.5 py-1 rounded-full transition-colors ${filter === "all" ? "bg-primary text-white" : "text-slate-500 hover:bg-slate-100"}`}
                  >
                    Tất cả
                  </button>
                  <button
                    onClick={() => setFilter("unread")}
                    className={`text-xs font-medium px-2.5 py-1 rounded-full transition-colors ${filter === "unread" ? "bg-primary text-white" : "text-slate-500 hover:bg-slate-100"}`}
                  >
                    Chờ phản hồi
                  </button>
                  <button
                    onClick={() => setFilter("resolved")}
                    className={`text-xs font-medium px-2.5 py-1 rounded-full transition-colors ${filter === "resolved" ? "bg-primary text-white" : "text-slate-500 hover:bg-slate-100"}`}
                  >
                    Đã xong
                  </button>
                </div>
                <div className="relative">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-400 text-[18px]">
                    search
                  </span>
                  <input
                    className="w-full pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary bg-white placeholder-slate-400"
                    placeholder="Tìm kiếm yêu cầu..."
                    type="text"
                  />
                </div>
              </div>
              <div className="flex-1 overflow-y-auto custom-scrollbar divide-y divide-slate-100">
                {isLoading ? (
                  <div className="flex items-center justify-center py-12">
                    <span className="material-symbols-outlined animate-spin text-3xl text-primary">
                      progress_activity
                    </span>
                  </div>
                ) : filteredTickets.length === 0 ? (
                  <div className="flex flex-col items-center justify-center py-12 text-center px-4">
                    <span className="material-symbols-outlined text-4xl text-slate-300 mb-2">inbox</span>
                    <p className="text-sm text-slate-500">Chưa có yêu cầu nào</p>
                  </div>
                ) : (
                  filteredTickets.map((ticket) => (
                    <div
                      key={ticket.id}
                      onClick={() => setSelectedTicketId(ticket.id)}
                      className={`p-4 cursor-pointer transition-colors ${
                        selectedTicketId === ticket.id
                          ? "bg-primary/5 border-l-4 border-l-primary"
                          : "hover:bg-slate-50"
                      }`}
                    >
                      <div className="flex items-start gap-3">
                        <div className={`w-2 h-2 mt-2 rounded-full shrink-0 ${priorityDot(ticket.priority)}`} />
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 mb-0.5">
                            <span className="bg-primary text-white text-[10px] font-bold px-1.5 rounded">
                              {ticket.code}
                            </span>
                          </div>
                          <h4 className="text-sm font-semibold text-slate-800 truncate">{ticket.title}</h4>
                          <div className="flex items-center justify-between mt-2">
                            <span className="text-[10px] text-slate-400">{timeAgo(ticket.createdAt)}</span>
                            <span
                              className={`text-[10px] font-medium px-1.5 py-0.5 rounded ${statusClass(ticket.status)}`}
                            >
                              {statusLabel(ticket.status)}
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
              {/* New ticket button */}
              <div className="p-3 border-t border-slate-200 shrink-0">
                <button
                  onClick={() => setIsCreateModalOpen(true)}
                  className="w-full flex items-center justify-center gap-2 py-2.5 bg-primary text-white text-sm font-medium rounded-lg hover:bg-primary-dark transition-colors"
                >
                  <span className="material-symbols-outlined text-[18px]">add</span>
                  Tạo yêu cầu mới
                </button>
              </div>
            </div>

            {/* Chat Detail */}
            <div className="lg:col-span-8 bento-card rounded-md overflow-hidden flex flex-col">
              {!selectedTicketId || !ticketDetail ? (
                <div className="flex-1 flex flex-col items-center justify-center text-center p-8">
                  <span className="material-symbols-outlined text-5xl text-slate-300 mb-3">forum</span>
                  <p className="text-lg font-bold text-slate-700">Chọn một yêu cầu</p>
                  <p className="text-sm text-slate-500 mt-1">Chọn yêu cầu từ danh sách bên trái để xem chi tiết.</p>
                </div>
              ) : isDetailLoading ? (
                <div className="flex-1 flex items-center justify-center">
                  <span className="material-symbols-outlined animate-spin text-3xl text-primary">
                    progress_activity
                  </span>
                </div>
              ) : (
                <>
                  <div className="p-4 border-b border-slate-200 flex items-center justify-between bg-slate-50/50 shrink-0">
                    <div className="flex items-center gap-3">
                      <span className="bg-primary text-white text-xs font-bold px-2 py-0.5 rounded">
                        {ticketDetail.code}
                      </span>
                      <h3 className="font-bold text-slate-800 text-lg truncate">{ticketDetail.title}</h3>
                    </div>
                    <span
                      className={`text-xs font-medium px-2.5 py-1 rounded-full ${statusClass(ticketDetail.status)}`}
                    >
                      {statusLabel(ticketDetail.status)}
                    </span>
                  </div>

                  {/* Description */}
                  <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/30">
                    <p className="text-xs font-semibold text-slate-400 uppercase mb-1">Nội dung yêu cầu</p>
                    <p className="text-sm text-slate-700 whitespace-pre-line">{ticketDetail.description}</p>
                  </div>

                  {/* Messages */}
                  <div className="flex-1 overflow-y-auto p-6 space-y-6 custom-scrollbar">
                    {ticketDetail.messages.length === 0 ? (
                      <div className="flex flex-col items-center justify-center py-8 text-center">
                        <span className="material-symbols-outlined text-3xl text-slate-300 mb-2">
                          chat_bubble_outline
                        </span>
                        <p className="text-sm text-slate-500">Chưa có phản hồi nào. Admin sẽ phản hồi sớm nhất.</p>
                      </div>
                    ) : (
                      ticketDetail.messages.map((msg) => {
                        const isOwn = msg.senderId === currentUserId;
                        const initials =
                          msg.sender?.fullName
                            ?.split(" ")
                            .map((w) => w[0])
                            .join("")
                            .slice(0, 2)
                            .toUpperCase() || "??";
                        return (
                          <div key={msg.id} className={`flex gap-3 ${!isOwn ? "flex-row-reverse" : ""}`}>
                            <div
                              className={`w-8 h-8 rounded-full shrink-0 flex items-center justify-center text-xs font-bold ${
                                !isOwn ? "bg-primary text-white" : "bg-slate-200 text-slate-500"
                              }`}
                            >
                              {initials}
                            </div>
                            <div className={`flex-1 ${!isOwn ? "flex flex-col items-end" : ""}`}>
                              <div className="flex items-center gap-2 mb-1">
                                {!isOwn ? (
                                  <>
                                    <span className="text-[10px] text-slate-400">• {timeAgo(msg.createdAt)}</span>
                                    <span className="font-semibold text-slate-800 text-sm">
                                      {msg.sender?.fullName || "Admin"}
                                    </span>
                                  </>
                                ) : (
                                  <>
                                    <span className="font-semibold text-slate-800 text-sm">Bạn</span>
                                    <span className="text-[10px] text-slate-400">• {timeAgo(msg.createdAt)}</span>
                                  </>
                                )}
                              </div>
                              <div
                                className={`rounded-xl p-4 text-sm max-w-[80%] whitespace-pre-line ${
                                  !isOwn
                                    ? "bg-primary text-white rounded-tr-none"
                                    : "bg-slate-100 text-slate-700 rounded-tl-none"
                                }`}
                              >
                                {msg.content}
                              </div>
                            </div>
                          </div>
                        );
                      })
                    )}
                    <div ref={messagesEndRef} />
                  </div>

                  {/* Reply Box */}
                  {ticketDetail.status !== "Closed" && ticketDetail.status !== "Resolved" && (
                    <div className="p-4 border-t border-slate-200 bg-white shrink-0">
                      <div className="relative">
                        <textarea
                          value={newMessage}
                          onChange={(e) => setNewMessage(e.target.value)}
                          onKeyDown={handleKeyDown}
                          disabled={isSending}
                          className="w-full h-24 p-3 pr-12 text-sm bg-slate-50 border border-slate-200 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary resize-none disabled:opacity-50"
                          placeholder="Nhập nội dung tin nhắn cho Admin..."
                        />
                        <div className="absolute bottom-3 right-3 flex gap-1">
                          <button className="p-1.5 text-slate-400 hover:text-primary transition-colors">
                            <span className="material-symbols-outlined text-[20px]">attach_file</span>
                          </button>
                          <button
                            onClick={handleReply}
                            disabled={isSending || !newMessage.trim()}
                            className="p-1.5 bg-primary text-white rounded hover:bg-primary-light transition-colors disabled:opacity-50"
                          >
                            <span className="material-symbols-outlined text-[20px]">
                              {isSending ? "progress_activity" : "send"}
                            </span>
                          </button>
                        </div>
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          </motion.div>
        </motion.div>
      </div>

      <CreateTicketModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onCreated={() => {
          fetchTickets();
        }}
      />
    </>
  );
}

function StatCard({
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
