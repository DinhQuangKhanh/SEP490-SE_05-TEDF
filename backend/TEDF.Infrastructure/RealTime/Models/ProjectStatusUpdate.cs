namespace TEDF.Infrastructure.RealTime.Models
{
    /// <summary>
    /// Project status update model for real-time notifications.
    /// </summary>
    public record ProjectStatusUpdate(
        Guid ProjectId,
        string ProjectName,
        string OldStatus,
        string NewStatus,
        DateTime UpdatedAt
    );
}
