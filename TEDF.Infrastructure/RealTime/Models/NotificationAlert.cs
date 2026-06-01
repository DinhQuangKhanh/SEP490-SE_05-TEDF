namespace TEDF.Infrastructure.RealTime.Models
{
    /// <summary>
    /// Real-time notification alert model.
    /// </summary>
    public record NotificationAlert(
        Guid Id,
        string Title,
        string Content,
        string Type,
        string Category,
        string? TargetUrl,
        DateTime CreatedAt
    );
}
