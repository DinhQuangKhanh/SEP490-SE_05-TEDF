import { useState, useEffect, useRef, useCallback, useMemo } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { AnimatePresence, motion } from "framer-motion";
import { semesterService, majorService } from "@/lib";
import type {
  SemesterDto,
  EligibleStudentDto,
  EligibleMentorDto,
  ImportRosterResponse,
  ImportRowIssue,
  MajorOption,
} from "@/types";

type Tab = "students" | "mentors";

const ACCEPTED = ["csv", "xlsx", "xls"];
const PAGE_SIZE = 20;

/** Gom các dòng lỗi theo lý do để hiển thị gọn. */
function groupIssues(issues: ImportRowIssue[]): [string, string[]][] {
  const map = new Map<string, string[]>();
  for (const it of issues) {
    const arr = map.get(it.reason) ?? [];
    arr.push(it.code);
    map.set(it.reason, arr);
  }
  return [...map.entries()];
}

export function SemesterRosterPage() {
  const params = useParams<{ id: string }>();
  const navigate = useNavigate();
  const semesterId = Number(params.id);

  const [semester, setSemester] = useState<SemesterDto | null>(null);
  const [tab, setTab] = useState<Tab>("students");
  const [students, setStudents] = useState<EligibleStudentDto[]>([]);
  const [mentors, setMentors] = useState<EligibleMentorDto[]>([]);
  const [majors, setMajors] = useState<MajorOption[]>([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [importing, setImporting] = useState(false);
  const [lastResult, setLastResult] = useState<{ tab: Tab; result: ImportRosterResponse } | null>(null);
  const [savingMentorId, setSavingMentorId] = useState<string | null>(null);
  const [publishing, setPublishing] = useState(false);
  const [showPublishConfirm, setShowPublishConfirm] = useState(false);

  // Tìm kiếm + phân trang (client) cho tab đang mở.
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  const fileInputRef = useRef<HTMLInputElement>(null);

  const isPublished = !!semester?.rosterPublishedAt;
  const canPublish = semester?.status === "Upcoming" && !isPublished;

  const loadRoster = useCallback(async () => {
    if (!Number.isFinite(semesterId)) {
      navigate("/admin/semesters", { replace: true });
      return;
    }
    setLoading(true);

    // Học kỳ là tài nguyên chính: không tồn tại/không tải được → quay về danh sách.
    let sem: SemesterDto;
    try {
      sem = await semesterService.getSemesterById(semesterId);
    } catch {
      navigate("/admin/semesters", { replace: true });
      return;
    }

    try {
      const [stu, men, maj] = await Promise.all([
        semesterService.getEligibleStudents(semesterId),
        semesterService.getEligibleMentors(semesterId),
        majorService.getMajors(),
      ]);
      setSemester(sem);
      setStudents(stu);
      setMentors(men);
      setMajors(maj);
      setError(null);
    } catch (err) {
      setSemester(sem);
      setError(err instanceof Error ? err.message : "Không tải được danh sách.");
    } finally {
      setLoading(false);
    }
  }, [semesterId, navigate]);

  useEffect(() => {
    loadRoster();
  }, [loadRoster]);

  const switchTab = (key: Tab) => {
    setTab(key);
    setLastResult(null);
    setSearch("");
    setPage(1);
  };

  const handleImportClick = () => fileInputRef.current?.click();

  const handleFile = async (file: File | undefined) => {
    if (!file) return;
    const ext = file.name.split(".").pop()?.toLowerCase();
    if (!ACCEPTED.includes(ext ?? "")) {
      setError("Chỉ chấp nhận file .csv, .xlsx hoặc .xls");
      return;
    }
    setError(null);
    setImporting(true);
    try {
      const result =
        tab === "students"
          ? await semesterService.importEligibleStudents(semesterId, file)
          : await semesterService.importEligibleMentors(semesterId, file);
      setLastResult({ tab, result });
      // Làm mới cả 2 bảng để badge & danh sách luôn đúng.
      const [stu, men] = await Promise.all([
        semesterService.getEligibleStudents(semesterId),
        semesterService.getEligibleMentors(semesterId),
      ]);
      setStudents(stu);
      setMentors(men);
      setPage(1);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Nhập danh sách thất bại.");
    } finally {
      setImporting(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  const handleMentorMajorChange = async (mentorId: string, majorId: number) => {
    setSavingMentorId(mentorId);
    try {
      await semesterService.updateEligibleMentorMajor(semesterId, mentorId, majorId);
      const major = majors.find((m) => m.id === majorId);
      setMentors((prev) =>
        prev.map((m) =>
          m.mentorId === mentorId
            ? { ...m, majorId, programCode: major?.code ?? m.programCode, programName: major?.name ?? m.programName }
            : m,
        ),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Cập nhật ngành thất bại.");
    } finally {
      setSavingMentorId(null);
    }
  };

  const handlePublish = async () => {
    setPublishing(true);
    try {
      await semesterService.publishRoster(semesterId);
      setSemester(await semesterService.getSemesterById(semesterId));
      setShowPublishConfirm(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Công bố danh sách thất bại.");
      setShowPublishConfirm(false);
    } finally {
      setPublishing(false);
    }
  };

  const statusPill = (active: boolean, onLabel: string, offLabel: string) => (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold border ${
        active ? "bg-green-50 text-green-700 border-green-200" : "bg-slate-100 text-slate-500 border-slate-200"
      }`}
    >
      {active ? onLabel : offLabel}
    </span>
  );

  // Lọc theo tìm kiếm (client).
  const filteredStudents = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return students;
    return students.filter((s) =>
      [s.studentCode, s.fullName, s.email].some((v) => (v ?? "").toLowerCase().includes(q)),
    );
  }, [students, search]);

  const filteredMentors = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return mentors;
    return mentors.filter((m) =>
      [m.employeeCode, m.fullName, m.email].some((v) => (v ?? "").toLowerCase().includes(q)),
    );
  }, [mentors, search]);

  const totalUnfiltered = tab === "students" ? students.length : mentors.length;
  const filteredCount = tab === "students" ? filteredStudents.length : filteredMentors.length;
  const pageCount = Math.max(1, Math.ceil(filteredCount / PAGE_SIZE));
  const currentPage = Math.min(page, pageCount);
  const sliceStart = (currentPage - 1) * PAGE_SIZE;
  const pagedStudents = filteredStudents.slice(sliceStart, sliceStart + PAGE_SIZE);
  const pagedMentors = filteredMentors.slice(sliceStart, sliceStart + PAGE_SIZE);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24 text-slate-400">
        <span className="material-symbols-outlined animate-spin text-[28px]">progress_activity</span>
      </div>
    );
  }

  return (
    <div className="flex-1 p-8 overflow-y-auto scrollbar-hide bg-slate-50">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="flex flex-col gap-4 mb-6 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex items-start gap-3">
            <button
              onClick={() => navigate("/admin/semesters")}
              className="p-2 mt-0.5 transition-colors rounded-full text-slate-400 hover:text-slate-600 hover:bg-slate-100"
              title="Quay lại"
            >
              <span className="material-symbols-outlined">arrow_back</span>
            </button>
            <div>
              <h1 className="text-2xl font-bold text-slate-800">Danh sách đủ điều kiện</h1>
              <p className="mt-0.5 text-sm text-slate-500">
                {semester ? (
                  <>
                    <span className="font-semibold text-slate-700">{semester.name}</span>
                    <span className="ml-2 font-mono text-xs text-slate-400">{semester.code}</span>
                  </>
                ) : (
                  "—"
                )}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {isPublished && semester?.rosterPublishedAt && (
              <span className="text-xs text-slate-500">
                Đã công bố lúc {format(new Date(semester.rosterPublishedAt), "HH:mm dd/MM/yyyy", { locale: vi })}
              </span>
            )}
            <button
              onClick={loadRoster}
              title="Làm mới danh sách"
              className="flex items-center gap-2 px-3 py-2 text-sm font-semibold transition-colors bg-white border rounded-md border-slate-300 text-slate-700 hover:bg-slate-50"
            >
              <span className="material-symbols-outlined text-[18px]">refresh</span>
              Làm mới
            </button>
            <button
              onClick={() => setShowPublishConfirm(true)}
              disabled={!canPublish || publishing}
              title={
                isPublished
                  ? "Danh sách đã được công bố"
                  : semester?.status !== "Upcoming"
                    ? "Chỉ công bố được khi kỳ học ở trạng thái Sắp tới"
                    : undefined
              }
              className="flex items-center gap-2 px-5 py-2 text-sm font-bold text-white transition-all rounded-md shadow-lg bg-primary shadow-primary/20 hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <span className="material-symbols-outlined text-[18px]">campaign</span>
              {isPublished ? "Đã công bố" : "Công bố & thông báo"}
            </button>
          </div>
        </div>

        {error && (
          <div className="flex items-start gap-3 p-3 mb-4 border border-red-200 rounded-md bg-red-50">
            <span className="material-symbols-outlined text-red-600 text-[20px] mt-0.5">error</span>
            <p className="text-sm text-red-800">{error}</p>
          </div>
        )}

        {/* Tabs */}
        <div className="flex gap-1 p-1 mb-4 rounded-lg bg-slate-100 w-fit">
          {(
            [
              { key: "students", label: "Sinh viên", count: students.length, icon: "school" },
              { key: "mentors", label: "Giảng viên", count: mentors.length, icon: "person" },
            ] as const
          ).map((t) => (
            <button
              key={t.key}
              onClick={() => switchTab(t.key)}
              className={`flex items-center gap-2 px-4 py-2 text-sm font-semibold rounded-md transition-colors ${
                tab === t.key ? "bg-white text-primary shadow-sm" : "text-slate-500 hover:text-slate-700"
              }`}
            >
              <span className="material-symbols-outlined text-[18px]">{t.icon}</span>
              {t.label}
              <span className="px-1.5 py-0.5 text-[10px] font-bold rounded-full bg-slate-200 text-slate-600">
                {t.count}
              </span>
            </button>
          ))}
        </div>

        {/* Import bar */}
        <div className="flex flex-wrap items-center justify-between gap-3 p-4 mb-4 bg-white border rounded-lg border-slate-200">
          <p className="text-xs text-slate-500">
            Nhập bổ sung từ Excel/CSV. Các dòng đã có trong danh sách sẽ được bỏ qua; chỉ thêm dòng mới.
            {tab === "mentors" &&
              " File giảng viên cần cột mã giảng viên; trùng mã nhưng khác SĐT sẽ được tạo mã mới. Giảng viên đã có đề tài trong kho chờ đăng ký sẽ được tự động thêm."}
          </p>
          <input
            ref={fileInputRef}
            type="file"
            accept=".csv,.xlsx,.xls"
            className="hidden"
            onChange={(e) => handleFile(e.target.files?.[0])}
          />
          <button
            onClick={handleImportClick}
            disabled={importing}
            className="flex items-center gap-2 px-4 py-2 text-sm font-semibold transition-colors bg-white border rounded-md border-slate-300 text-slate-700 hover:bg-slate-50 disabled:opacity-50"
          >
            <span className={`material-symbols-outlined text-[18px] ${importing ? "animate-spin" : ""}`}>
              {importing ? "progress_activity" : "upload_file"}
            </span>
            {importing ? "Đang nhập..." : "Nhập bổ sung"}
          </button>
        </div>

        {/* Import result */}
        {lastResult && lastResult.tab === tab && (
          <div className="p-3 mb-4 border rounded-md border-blue-200 bg-blue-50">
            <p className="text-sm font-semibold text-blue-900">
              Đã nhập {lastResult.result.successfullyImported}/{lastResult.result.totalProcessed} dòng.
            </p>
            {(lastResult.result.issues ?? []).length > 0 && (
              <div className="mt-1 space-y-1">
                {groupIssues(lastResult.result.issues ?? []).map(([reason, codes]) => (
                  <p key={reason} className="text-xs text-amber-700">
                    <span className="font-semibold">{codes.length} mã</span> — {reason}: {codes.join(", ")}
                  </p>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Search */}
        <div className="flex flex-wrap items-center justify-between gap-3 mb-3">
          <div className="relative">
            <span className="material-symbols-outlined text-[18px] text-slate-400 absolute left-2.5 top-1/2 -translate-y-1/2">
              search
            </span>
            <input
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
              placeholder={
                tab === "students" ? "Tìm theo MSSV, họ tên, email…" : "Tìm theo mã GV, họ tên, email…"
              }
              className="py-2 pl-9 pr-3 text-sm bg-white border rounded-md outline-none w-72 border-slate-200 focus:ring-2 focus:ring-primary/20 focus:border-primary"
            />
          </div>
          <p className="text-xs text-slate-500">{filteredCount} kết quả</p>
        </div>

        {/* Tables */}
        <div className="overflow-x-auto bg-white border rounded-lg border-slate-200">
          {tab === "students" ? (
            <table className="w-full text-sm">
              <thead className="text-xs uppercase border-b text-slate-500 bg-slate-50 border-slate-200">
                <tr>
                  <th className="px-4 py-3 font-semibold text-left">MSSV</th>
                  <th className="px-4 py-3 font-semibold text-left">Họ tên</th>
                  <th className="px-4 py-3 font-semibold text-left">Email</th>
                  <th className="px-4 py-3 font-semibold text-left">SĐT</th>
                  <th className="px-4 py-3 font-semibold text-left">Ngành</th>
                  <th className="px-4 py-3 font-semibold text-left">Trạng thái</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredStudents.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                      {totalUnfiltered === 0
                        ? "Chưa có sinh viên nào. Hãy nhập danh sách từ Excel."
                        : "Không tìm thấy kết quả phù hợp."}
                    </td>
                  </tr>
                ) : (
                  pagedStudents.map((s) => (
                    <tr key={s.studentId} className="hover:bg-slate-50">
                      <td className="px-4 py-2.5 font-mono text-xs text-slate-700">{s.studentCode}</td>
                      <td className="px-4 py-2.5 text-slate-700">{s.fullName ?? "—"}</td>
                      <td className="px-4 py-2.5 text-slate-500">{s.email ?? "—"}</td>
                      <td className="px-4 py-2.5 text-slate-500">{s.phoneNumber ?? "—"}</td>
                      <td className="px-4 py-2.5 text-slate-500">{s.programName ?? s.programCode ?? "—"}</td>
                      <td className="px-4 py-2.5">{statusPill(s.isEligible, "Đủ điều kiện", "Đã thu hồi")}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          ) : (
            <table className="w-full text-sm">
              <thead className="text-xs uppercase border-b text-slate-500 bg-slate-50 border-slate-200">
                <tr>
                  <th className="px-4 py-3 font-semibold text-left">Mã GV</th>
                  <th className="px-4 py-3 font-semibold text-left">Họ tên</th>
                  <th className="px-4 py-3 font-semibold text-left">Email</th>
                  <th className="px-4 py-3 font-semibold text-left">SĐT</th>
                  <th className="px-4 py-3 font-semibold text-left">Bộ môn</th>
                  <th className="px-4 py-3 font-semibold text-left">Ngành hướng dẫn</th>
                  <th className="px-4 py-3 font-semibold text-left">Trạng thái</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredMentors.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                      {totalUnfiltered === 0
                        ? "Chưa có giảng viên nào. Hãy nhập danh sách từ Excel."
                        : "Không tìm thấy kết quả phù hợp."}
                    </td>
                  </tr>
                ) : (
                  pagedMentors.map((m) => (
                    <tr key={m.mentorId} className="hover:bg-slate-50">
                      <td className="px-4 py-2.5 font-mono text-xs text-slate-700">{m.employeeCode}</td>
                      <td className="px-4 py-2.5 text-slate-700">{m.fullName ?? "—"}</td>
                      <td className="px-4 py-2.5 text-slate-500">{m.email ?? "—"}</td>
                      <td className="px-4 py-2.5 text-slate-500">{m.phoneNumber ?? "—"}</td>
                      <td className="px-4 py-2.5 text-slate-600">
                        {m.division ? (
                          <span className="inline-flex items-center px-2 py-0.5 text-xs font-semibold rounded bg-slate-100 text-slate-600">
                            {m.division}
                          </span>
                        ) : (
                          "—"
                        )}
                      </td>
                      <td className="px-4 py-2.5">
                        <div className="flex items-center gap-2">
                          <select
                            value={m.majorId ?? ""}
                            disabled={savingMentorId === m.mentorId || isPublished}
                            onChange={(e) => {
                              const value = e.target.value;
                              if (value) handleMentorMajorChange(m.mentorId, Number(value));
                            }}
                            className={`px-2 py-1 text-xs border rounded-md outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary disabled:opacity-60 ${
                              m.majorId == null ? "border-red-300 bg-red-50 text-red-700" : "border-slate-200"
                            }`}
                          >
                            {m.majorId == null && <option value="">— Chọn ngành —</option>}
                            {majors.map((maj) => (
                              <option key={maj.id} value={maj.id}>
                                {maj.name}
                              </option>
                            ))}
                          </select>
                          {savingMentorId === m.mentorId && (
                            <span className="material-symbols-outlined animate-spin text-[16px] text-slate-400">
                              progress_activity
                            </span>
                          )}
                        </div>
                      </td>
                      <td className="px-4 py-2.5">{statusPill(m.isAssigned, "Đã phân công", "Đã thu hồi")}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          )}
        </div>

        {/* Pagination */}
        {filteredCount > 0 && (
          <div className="flex items-center justify-between gap-3 mt-3 text-sm text-slate-600">
            <span>
              Trang <span className="font-semibold">{currentPage}</span>/{pageCount}
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage(currentPage - 1)}
                disabled={currentPage <= 1}
                className="px-3 py-1.5 text-sm font-semibold transition-colors bg-white border rounded-md border-slate-200 text-slate-700 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed"
              >
                Trước
              </button>
              <button
                onClick={() => setPage(currentPage + 1)}
                disabled={currentPage >= pageCount}
                className="px-3 py-1.5 text-sm font-semibold transition-colors bg-white border rounded-md border-slate-200 text-slate-700 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed"
              >
                Sau
              </button>
            </div>
          </div>
        )}

        {/* Publish confirm dialog */}
        <AnimatePresence>
          {showPublishConfirm && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm"
              onClick={() => !publishing && setShowPublishConfirm(false)}
            >
              <motion.div
                initial={{ opacity: 0, scale: 0.95, y: 20 }}
                animate={{ opacity: 1, scale: 1, y: 0 }}
                exit={{ opacity: 0, scale: 0.95, y: 20 }}
                onClick={(e) => e.stopPropagation()}
                className="w-full max-w-md p-6 bg-white shadow-2xl rounded-xl"
              >
                <div className="flex items-center justify-center w-12 h-12 mx-auto mb-4 text-amber-600 bg-amber-100 rounded-full">
                  <span className="material-symbols-outlined text-[26px]">campaign</span>
                </div>
                <h3 className="mb-2 text-lg font-bold text-center text-slate-800">Công bố danh sách?</h3>
                <p className="mb-6 text-sm text-center text-slate-500">
                  Hành động này sẽ <strong>gửi thông báo cho giảng viên</strong> và <strong>email cho toàn bộ sinh
                  viên đủ điều kiện</strong>. Bạn không thể hoàn tác.
                </p>
                <div className="flex gap-3">
                  <button
                    onClick={() => setShowPublishConfirm(false)}
                    disabled={publishing}
                    className="flex-1 px-4 py-2 text-sm font-semibold transition-colors border rounded-md border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-50"
                  >
                    Hủy
                  </button>
                  <button
                    onClick={handlePublish}
                    disabled={publishing}
                    className="flex items-center justify-center flex-1 gap-2 px-4 py-2 text-sm font-bold text-white transition-colors rounded-md bg-primary hover:bg-primary/90 disabled:opacity-50"
                  >
                    {publishing ? (
                      <>
                        <span className="material-symbols-outlined animate-spin text-[18px]">progress_activity</span>
                        Đang công bố...
                      </>
                    ) : (
                      "Xác nhận công bố"
                    )}
                  </button>
                </div>
              </motion.div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  );
}
