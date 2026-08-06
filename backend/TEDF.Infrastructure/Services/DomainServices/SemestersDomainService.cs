using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.Commands;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate.Entities;
using TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using DomainEmail = TEDF.Domain.Aggregates.UserAggregate.ValueObjects.Email;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Constants;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Semester;
using TEDF.Domain.Services;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SemestersDomainService> _logger;

    public SemestersDomainService(
        ISemesterRepository semesterRepository,
        IUserRepository userRepository,
        IMajorReadRepository majorRepository,
        IProjectRepository projectRepository,
        IExcelService excelService,
        IUnitOfWork unitOfWork,
        ILogger<SemestersDomainService> logger)
    {
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
        _majorRepository = majorRepository;
        _projectRepository = projectRepository;
        _excelService = excelService;
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
            p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow);
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
        int semesterId, Stream fileStream, string fileName, Guid importedBy, CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        if (fileStream == null || fileStream.Length == 0)
            throw new BusinessRuleValidationException("File tải lên không hợp lệ hoặc rỗng.");

        var rows = await _excelService.ExtractEligibleStudentRowsAsync(fileStream, fileName, cancellationToken);

        if (rows.Count == 0)
            throw new BusinessRuleValidationException("Không tìm thấy mã sinh viên nào trong file. Đảm bảo file có cột chứa mã sinh viên hợp lệ.");

        var successCount = 0;
        var issues = new List<ImportRowIssue>();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var majorLookup = await LoadMajorLookupAsync(cancellationToken);

        foreach (var row in rows)
        {
            var student = await _userRepository.GetByStudentCodeAsync(row.StudentCode, cancellationToken);

            // Resolve the major by name or code (loaded once, in-memory — no per-row DB query).
            var major = ResolveMajorFor(majorLookup, row.MajorName);

            if (student is null)
            {
                student = await TryProvisionStudentAsync(row, major, seenEmails, cancellationToken);
                if (student is null)
                {
                    issues.Add(new ImportRowIssue(row.StudentCode, EmailIssueReason));
                    continue;
                }
            }
            else if (!InfoMatches(student, row.Email, row.FullName, row.PhoneNumber))
            {
                issues.Add(new ImportRowIssue(row.StudentCode,
                    "Thông tin (email/họ tên/SĐT) không khớp dữ liệu hệ thống — cần kiểm tra"));
                continue;
            }

            semester.AddEligibleStudent(student.Id, row.StudentCode, row.Email, row.PhoneNumber, major?.Id, importedBy);
            successCount++;
        }

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Imported {Count} eligible students for Semester {SemesterId}. Issues: {IssueCount}",
            successCount, semesterId, issues.Count);

        return new EligibleStudentsImportResult(rows.Count, successCount, issues);
    }

    public async Task<EligibleMentorsImportResult> ImportEligibleMentorsAsync(
        int semesterId, Stream fileStream, string fileName, Guid importedBy, CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        if (fileStream == null || fileStream.Length == 0)
            throw new BusinessRuleValidationException("File tải lên không hợp lệ hoặc rỗng.");

        var rows = await _excelService.ExtractEligibleMentorRowsAsync(fileStream, fileName, cancellationToken);

        if (rows.Count == 0)
            throw new BusinessRuleValidationException("Không tìm thấy mã giảng viên nào trong file. Đảm bảo file có cột chứa mã giảng viên và ngành hợp lệ.");

        var successCount = 0;
        var issues = new List<ImportRowIssue>();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var majorLookup = await LoadMajorLookupAsync(cancellationToken);

        foreach (var row in rows)
        {
            // Resolve the advising major from the file's "Ngành" column, falling back to the
            // "Bộ môn" (division) column — its code (SE/IA/AI/IC) matches a major code — so the
            // admin no longer has to pick "Ngành hướng dẫn" by hand for every imported row.
            var major = ResolveMajorFor(majorLookup, row.MajorName, row.Division);

            var resolved = await ResolveMentorIdentityAsync(row, major, seenCodes, seenEmails, issues, cancellationToken);
            if (resolved is null) continue;

            seenCodes.Add(resolved.Code);
            semester.AddEligibleMentor(resolved.Mentor.Id, resolved.Code, major?.Id,
                resolved.SnapshotEmail, row.PhoneNumber, row.Division, importedBy);
            successCount++;
        }

        await SyncPoolMentorsAsync(semester, cancellationToken);

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Imported {Count} eligible mentors for Semester {SemesterId}. Issues: {IssueCount}",
            successCount, semesterId, issues.Count);

        return new EligibleMentorsImportResult(rows.Count, successCount, issues);
    }

    private sealed record MentorResolution(User Mentor, string Code, string? SnapshotEmail);

    private async Task<MentorResolution?> ResolveMentorIdentityAsync(
        EligibleMentorRow row, Major? major, HashSet<string> seenCodes, HashSet<string> seenEmails,
        List<ImportRowIssue> issues, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByEmployeeCodeAsync(row.EmployeeCode, cancellationToken);

        if (existing is null)
        {
            var req = new UserProvisionRequest(row.EmployeeCode, row.FullName, row.Email, row.PhoneNumber, major, DomainRoleNames.Mentor, IsStudent: false);
            var mentor = await TryProvisionUserAsync(req, seenEmails, cancellationToken);
            if (mentor is null) { issues.Add(new ImportRowIssue(row.EmployeeCode, EmailIssueReason)); return null; }
            return new MentorResolution(mentor, row.EmployeeCode, row.Email);
        }

        if (!IsDifferentMentor(existing, row.PhoneNumber))
            return new MentorResolution(existing, row.EmployeeCode, row.Email);

        // Trùng mã nhưng KHÁC SĐT ⇒ người khác ⇒ thêm số thứ tự cho cả mã lẫn email.
        if (string.IsNullOrWhiteSpace(row.Email) || !DomainEmail.IsAllowed(row.Email.Trim()))
        {
            issues.Add(new ImportRowIssue(row.EmployeeCode, "Trùng mã GV nhưng khác người, thiếu email hợp lệ (@fpt.edu.vn / @fe.edu.vn / @gmail.com) để tạo tài khoản mới"));
            return null;
        }

        var (newCode, newEmail) = await NextAvailableSuffixAsync(row.EmployeeCode, row.Email.Trim(), seenCodes, seenEmails, cancellationToken);
        var newReq = new UserProvisionRequest(newCode, row.FullName, newEmail, row.PhoneNumber, major, DomainRoleNames.Mentor, IsStudent: false);
        var newMentor = await TryProvisionUserAsync(newReq, seenEmails, cancellationToken);
        if (newMentor is null) { issues.Add(new ImportRowIssue(row.EmployeeCode, "Không tạo được tài khoản mới (email bị trùng)")); return null; }
        return new MentorResolution(newMentor, newCode, newEmail);
    }

    public async Task PublishRosterAsync(int semesterId, Guid publishedBy, CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        await SyncPoolMentorsAsync(semester, cancellationToken);

        semester.PublishRoster(publishedBy);

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Published eligibility roster for Semester {SemesterId} by {PublishedBy}.", semesterId, publishedBy);
    }

    public async Task UpdateEligibleMentorMajorAsync(int semesterId, Guid mentorId, int majorId, CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        _ = await _majorRepository.GetByIdAsync(majorId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Major), majorId);

        semester.UpdateEligibleMentorMajor(mentorId, majorId);

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveEligibleStudentsAsync(int semesterId, IReadOnlyList<Guid> studentIds, CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        semester.RemoveEligibleStudents(studentIds);

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveEligibleMentorsAsync(int semesterId, IReadOnlyList<Guid> mentorIds, CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        semester.RemoveEligibleMentors(mentorIds);

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsMentorAssignedAsync(Guid mentorId, int semesterId, CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, cancellationToken);
        if (semester is not null && semester.EligibleMentors.Any(m => m.MentorId == mentorId && m.IsAssigned))
            return true;

        var poolMentors = await _projectRepository.GetPoolMentorAssignmentsForSemesterAsync(semesterId, cancellationToken);
        return poolMentors.Any(p => p.MentorId == mentorId);
    }

    private const string EmailIssueReason = "Email không hợp lệ (@fpt.edu.vn / @fe.edu.vn / @gmail.com) hoặc trùng";

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

    /// <summary>
    /// Loads every major once, keyed by both Code (e.g. "SE") and Name ("Kỹ thuật phần mềm"),
    /// case-insensitive — so imports resolve majors in-memory instead of one DB query per row.
    /// </summary>
    private async Task<Dictionary<string, Major>> LoadMajorLookupAsync(CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<string, Major>(StringComparer.OrdinalIgnoreCase);
        foreach (var major in await _majorRepository.GetAllAsync(cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(major.Code)) lookup.TryAdd(major.Code.Trim(), major);
            if (!string.IsNullOrWhiteSpace(major.Name)) lookup.TryAdd(major.Name.Trim(), major);
        }
        return lookup;
    }

    /// <summary>Returns the major for the first candidate that matches a major Code or Name.</summary>
    private static Major? ResolveMajorFor(Dictionary<string, Major> lookup, params string?[] candidates)
    {
        foreach (var candidate in candidates)
            if (!string.IsNullOrWhiteSpace(candidate) && lookup.TryGetValue(candidate.Trim(), out var major))
                return major;
        return null;
    }

    /// <summary>Tìm (mã, email) còn trống bằng cách thêm số thứ tự: LamDQ/lamdq@… → LamDQ1/lamdq1@…, LamDQ2/…</summary>
    private async Task<(string Code, string Email)> NextAvailableSuffixAsync(
        string baseCode, string baseEmail, HashSet<string> seenCodes, HashSet<string> seenEmails, CancellationToken cancellationToken)
    {
        var at = baseEmail.IndexOf('@');
        var local = at > 0 ? baseEmail[..at] : baseEmail;
        var domain = at > 0 ? baseEmail[at..] : "@fpt.edu.vn"; // gồm '@'

        for (var n = 1; n <= 1000; n++)
        {
            var candCode = $"{baseCode}{n}";
            var candEmail = $"{local}{n}{domain}".ToLowerInvariant();
            if (seenCodes.Contains(candCode) || seenEmails.Contains(candEmail)) continue;
            if (await _userRepository.GetByEmployeeCodeAsync(candCode, cancellationToken) is not null) continue;
            if (await _userRepository.ExistsByEmailAsync(candEmail, cancellationToken)) continue;
            return (candCode, candEmail);
        }

        throw new BusinessRuleValidationException($"Không tìm được mã/email khả dụng cho giảng viên '{baseCode}' sau 1000 lần thử.");
    }

    private sealed record UserProvisionRequest(
        string Code, string? FullName, string? Email, string? Phone,
        Major? Major, string Role, bool IsStudent);

    /// <summary>Tạo "tài khoản chờ" cho sinh viên chưa tồn tại; null nếu không thể tạo (caller báo lỗi).</summary>
    private Task<User?> TryProvisionStudentAsync(EligibleStudentRow row, Major? major, HashSet<string> seenEmails, CancellationToken cancellationToken)
    {
        var req = new UserProvisionRequest(row.StudentCode, row.FullName, row.Email, row.PhoneNumber, major, DomainRoleNames.Student, IsStudent: true);
        return TryProvisionUserAsync(req, seenEmails, cancellationToken);
    }

    /// <summary>
    /// Tạo User mới với FirebaseUid tạm ("pending:&lt;mã&gt;") để liên kết khi đăng nhập Google lần đầu.
    /// Yêu cầu email thuộc domain hợp lệ (@fpt.edu.vn / @fe.edu.vn / @gmail.com) và không trùng
    /// (trong file lẫn trong DB); trả null nếu vi phạm.
    /// </summary>
    private async Task<User?> TryProvisionUserAsync(
        UserProvisionRequest req, HashSet<string> seenEmails, CancellationToken cancellationToken)
    {
        var trimmed = req.Email?.Trim();
        // Accept any allowed domain (@fpt.edu.vn / @fe.edu.vn / @gmail.com) — same rule as Email.Create.
        if (string.IsNullOrWhiteSpace(trimmed) || !DomainEmail.IsAllowed(trimmed))
            return null;

        var normalized = trimmed.ToLowerInvariant();
        if (!seenEmails.Add(normalized)) return null;
        if (await _userRepository.ExistsByEmailAsync(normalized, cancellationToken)) return null;

        var user = User.Create(
            firebaseUid: $"{User.PendingUidPrefix}{req.Code}",
            email: normalized,
            fullName: string.IsNullOrWhiteSpace(req.FullName) ? req.Code : req.FullName.Trim(),
            departmentId: req.Major?.DepartmentId,
            phoneNumber: string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim());

        if (req.IsStudent)
            user.InitializeStudentProfile(req.Code);
        else
            user.InitializeStaffProfile(req.Code);

        user.AssignRole(DomainRoleIds.FromName(req.Role), req.Role);
        await _userRepository.AddAsync(user, cancellationToken);
        return user;
    }

    /// <summary>
    /// Adds (idempotently) any mentor who already owns a pool topic awaiting registration for this
    /// semester, using the topic's Major — so they are assigned even if the imported CSV omits them.
    /// </summary>
    private async Task SyncPoolMentorsAsync(Semester semester, CancellationToken cancellationToken)
    {
        var poolMentors = await _projectRepository.GetPoolMentorAssignmentsForSemesterAsync(semester.Id, cancellationToken);
        foreach (var (mentorId, majorId) in poolMentors)
        {
            var user = await _userRepository.GetByIdAsync(mentorId, cancellationToken);
            if (user is null || string.IsNullOrEmpty(user.Lecturer?.EmployeeCode)) continue;

            semester.AddEligibleMentor(mentorId, user.Lecturer.EmployeeCode, majorId, user.Email.Value, user.PhoneNumber, division: null, importedBy: null);
        }
    }
}
