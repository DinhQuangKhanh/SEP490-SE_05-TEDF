namespace TEDF.Application.Features.StudentGroups.DTOs;

public record MentorGroupDto
{
    public Guid GroupId { get; init; }
    public string GroupCode { get; init; } = string.Empty;
    public string? GroupName { get; init; }

    /// <summary>
    /// What to show the user: the group id (SE_NN) until the topic passes evaluation, then
    /// "SE_NN - English topic name - Mentor".
    /// </summary>
    public string DisplayName { get; init; } = null!;
    public string GroupStatus { get; init; } = string.Empty;
    public int MaxMembers { get; init; }
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }

    /// <summary>The topic's English name — the middle segment of <see cref="DisplayName"/>.</summary>
    public string? ProjectNameEn { get; init; }
    public string? ProjectCode { get; init; }
    public string? ProjectStatus { get; init; }
    public int SemesterId { get; init; }
    public string SemesterName { get; init; } = string.Empty;
    public DateTime SemesterStartDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<GroupMemberDto> Members { get; init; } = [];
}
