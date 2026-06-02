using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Application.Features.TopicPools.DTOs;

public class TopicPoolDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int MajorId { get; init; }
    public TopicPoolStatus Status { get; init; }
    public string StatusName => Status.ToString();
    public int MaxActiveTopicsPerMentor { get; init; }
    public int ExpirationSemesters { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
