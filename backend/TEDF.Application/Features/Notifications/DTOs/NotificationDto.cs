namespace TEDF.Application.Features.Notifications.DTOs;

/// <summary>
/// DTO representing a single notification.
/// </summary>
public record NotificationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? TargetUrl { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for paginated notification list response.
/// </summary>
public record NotificationListResponseDto
{
    public IEnumerable<NotificationDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public long UnreadCount { get; init; }
}
