using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-only query service for complex StudentGroup queries that span multiple aggregates.
/// Implemented in the Persistence layer with direct DbContext access for optimal performance.
/// </summary>
public interface IStudentGroupQueryService
{
    /// <summary>
    /// Gets all groups that a mentor is guiding in a specific semester.
    /// If semesterId is null, uses the currently active semester.
    /// </summary>
    Task<List<MentorGroupDto>> GetMentorGroupsAsync(
        Guid mentorId,
        int? semesterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the group that a student belongs to in a specific semester.
    /// If semesterId is null, uses the currently active semester.
    /// </summary>
    Task<StudentGroupDto?> GetStudentGroupAsync(
        Guid studentId,
        int? semesterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open groups (active, not full, accepting requests) in the next semester.
    /// Excludes groups that the student is already a member of.
    /// If semesterId is null, automatically resolves the next semester.
    /// </summary>
    Task<List<OpenGroupDto>> GetOpenGroupsAsync(
        Guid studentId,
        int? semesterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending invitations for a student.
    /// </summary>
    Task<List<InvitationDto>> GetStudentInvitationsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending join requests for a group.
    /// </summary>
    Task<List<JoinRequestDto>> GetGroupJoinRequestsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current student's pending join request in a semester.
    /// If semesterId is null, uses the currently active semester.
    /// </summary>
    Task<PendingJoinRequestDto?> GetStudentPendingJoinRequestAsync(
        Guid studentId,
        int? semesterId,
        CancellationToken cancellationToken = default);
}
