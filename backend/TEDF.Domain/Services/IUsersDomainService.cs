namespace TEDF.Domain.Services;

/// <summary>
/// Write-side service for the Users feature.
/// Command handlers in <c>Application/Features/Users</c> depend on this service only
/// (no repositories / IUnitOfWork directly). The implementation owns the transaction.
/// </summary>
public interface IUsersDomainService
{
    /// <summary>Locks a user account (DB + auth provider). <paramref name="actingUserId"/> cannot equal <paramref name="userId"/>.</summary>
    Task LockAsync(Guid userId, Guid actingUserId, CancellationToken cancellationToken = default);

    /// <summary>Unlocks / re-activates a user account (DB + auth provider).</summary>
    Task UnlockAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Assigns a lecturer (Mentor/Evaluator) as head of a department, replacing the previous head.</summary>
    Task AssignDepartmentHeadAsync(int departmentId, Guid userId, Guid assignedBy, CancellationToken cancellationToken = default);

    /// <summary>Updates the profile of a user.</summary>
    Task UpdateMyProfileAsync(Guid userId, string? phoneNumber, DateOnly? birthDate, string? privacySettings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Provisions a single user (SSO/pending model — no Firebase account). The role must be one of
    /// Student/Mentor/Evaluator/DepartmentHead (never Admin); DepartmentHead is rejected when one
    /// already exists. Returns the new user id.
    /// </summary>
    Task<Guid> CreateAsync(CreateUserInput input, Guid actingUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-provisions users from an Excel/CSV stream (Student/Mentor/Evaluator only). Never throws
    /// on bad rows — returns a per-row issue summary alongside the success count.
    /// </summary>
    Task<UserImportResult> ImportUsersAsync(Stream fileStream, string fileName, Guid actingUserId, CancellationToken cancellationToken = default);
}

/// <summary>Input for provisioning a single user via the admin "Thêm mới" flow.</summary>
public record CreateUserInput(
    string Role,
    string Email,
    string FullName,
    string Code,
    string? Phone,
    string? AcademicTitle,
    int? MajorId);

/// <summary>Result of a bulk user import (mirrors the eligible-roster import result shape).</summary>
public record UserImportResult(int TotalProcessed, int SuccessfullyImported, IReadOnlyList<ImportRowIssue> Issues);
