namespace TEDF.Application.Features.Topics.DTOs;

/// <summary>
/// Lightweight DTO for listing individual topics (projects) browsed from a topic pool.
/// Used in paginated list views. Includes denormalized major and primary mentor info.
/// </summary>
public class TopicInPoolItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string NameVi { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Technologies { get; init; }
    public int MajorId { get; init; }
    public string MajorName { get; init; } = string.Empty;
    public string MajorCode { get; init; } = string.Empty;
    public int PoolStatus { get; init; }
    public string PoolStatusName { get; init; } = string.Empty;
    public int MaxStudents { get; init; }
    public string MentorName { get; init; } = string.Empty;
    public Guid MentorId { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Paginated result for topics-in-pool list queries.
/// </summary>
public record GetTopicsInPoolResult(
    IReadOnlyList<TopicInPoolItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
