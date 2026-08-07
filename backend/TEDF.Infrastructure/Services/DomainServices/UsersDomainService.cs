using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Constants;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.User;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the Users feature. See <see cref="IUsersDomainService"/>.
/// </summary>
public class UsersDomainService : IUsersDomainService
{
    private const string FptEmailDomain = "@fpt.edu.vn";

    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IMajorReadRepository _majorRepository;
    private readonly IExcelService _excelService;
    private readonly IAuthAccountService _authAccount;
    private readonly IUnitOfWork _unitOfWork;

    public UsersDomainService(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IMajorReadRepository majorRepository,
        IExcelService excelService,
        IAuthAccountService authAccount,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _majorRepository = majorRepository;
        _excelService = excelService;
        _authAccount = authAccount;
        _unitOfWork = unitOfWork;
    }

    public async Task LockAsync(Guid userId, Guid actingUserId, CancellationToken cancellationToken = default)
    {
        // Prevent admin from locking themselves.
        if (actingUserId == userId)
            throw new BusinessRuleValidationException("Cannot lock your own account.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        user.Lock();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Disable the auth account before committing DB changes; if this fails the transaction is not persisted.
        await _authAccount.DisableAccountAsync(user.FirebaseUid, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        user.Activate();
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _authAccount.EnableAccountAsync(user.FirebaseUid, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignDepartmentHeadAsync(int departmentId, Guid userId, Guid assignedBy, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        if (user.DepartmentId != departmentId)
            throw new BusinessRuleValidationException("User does not belong to this department.");

        // One code path only, so the department-scoped route cannot create a second Department Head.
        await SetDepartmentHeadAsync(userId, assignedBy, cancellationToken);
    }

    public async Task SetDepartmentHeadAsync(Guid userId, Guid assignedBy, CancellationToken cancellationToken = default)
    {
        var newHead = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        if (!newHead.HasRole(DomainRoleIds.Mentor) && !newHead.HasRole(DomainRoleIds.Evaluator))
            throw new BusinessRuleValidationException(
                "Chỉ giảng viên (Mentor/Evaluator) mới có thể được gán làm Trưởng bộ môn.");

        if (newHead.Status != UserStatus.Active)
            throw new BusinessRuleValidationException(
                "Không thể gán Trưởng bộ môn cho tài khoản đang bị khóa hoặc chưa kích hoạt.");

        if (newHead.DepartmentId is not int departmentId)
            throw new BusinessRuleValidationException(
                "Giảng viên chưa thuộc bộ môn nào — hãy cập nhật bộ môn cho giảng viên trước khi gán Trưởng bộ môn.");

        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Department), departmentId);

        // The role is a singleton system-wide, so this is a transfer: strip it from the current
        // holder (in any department) before granting it. Doing it here rather than rejecting the
        // request means the system is never left without a Department Head mid-handover.
        await ClearDepartmentHeadsAsync(exceptUserId: userId, cancellationToken);

        newHead.AssignRole(DomainRoleIds.DepartmentHead, DomainRoleNames.DepartmentHead, assignedBy);
        await _userRepository.UpdateAsync(newHead, cancellationToken);

        department.SetHeadOfDepartment(userId);
        _departmentRepository.Update(department);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeDepartmentHeadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        if (!user.HasRole(DomainRoleIds.DepartmentHead))
            throw new BusinessRuleValidationException("Người dùng này không giữ vai trò Trưởng bộ môn.");

        user.RemoveRole(DomainRoleIds.DepartmentHead);
        await _userRepository.UpdateAsync(user, cancellationToken);

        await ClearHeadPointersAsync(userId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deactivates the DepartmentHead role on every holder except <paramref name="exceptUserId"/>,
    /// and clears the head pointer of the departments they headed. Does not save.
    /// </summary>
    private async Task ClearDepartmentHeadsAsync(Guid exceptUserId, CancellationToken cancellationToken)
    {
        var currentHeads = await _userRepository.GetByRoleAsync(DomainRoleNames.DepartmentHead, cancellationToken);

        foreach (var head in currentHeads.Where(h => h.Id != exceptUserId))
        {
            head.RemoveRole(DomainRoleIds.DepartmentHead);
            await _userRepository.UpdateAsync(head, cancellationToken);
            await ClearHeadPointersAsync(head.Id, cancellationToken);
        }
    }

    /// <summary>
    /// Clears <c>HeadOfDepartmentId</c> on every department still pointing at the user — plural
    /// because the user's own DepartmentId may have moved since they were appointed. Does not save.
    /// </summary>
    private async Task ClearHeadPointersAsync(Guid userId, CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);

        foreach (var department in departments.Where(d => d.HeadOfDepartmentId == userId))
        {
            department.SetHeadOfDepartment(null);
            _departmentRepository.Update(department);
        }
    }

    public async Task UpdateMyProfileAsync(Guid userId, string? phoneNumber, DateOnly? birthDate, string? privacySettings, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        user.UpdateProfile(
            phoneNumber: phoneNumber,
            birthDate: birthDate,
            privacySettings: privacySettings);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(CreateUserInput input, Guid actingUserId, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeRole(input.Role, out var role))
            throw new BusinessRuleValidationException($"Vai trò '{input.Role}' không hợp lệ.");
        if (role == DomainRoleNames.Admin)
            throw new BusinessRuleValidationException("Không thể tạo tài khoản Admin.");

        var email = (input.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!email.EndsWith(FptEmailDomain, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleValidationException($"Email phải thuộc miền {FptEmailDomain}.");
        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new BusinessRuleValidationException("Email đã tồn tại trong hệ thống.");

        var code = (input.Code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessRuleValidationException("Mã số không được để trống.");

        var isStudent = role == DomainRoleNames.Student;
        if (isStudent && await _userRepository.GetByStudentCodeAsync(code, cancellationToken) is not null)
            throw new BusinessRuleValidationException("Mã số sinh viên đã tồn tại.");
        if (!isStudent && await _userRepository.GetByEmployeeCodeAsync(code, cancellationToken) is not null)
            throw new BusinessRuleValidationException("Mã giảng viên đã tồn tại.");

        int? departmentId = await ResolveDepartmentIdAsync(input.MajorId, cancellationToken);

        var user = ProvisionUser(
            new NewUserData(role, email, input.FullName, code, input.Phone, input.AcademicTitle, departmentId),
            actingUserId);
        await _userRepository.AddAsync(user, cancellationToken);

        // Department Head is a singleton, and creating one is a handover like any other: the sitting
        // head loses the role in the same transaction rather than the request being refused. Same
        // semantics as SetDepartmentHeadAsync, so both entry points behave identically.
        if (role == DomainRoleNames.DepartmentHead)
        {
            await ClearDepartmentHeadsAsync(exceptUserId: user.Id, cancellationToken);

            if (departmentId is int newHeadDepartmentId)
            {
                var department = await _departmentRepository.GetByIdAsync(newHeadDepartmentId, cancellationToken);
                if (department is not null)
                {
                    department.SetHeadOfDepartment(user.Id);
                    _departmentRepository.Update(department);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task<UserImportResult> ImportUsersAsync(Stream fileStream, string fileName, Guid actingUserId, CancellationToken cancellationToken = default)
    {
        var rows = await _excelService.ExtractUserRowsAsync(fileStream, fileName, cancellationToken);
        var issues = new List<ImportRowIssue>();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var success = 0;

        foreach (var row in rows)
        {
            var (issue, data) = await ValidateImportRowAsync(row, seenEmails, seenCodes, cancellationToken);
            if (issue is not null || data is null)
            {
                if (issue is not null) issues.Add(issue);
                continue;
            }

            try
            {
                await _userRepository.AddAsync(ProvisionUser(data, actingUserId), cancellationToken);
                success++;
            }
            catch (Exception ex)
            {
                issues.Add(new ImportRowIssue(data.Code, ex.Message));
            }
        }

        if (success > 0) await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new UserImportResult(rows.Count, success, issues);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Validated data needed to provision one user (bundles the fields to keep signatures small).</summary>
    private sealed record NewUserData(
        string Role, string Email, string? FullName, string Code, string? Phone, string? AcademicTitle, int? DepartmentId);

    /// <summary>Builds a pending (SSO) user with the right sub-profile and role. Does not persist.</summary>
    private static User ProvisionUser(NewUserData data, Guid actingUserId)
    {
        var user = User.Create(
            firebaseUid: $"{User.PendingUidPrefix}{data.Code}",
            email: data.Email,
            fullName: string.IsNullOrWhiteSpace(data.FullName) ? data.Code : data.FullName.Trim(),
            departmentId: data.DepartmentId,
            phoneNumber: string.IsNullOrWhiteSpace(data.Phone) ? null : data.Phone.Trim());

        if (data.Role == DomainRoleNames.Student)
            user.InitializeStudentProfile(data.Code);
        else
            user.InitializeStaffProfile(data.Code, string.IsNullOrWhiteSpace(data.AcademicTitle) ? null : data.AcademicTitle.Trim());

        user.AssignRole(DomainRoleIds.FromName(data.Role), data.Role, actingUserId);
        return user;
    }

    /// <summary>
    /// Validates one import row (role, email, code, in-file + DB duplicates) and resolves its major.
    /// Returns an issue to skip the row, or the provisioning data when the row is valid.
    /// </summary>
    private async Task<(ImportRowIssue? Issue, NewUserData? Data)> ValidateImportRowAsync(
        UserImportRow row, HashSet<string> seenEmails, HashSet<string> seenCodes, CancellationToken ct)
    {
        var code = (row.Code ?? string.Empty).Trim();
        var label = string.IsNullOrWhiteSpace(code) ? (row.Email ?? "?") : code;

        // Import provisions students & lecturers only; DepartmentHead is a singleton created manually.
        if (!TryNormalizeRole(row.Role, out var role) || role is DomainRoleNames.Admin or DomainRoleNames.DepartmentHead)
            return (new ImportRowIssue(label, $"Vai trò '{row.Role}' không hợp lệ (chỉ Student/Mentor/Evaluator)."), null);

        var email = (row.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!email.EndsWith(FptEmailDomain, StringComparison.OrdinalIgnoreCase))
            return (new ImportRowIssue(label, $"Email không hợp lệ (phải là {FptEmailDomain})."), null);
        if (string.IsNullOrWhiteSpace(code))
            return (new ImportRowIssue(label, "Thiếu mã số."), null);
        if (!seenEmails.Add(email))
            return (new ImportRowIssue(label, $"Email '{email}' trùng trong file."), null);
        if (!seenCodes.Add(code))
            return (new ImportRowIssue(label, $"Mã số '{code}' trùng trong file."), null);
        if (await _userRepository.ExistsByEmailAsync(email, ct))
            return (new ImportRowIssue(label, $"Email '{email}' đã tồn tại."), null);

        var isStudent = role == DomainRoleNames.Student;
        if (isStudent && await _userRepository.GetByStudentCodeAsync(code, ct) is not null)
            return (new ImportRowIssue(label, "MSSV đã tồn tại."), null);
        if (!isStudent && await _userRepository.GetByEmployeeCodeAsync(code, ct) is not null)
            return (new ImportRowIssue(label, "Mã giảng viên đã tồn tại."), null);

        var departmentId = await ResolveDepartmentIdByNameOrCodeAsync(row.MajorName, ct);
        return (null, new NewUserData(role, email, row.FullName, code, row.Phone, row.AcademicTitle, departmentId));
    }

    private async Task<int?> ResolveDepartmentIdAsync(int? majorId, CancellationToken ct)
    {
        if (!majorId.HasValue) return null;
        var major = await _majorRepository.GetByIdAsync(majorId.Value, ct)
            ?? throw new EntityNotFoundException(nameof(Major), majorId.Value);
        return major.DepartmentId;
    }

    private async Task<int?> ResolveDepartmentIdByNameOrCodeAsync(string? majorNameOrCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(majorNameOrCode)) return null;
        var key = majorNameOrCode.Trim();
        var major = await _majorRepository.GetByNameAsync(key, ct) ?? await _majorRepository.GetByCodeAsync(key, ct);
        return major?.DepartmentId;
    }

    /// <summary>Maps an incoming role string (English canonical or common Vietnamese) to a canonical role name.</summary>
    private static bool TryNormalizeRole(string? raw, out string role)
    {
        role = string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        switch (raw.Trim().ToLowerInvariant())
        {
            case "student" or "sinh viên" or "sinhvien" or "sv":
                role = DomainRoleNames.Student; return true;
            case "mentor" or "giảng viên hướng dẫn" or "gvhd":
                role = DomainRoleNames.Mentor; return true;
            case "evaluator" or "người đánh giá" or "hội đồng":
                role = DomainRoleNames.Evaluator; return true;
            case "departmenthead" or "department head" or "trưởng bộ môn" or "truong bo mon":
                role = DomainRoleNames.DepartmentHead; return true;
            case "admin" or "quản trị":
                role = DomainRoleNames.Admin; return true;
            default:
                return false;
        }
    }
}
