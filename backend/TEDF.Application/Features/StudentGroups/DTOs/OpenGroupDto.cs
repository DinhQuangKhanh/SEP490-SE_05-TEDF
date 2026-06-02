namespace TEDF.Application.Features.StudentGroups.DTOs;

public record OpenGroupDto
{
    public Guid GroupId { get; init; }
    public string GroupCode { get; init; } = null!;
    public string? GroupName { get; init; }
    public int MemberCount { get; init; }
    public int MaxMembers { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<GroupMemberDto> Members { get; init; } = [];
}
