namespace TEDF.Domain.Services;

/// <summary>
/// Write-side service for the StudentGroups feature (plus group helper queries used by write flows).
/// Command handlers depend on this only.
/// </summary>
public interface IStudentGroupsDomainService
{
    // ── StudentGroups feature write operations ──
    /// <summary>Creates a group; <paramref name="displayName"/> is the optional student nickname —
    /// the code and Name are generated as {SemesterCode}-SE_NN / SE_NN.</summary>
    /// <summary>
    /// Creates a group for the student, who becomes its leader. The group's id (SE_NN) is assigned
    /// by the system; groups carry no user-supplied name.
    /// </summary>
    Task<Guid> CreateGroupAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<int> InviteMemberAsync(Guid groupId, Guid inviterId, string studentCode, string? message, CancellationToken cancellationToken = default);
    Task<int> RequestJoinAsync(Guid groupId, Guid studentId, string? message, CancellationToken cancellationToken = default);
    Task RespondInvitationAsync(Guid groupId, int invitationId, Guid studentId, bool accept, CancellationToken cancellationToken = default);
    Task RespondJoinRequestAsync(Guid groupId, int requestId, Guid leaderId, bool approve, CancellationToken cancellationToken = default);

    /// <summary>A non-leader member drops out of the group on their own.</summary>
    Task LeaveGroupAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>The leader disbands the group; every remaining member is dropped with it.</summary>
    Task DisbandGroupAsync(Guid groupId, Guid leaderId, CancellationToken cancellationToken = default);
}
