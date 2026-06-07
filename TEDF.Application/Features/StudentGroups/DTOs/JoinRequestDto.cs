namespace TEDF.Application.Features.StudentGroups.DTOs;

public record JoinRequestDto
{
    public int Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = null!;
    public string? StudentCode { get; init; }
    public string? Message { get; init; }
    public string Status { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}
