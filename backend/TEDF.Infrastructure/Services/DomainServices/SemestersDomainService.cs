using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.Commands;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate.Entities;
using TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Constants;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Semester;
using TEDF.Domain.Services;
using IDateTimeService = TEDF.Application.Common.Interfaces.IDateTimeService;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the Semesters feature. See <see cref="ISemestersDomainService"/>.
/// </summary>
public class SemestersDomainService : ISemestersDomainService
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMajorReadRepository _majorRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IExcelService _excelService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SemestersDomainService> _logger;

    public SemestersDomainService(
        ISemesterRepository semesterRepository,
        IUserRepository userRepository,
        IMajorReadRepository majorRepository,
        IProjectRepository projectRepository,
        IExcelService excelService,
        IDateTimeService dateTimeService,
        IUnitOfWork unitOfWork,
        ILogger<SemestersDomainService> logger)
    {
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
        _majorRepository = majorRepository;
        _projectRepository = projectRepository;
        _excelService = excelService;
        _dateTimeService = dateTimeService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ── Helper queries ─────────────────────────────────────────────────────
    public async Task<int?> GetActiveSemesterIdAsync(CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetActiveAsync(ct);
        return semester?.Id;
    }

    public async Task<int?> GetCurrentPhaseIdAsync(int semesterId, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetWithPhasesAsync(semesterId, ct);
        var currentPhase = semester?.Phases.FirstOrDefault(p =>
            p.StartDate <= _dateTimeService.UtcNow && p.EndDate >= _dateTimeService.UtcNow);
        return currentPhase?.Id;
    }

    public async Task<bool> IsWithinPhaseAsync(int semesterId, int phaseId, DateTime date, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetWithPhasesAsync(semesterId, ct);
        var phase = semester?.Phases.FirstOrDefault(p => p.Id == phaseId);
        if (phase is null) return false;
        return date >= phase.StartDate && date <= phase.EndDate;
    }

    public async Task<int?> GetSemesterAfterAsync(int semesterId, int count, CancellationToken ct = default)
    {
        if (count <= 0) return semesterId;
        var targetSemester = await _semesterRepository.GetSemesterAfterAsync(semesterId, count, ct);
        return targetSemester?.Id;
    }

    // ── Write operations ───────────────────────────────────────────────────
    public async Task<int> CreateAsync(
        string name, string codeValue, DateTime startDate, DateTime endDate,
        int academicYearStart, string? description,
        IReadOnlyList<NewSemesterPhase> phases, CancellationToken ct = default)
    {
        var code = SemesterCode.Create(codeValue);
        if (await _semesterRepository.ExistsCodeAsync(code, ct))
            throw new BusinessRuleValidationException($"Semester code '{codeValue}' already exists.");

        if (await _semesterRepository.HasOverlappingAsync(startDate, endDate, cancellationToken: ct))
            throw new BusinessRuleValidationException("Khoảng thời gian học kỳ bị trùng lặp với một học kỳ khác đã tồn tại.");

        var nextId = await _semesterRepository.GetNextIdAsync(ct);
        var academicYear = AcademicYear.Create(academicYearStart);

        var semester = Semester.Create(nextId, name, code, startDate, endDate, academicYear, description);

        // Registration & Evaluation must fall within the currently-active semester; Implementation &
        // Defense are validated against the new semester by the aggregate.
        var currentSemester = await _semesterRepository.GetActiveAsync(ct);

        foreach (var phaseDto in phases)
        {
            if (!Enum.TryParse<SemesterPhaseType>(phaseDto.Type, true, out var phaseType))
                throw new BusinessRuleValidationException($"Invalid phase type '{phaseDto.Type}'.");

            CurrentSemesterPhaseGuard.EnsureWithinCurrentSemester(
                currentSemester, phaseType, phaseDto.Name, phaseDto.StartDate, phaseDto.EndDate);

            semester.AddPhase(phaseDto.Name, phaseType, phaseDto.StartDate, phaseDto.EndDate);
        }

        await _semesterRepository.AddAsync(semester, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return semester.Id;
    }

    public async Task UpdateAsync(
        int id, string name, string? description, DateTime startDate, DateTime endDate,
        IReadOnlyList<SemesterPhaseDateChange>? phases, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Semester), id);

        if (await _semesterRepository.HasOverlappingAsync(startDate, endDate, id, ct))
            throw new BusinessRuleValidationException("Khoảng thời gian học kỳ bị trùng lặp với một học kỳ khác đã tồn tại.");

        semester.UpdateDetails(name, description);
        semester.UpdateDates(startDate, endDate);

        if (phases is { Count: > 0 })
        {
            var currentSemester = await _semesterRepository.GetActiveAsync(ct);

            foreach (var phase in phases)
            {
                var domainPhase = semester.Phases.FirstOrDefault(p => p.Id == phase.Id)
                    ?? throw new EntityNotFoundException(nameof(SemesterPhase), phase.Id);

                CurrentSemesterPhaseGuard.EnsureWithinCurrentSemester(
                    currentSemester, domainPhase.Type, domainPhase.Name, phase.StartDate, phase.EndDate);

                semester.UpdatePhaseDates(phase.Id, phase.StartDate, phase.EndDate);
            }
        }

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Semester), id);

        if (semester.Status != SemesterStatus.Upcoming)
            throw new BusinessRuleValidationException("Only upcoming semesters can be deleted.");

        _semesterRepository.Remove(semester);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<EligibleStudentsImportResult> ImportEligibleStudentsAsync(
        int semesterId, Stream fileStream, string fileName, Guid importedBy, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, ct)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        if (fileStream == null || fileStream.Length == 0)
            throw new BusinessRuleValidationException("File tải lên không hợp lệ hoặc rỗng.");

        var rows = await _excelService.ExtractEligibleStudentRowsAsync(fileStream, fileName, ct);

        if (rows.Count == 0)
            throw new BusinessRuleValidationException("Không tìm thấy mã sinh viên nào trong file. Đảm bảo file có cột chứa mã sinh viên hợp lệ.");

        var successCount = 0;
        var issues = new List<ImportRowIssue>();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var student = await _userRepository.GetByStudentCodeAsync(row.StudentCode, ct);

            // Load the program once: used both for the eligible snapshot and the new account's department.
            var major = string.IsNullOrWhiteSpace(row.ProgramCode)
                ? null
                : await _majorRepository.GetByCodeAsync(row.ProgramCode.Trim(), ct);

            if (student is null)
            {
                // Tài khoản chưa có → tạo "tài khoản chờ" (liên kết khi đăng nhập Google lần đầu).
                student = await TryProvisionStudentAsync(row, major, seenEmails, ct);
                if (student is null)
                {
                    issues.Add(new ImportRowIssue(row.StudentCode, EmailIssueReason));
                    continue;
                }
            }
            else if (!InfoMatches(student, row.Email, row.FullName, row.PhoneNumber))
            {
                // MSSV là duy nhất toàn hệ thống: thông tin lệch ⇒ dữ liệu có vấn đề ⇒ bỏ qua + cảnh báo.
                issues.Add(new ImportRowIssue(row.StudentCode,
                    "Thông tin (email/họ tên/SĐT) không khớp dữ liệu hệ thống — cần kiểm tra"));
                continue;
            }

            // Idempotent: existing rows are ignored (snapshot refreshed) — the supplementary-import rule.
            semester.AddEligibleStudent(student.Id, row.StudentCode, row.Email, row.PhoneNumber, major?.Id, importedBy);
            successCount++;
        }

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Imported {Count} eligible students for Semester {SemesterId}. Issues: {IssueCount}",
            successCount, semesterId, issues.Count);

        return new EligibleStudentsImportResult(rows.Count, successCount, issues);
    }

    public async Task<EligibleMentorsImportResult> ImportEligibleMentorsAsync(
        int semesterId, Stream fileStream, string fileName, Guid importedBy, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, ct)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        if (fileStream == null || fileStream.Length == 0)
            throw new BusinessRuleValidationException("File tải lên không hợp lệ hoặc rỗng.");

        var rows = await _excelService.ExtractEligibleMentorRowsAsync(fileStream, fileName, ct);

        if (rows.Count == 0)
            throw new BusinessRuleValidationException("Không tìm thấy mã giảng viên nào trong file. Đảm bảo file có cột chứa mã giảng viên và ngành hợp lệ.");

        var successCount = 0;
        var issues = new List<ImportRowIssue>();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            // Ngành tùy chọn lúc import: khớp được thì lấy, không thì để trống (admin chọn sau qua dropdown).
            var major = string.IsNullOrWhiteSpace(row.ProgramCode)
                ? null
                : await _majorRepository.GetByCodeAsync(row.ProgramCode.Trim(), ct);

            var existing = await _userRepository.GetByEmployeeCodeAsync(row.EmployeeCode, ct);

            User? mentor;
            string code;
            string? snapshotEmail = row.Email;

            if (existing is null)
            {
                // Mã chưa có → tạo tài khoản với mã/email gốc.
                mentor = await TryProvisionUserAsync(row.EmployeeCode, row.FullName, row.Email, row.PhoneNumber,
                    major, DomainRoleNames.Mentor, isStudent: false, seenEmails, ct);
                if (mentor is null) { issues.Add(new ImportRowIssue(row.EmployeeCode, EmailIssueReason)); continue; }
                code = row.EmployeeCode;
            }
            else if (!IsDifferentMentor(existing, row.PhoneNumber))
            {
                // Cùng người (cùng SĐT, hoặc không đủ SĐT để phân biệt) → dùng tài khoản cũ.
                mentor = existing;
                code = row.EmployeeCode;
            }
            else
            {
                // Trùng mã nhưng KHÁC SĐT ⇒ người khác ⇒ tạo tài khoản mới, thêm số thứ tự cho cả mã lẫn email.
                if (string.IsNullOrWhiteSpace(row.Email) ||
                    !row.Email.Trim().EndsWith("@fpt.edu.vn", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new ImportRowIssue(row.EmployeeCode,
                        "Trùng mã GV nhưng khác người, thiếu email @fpt.edu.vn để tạo tài khoản mới"));
                    continue;
                }

                var (newCode, newEmail) = await NextAvailableSuffixAsync(row.EmployeeCode, row.Email.Trim(), seenCodes, seenEmails, ct);
                mentor = await TryProvisionUserAsync(newCode, row.FullName, newEmail, row.PhoneNumber,
                    major, DomainRoleNames.Mentor, isStudent: false, seenEmails, ct);
                if (mentor is null) { issues.Add(new ImportRowIssue(row.EmployeeCode, "Không tạo được tài khoản mới (email bị trùng)")); continue; }
                code = newCode;
                snapshotEmail = newEmail;
            }

            seenCodes.Add(code);
            semester.AddEligibleMentor(mentor.Id, code, major?.Id, snapshotEmail, row.PhoneNumber, row.Division, importedBy);
            successCount++;
        }

        // Mentors who already own a pool topic awaiting registration are assigned automatically.
        await SyncPoolMentorsAsync(semester, ct);

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Imported {Count} eligible mentors for Semester {SemesterId}. Issues: {IssueCount}",
            successCount, semesterId, issues.Count);

        return new EligibleMentorsImportResult(rows.Count, successCount, issues);
    }

    public async Task PublishRosterAsync(int semesterId, Guid publishedBy, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, ct)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        // Ensure mentors who already own pool topics for this semester are on the roster before notifying.
        await SyncPoolMentorsAsync(semester, ct);

        // Raises SemesterRosterPublishedEvent; handlers (notify mentors / enqueue student emails)
        // run after SaveChanges via DomainEventInterceptor.
        semester.PublishRoster(publishedBy);

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Published eligibility roster for Semester {SemesterId} by {PublishedBy}.", semesterId, publishedBy);
    }

    public async Task UpdateEligibleMentorMajorAsync(int semesterId, Guid mentorId, int majorId, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, ct)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        _ = await _majorRepository.GetByIdAsync(majorId, ct)
            ?? throw new EntityNotFoundException(nameof(Major), majorId);

        semester.UpdateEligibleMentorMajor(mentorId, majorId);

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<bool> IsMentorAssignedAsync(Guid mentorId, int semesterId, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, ct);
        if (semester is not null && semester.EligibleMentors.Any(m => m.MentorId == mentorId && m.IsAssigned))
            return true;

        // Pool-topic owners qualify even before the roster is published. (Not "active supervision" — that
        // would circularly pass the very project being approved, which already lists this mentor.)
        var poolMentors = await _projectRepository.GetPoolMentorAssignmentsForSemesterAsync(semesterId, ct);
        return poolMentors.Any(p => p.MentorId == mentorId);
    }

    private const string EmailIssueReason = "Email không hợp lệ (@fpt.edu.vn) hoặc trùng";

    /// <summary>So sánh từng trường: chỉ coi là LỆCH khi cả 2 bên đều có giá trị và khác nhau.</summary>
    private static bool FieldMatches(string? dbValue, string? fileValue)
        => string.IsNullOrWhiteSpace(dbValue) || string.IsNullOrWhiteSpace(fileValue)
           || string.Equals(dbValue.Trim(), fileValue.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool InfoMatches(User user, string? fileEmail, string? fileName, string? filePhone)
        => FieldMatches(user.Email.Value, fileEmail)
        && FieldMatches(user.FullName, fileName)
        && FieldMatches(user.PhoneNumber, filePhone);

    /// <summary>Khác người nếu cùng mã GV nhưng SĐT khác nhau (chỉ kết luận khi cả 2 đều có SĐT).</summary>
    private static bool IsDifferentMentor(User existing, string? filePhone)
        => !string.IsNullOrWhiteSpace(existing.PhoneNumber)
        && !string.IsNullOrWhiteSpace(filePhone)
        && !string.Equals(existing.PhoneNumber.Trim(), filePhone.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Tìm (mã, email) còn trống bằng cách thêm số thứ tự: LamDQ/lamdq@… → LamDQ1/lamdq1@…, LamDQ2/…</summary>
    private async Task<(string Code, string Email)> NextAvailableSuffixAsync(
        string baseCode, string baseEmail, HashSet<string> seenCodes, HashSet<string> seenEmails, CancellationToken ct)
    {
        var at = baseEmail.IndexOf('@');
        var local = at > 0 ? baseEmail[..at] : baseEmail;
        var domain = at > 0 ? baseEmail[at..] : "@fpt.edu.vn"; // gồm '@'

        for (var n = 1; ; n++)
        {
            var candCode = $"{baseCode}{n}";
            var candEmail = $"{local}{n}{domain}".ToLowerInvariant();
            if (seenCodes.Contains(candCode) || seenEmails.Contains(candEmail)) continue;
            if (await _userRepository.GetByEmployeeCodeAsync(candCode, ct) is not null) continue;
            if (await _userRepository.ExistsByEmailAsync(candEmail, ct)) continue;
            return (candCode, candEmail);
        }
    }

    /// <summary>Tạo "tài khoản chờ" cho sinh viên chưa tồn tại; null nếu không thể tạo (caller báo lỗi).</summary>
    private Task<User?> TryProvisionStudentAsync(EligibleStudentRow row, Major? major, HashSet<string> seenEmails, CancellationToken ct)
        => TryProvisionUserAsync(row.StudentCode, row.FullName, row.Email, row.PhoneNumber, major, DomainRoleNames.Student, isStudent: true, seenEmails, ct);

    /// <summary>
    /// Tạo User mới với FirebaseUid tạm ("pending:&lt;mã&gt;") để liên kết khi đăng nhập Google lần đầu.
    /// Yêu cầu email @fpt.edu.vn hợp lệ và không trùng (trong file lẫn trong DB); trả null nếu vi phạm.
    /// </summary>
    private async Task<User?> TryProvisionUserAsync(
        string code, string? fullName, string? email, string? phone, Major? major, string role, bool isStudent,
        HashSet<string> seenEmails, CancellationToken ct)
    {
        var trimmed = email?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) ||
            !trimmed.EndsWith("@fpt.edu.vn", StringComparison.OrdinalIgnoreCase))
            return null; // cần email FPT để đăng nhập Google

        var normalized = trimmed.ToLowerInvariant();
        if (!seenEmails.Add(normalized)) return null;                         // trùng email trong cùng file
        if (await _userRepository.ExistsByEmailAsync(normalized, ct)) return null; // email đã thuộc người khác

        var user = User.Create(
            firebaseUid: $"{User.PendingUidPrefix}{code}",
            email: normalized,
            fullName: string.IsNullOrWhiteSpace(fullName) ? code : fullName.Trim(),
            studentCode: isStudent ? code : null,
            employeeCode: isStudent ? null : code,
            departmentId: major?.DepartmentId,
            phoneNumber: string.IsNullOrWhiteSpace(phone) ? null : phone.Trim());

        user.AssignRole(role);
        await _userRepository.AddAsync(user, ct);
        return user;
    }

    /// <summary>
    /// Adds (idempotently) any mentor who already owns a pool topic awaiting registration for this
    /// semester, using the topic's Major — so they are assigned even if the imported CSV omits them.
    /// </summary>
    private async Task SyncPoolMentorsAsync(Semester semester, CancellationToken ct)
    {
        var poolMentors = await _projectRepository.GetPoolMentorAssignmentsForSemesterAsync(semester.Id, ct);
        foreach (var (mentorId, majorId) in poolMentors)
        {
            var user = await _userRepository.GetByIdAsync(mentorId, ct);
            if (user is null || string.IsNullOrEmpty(user.EmployeeCode)) continue; // not a resolvable lecturer

            semester.AddEligibleMentor(mentorId, user.EmployeeCode, majorId, user.Email.Value, user.PhoneNumber, division: null, importedBy: null);
        }
    }
}
