namespace TEDF.Domain.Services;

/// <summary>
/// Write-side service for the StudentGroups feature (plus group helper queries used by write flows).
/// Command handlers depend on this only.
/// </summary>
public interface IStudentGroupsDomainService
{
    // ── Helper queries ──
    Task<(bool CanJoin, string? Reason)> CanStudentJoinGroupAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> GetGroupsWithoutProjectAsync(int semesterId, CancellationToken cancellationToken = default);

    // ── StudentGroups feature write operations ──
    /// <summary>Creates a group; <paramref name="displayName"/> is the optional student nickname —
    /// the code and Name are generated as {SemesterCode}-SE_NN / SE_NN.</summary>
    Task<Guid> CreateGroupAsync(Guid studentId, string? displayName, CancellationToken cancellationToken = default);
    Task<int> InviteMemberAsync(Guid groupId, Guid inviterId, string studentCode, string? message, CancellationToken cancellationToken = default);
    Task<int> RequestJoinAsync(Guid groupId, Guid studentId, string? message, CancellationToken cancellationToken = default);
    Task RespondInvitationAsync(Guid groupId, int invitationId, Guid studentId, bool accept, CancellationToken cancellationToken = default);
    Task RespondJoinRequestAsync(Guid groupId, int requestId, Guid leaderId, bool approve, CancellationToken cancellationToken = default);
}
