namespace TEDF.Infrastructure.RealTime.Models
{
    /// <summary>
    /// Defense notification model for real-time notifications.
    /// </summary>
    public record DefenseNotification(
        Guid DefenseId,
        Guid ProjectId,
        string ProjectName,
        DateTime ScheduledTime,
        string Location,
        string Role
    );
}
