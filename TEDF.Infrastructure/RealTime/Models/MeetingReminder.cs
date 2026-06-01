namespace TEDF.Infrastructure.RealTime.Models
{
    /// <summary>
    /// Meeting reminder model for real-time notifications.
    /// </summary>
    public record MeetingReminder(
        Guid MeetingId,
        string Title,
        DateTime ScheduledTime,
        string Location,
        int MinutesUntilStart
    );
}
