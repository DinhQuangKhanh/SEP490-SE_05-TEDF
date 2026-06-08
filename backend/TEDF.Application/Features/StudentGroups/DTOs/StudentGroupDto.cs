namespace TEDF.Application.Features.StudentGroups.DTOs;

public record StudentGroupDto
{
    public Guid GroupId { get; init; }
    public string GroupCode { get; init; } = null!;
    public string? GroupName { get; init; }
    public string GroupStatus { get; init; } = null!;
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
