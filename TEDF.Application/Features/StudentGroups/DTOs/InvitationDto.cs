namespace TEDF.Application.Features.StudentGroups.DTOs;

public record InvitationDto
{
    public int Id { get; init; }
    public Guid GroupId { get; init; }
    public string GroupCode { get; init; } = null!;
    public string? GroupName { get; init; }
    public Guid InviterId { get; init; }
    public string InviterName { get; init; } = null!;
    public string? Message { get; init; }
    public string Status { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}
