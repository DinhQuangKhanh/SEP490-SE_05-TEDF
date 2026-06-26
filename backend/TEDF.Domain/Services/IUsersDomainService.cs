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
}
