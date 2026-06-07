namespace TEDF.Application.Features.StudentGroups.DTOs;

public record MentorGroupDto
{
    public Guid GroupId { get; init; }
    public string GroupCode { get; init; } = string.Empty;
    public string? GroupName { get; init; }
    public string GroupStatus { get; init; } = string.Empty;
    public int MaxMembers { get; init; }
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public string? ProjectCode { get; init; }
    public string? ProjectStatus { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<GroupMemberDto> Members { get; init; } = [];
}
