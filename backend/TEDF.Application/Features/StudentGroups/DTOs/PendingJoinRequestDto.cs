namespace TEDF.Application.Features.StudentGroups.DTOs;

public record PendingJoinRequestDto
{
    public int RequestId { get; init; }
    public Guid GroupId { get; init; }
    public string GroupCode { get; init; } = string.Empty;
    public string? GroupName { get; init; }
    public string? Message { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}
