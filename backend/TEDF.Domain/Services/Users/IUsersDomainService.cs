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

    /// <summary>
    /// Makes the lecturer the system's Department Head. The department is taken from the lecturer's
    /// own <c>DepartmentId</c>, so the caller does not have to know it.
    /// <para>
    /// The role is a singleton: whoever holds it loses it in the same operation, and their
    /// department's head pointer is cleared. The lecturer must be Active and hold Mentor or
    /// Evaluator, and must already belong to a department.
    /// </para>
    /// </summary>
    Task SetDepartmentHeadAsync(Guid userId, Guid assignedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes the DepartmentHead role away from the user and clears the head pointer of every
    /// department still pointing at them. Throws when the user does not hold the role.
    /// </summary>
    Task RevokeDepartmentHeadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Updates the profile of a user.</summary>
    Task UpdateMyProfileAsync(Guid userId, string? phoneNumber, DateOnly? birthDate, string? privacySettings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Provisions a single user (SSO/pending model — no Firebase account). The role must be one of
    /// Student/Mentor/Evaluator/DepartmentHead (never Admin). Creating a DepartmentHead transfers the
    /// role away from the sitting head, exactly like <see cref="SetDepartmentHeadAsync"/>.
    /// Returns the new user id.
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
