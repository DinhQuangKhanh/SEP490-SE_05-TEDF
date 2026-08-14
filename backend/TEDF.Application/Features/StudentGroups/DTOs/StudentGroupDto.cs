namespace TEDF.Application.Features.StudentGroups.DTOs;

public record StudentGroupDto
{
    public Guid GroupId { get; init; }
    public string GroupCode { get; init; } = null!;

    /// <summary>Always SE_NN — the tail of <see cref="GroupCode"/>.</summary>
    public string? GroupName { get; init; }

    /// <summary>
    /// What to show the user: the group id (SE_NN) until the topic passes evaluation, then
    /// "SE_NN - English topic name - Mentor".
    /// </summary>
    public string DisplayName { get; init; } = null!;
    public string GroupStatus { get; init; } = null!;

    /// <summary>True when the requesting student is this group's leader — drives who may disband it.</summary>
    public bool IsLeader { get; init; }
    public int MaxMembers { get; init; }
    public bool IsOpenForRequests { get; init; }
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public string? ProjectCode { get; init; }
    public string? ProjectStatus { get; init; }
    public string? MentorName { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<GroupMemberDto> Members { get; init; } = new();
}
